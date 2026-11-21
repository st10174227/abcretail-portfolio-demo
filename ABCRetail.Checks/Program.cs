using ABCRetail.Demo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

var fixture=Path.Combine(Path.GetTempPath(),"abc-retail-checks-"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Path.Combine(fixture,"wwwroot"));
var passed=0;
static void Need(bool value,string message) { if(!value)throw new InvalidOperationException(message); }
async Task Check(string name,Func<Task> run) { await run(); Console.WriteLine("PASS "+name); passed++; }
static string Attr(string tag,string name)
{
    var match=Regex.Match(tag,"\\b"+Regex.Escape(name)+"=\"([^\"]*)\"",RegexOptions.IgnoreCase);
    return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : "";
}
static string Hidden(string html,string name) => Regex.Matches(html,"<input\\b[^>]*>",RegexOptions.IgnoreCase).Select(m=>m.Value)
    .Where(tag=>Attr(tag,"name")==name).Select(tag=>Attr(tag,"value")).FirstOrDefault() ?? throw new InvalidOperationException("Missing form field: "+name);
static HashSet<string> Ids(string html,string action) => Regex.Matches(html,"/"+Regex.Escape(action)+"/([a-f0-9]{32})",RegexOptions.IgnoreCase).Select(m=>m.Groups[1].Value).ToHashSet();
static FormUrlEncodedContent Form(Dictionary<string,string> fields) => new(fields);
async Task<string> Token(HttpClient client,string route="/Demo") => Hidden(await client.GetStringAsync(route),"__RequestVerificationToken");
async Task<HttpResponseMessage> Post(HttpClient client,string route,Dictionary<string,string> fields)
{ fields["__RequestVerificationToken"]=await Token(client); return await client.PostAsync(route,Form(fields)); }
async Task<string> Good(HttpClient client,string route,Dictionary<string,string> fields)
{
    using var response=await Post(client,route,fields); var body=await response.Content.ReadAsStringAsync();
    Need(response.StatusCode==HttpStatusCode.Redirect,$"Expected redirect from {route}; HTTP {(int)response.StatusCode}; {body[..Math.Min(body.Length,800)]}");
    return response.Headers.Location?.ToString() ?? throw new InvalidOperationException("Redirect address missing.");
}
async Task<string> Invalid(HttpClient client,string route,Dictionary<string,string> fields)
{
    using var response=await Post(client,route,fields);var body=await response.Content.ReadAsStringAsync();
    Need(response.StatusCode==HttpStatusCode.OK && body.Contains("validation-summary-errors"),$"Expected readable form errors at {route}; HTTP {(int)response.StatusCode}");return body;
}
async Task<HttpResponseMessage> Upload(HttpClient client,string route,string field,string name,byte[] bytes,Dictionary<string,string>? fields=null)
{
    using var content=new MultipartFormDataContent();content.Add(new StringContent(await Token(client)),"__RequestVerificationToken");
    if(fields is not null)foreach(var pair in fields)content.Add(new StringContent(pair.Value),pair.Key);
    content.Add(new ByteArrayContent(bytes),field,name);return await client.PostAsync(route,content);
}
var customerId="";var productId="";var orderId="";var imageId="";var contractId="";
Dictionary<string,string> Customer(string name="Visitor One",string email="visitor-one@example.invalid") => new() { ["Name"]=name,["Email"]=email };
Dictionary<string,string> Product(string name="Fixture product",string price="12.50") => new() { ["Name"]=name,["Description"]="A fictional check product.",["Price"]=price };
Dictionary<string,string> Order(string quantity="3") => new() { ["CustomerId"]=customerId,["ProductId"]=productId,["Quantity"]=quantity,["OrderDate"]=DateTime.UtcNow.ToString("yyyy-MM-dd") };
try
{
    await using var app=CLDV_Final.Program.BuildApplication(["--environment=Testing","--contentRoot="+fixture],builder=>
    {
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string,string?> { ["Demo:DataDirectory"]=Path.Combine(fixture,"Keys") });
        builder.Logging.ClearProviders();builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Error);
    });
    await app.StartAsync();
    HttpClient Client()=>new(new CookieJar(app.GetTestServer().CreateHandler())) { BaseAddress=new Uri("https://localhost") };
    using var one=Client();using var two=Client();
    var registry=app.Services.GetRequiredService<WorkspaceRegistry>();
    await Check("Health checks and unmatched URLs do not allocate visitor workspaces",async()=>
    {
        using var health=await one.GetAsync("/health");using var missing=await one.GetAsync("/missing-path");
        Need(health.StatusCode==HttpStatusCode.OK && missing.StatusCode==HttpStatusCode.NotFound && registry.Count==0,"Health or unmatched URL allocated a workspace.");
    });
    await Check("Production HTTP redirects before rendering secure forms",async()=>
    {
        using var client=Client();using var response=await client.GetAsync("http://localhost/Order/AddOrder");
        Need(response.StatusCode==HttpStatusCode.TemporaryRedirect && response.Headers.Location?.Scheme=="https" && registry.Count==0,"HTTP forms were not redirected before allocation.");
    });
    await Check("Fresh samples and every workspace page load without Azure settings",async()=>
    {
        foreach(var route in new[]{"/","/Home/Privacy","/Customer","/Customer/AddCustomer","/Product","/Product/AddProduct","/Order","/Order/AddOrder","/ABCRetail/ListContract","/ABCRetail/UploadContract","/ABCRetail/ListImages","/ABCRetail/UploadImage","/Demo"})
        { using var response=await one.GetAsync(route);Need(response.StatusCode==HttpStatusCode.OK,$"Page failed: {route}; HTTP {(int)response.StatusCode}"); }
        Need((await one.GetStringAsync("/Customer")).Contains("Amina Example"),"Seed customers are missing.");
        using var response2=await two.GetAsync("/");Need(registry.Count==2,"Browsers did not get independent workspaces.");
    });
    await Check("Every mutating workflow requires antiforgery",async()=>
    {
        foreach(var route in new[]{"/Customer/AddCustomer","/Customer/EditCustomer","/Customer/DeleteCustomer","/Product/AddProduct","/Product/EditProduct","/Product/DeleteProduct","/Order/AddOrder","/Order/EditOrder","/Order/DeleteOrder","/ABCRetail/UploadContract","/ABCRetail/UploadImage","/ABCRetail/DeleteContract","/ABCRetail/DeleteImage","/Demo/Reset","/Demo/ProcessNext"})
        { using var response=await one.PostAsync(route,Form(new()));Need(response.StatusCode==HttpStatusCode.BadRequest,"Tokenless mutation accepted: "+route); }
    });
    await Check("Valid customer creation persists only inside its browser workspace",async()=>
    {
        var before=Ids(await one.GetStringAsync("/Customer"),"EditCustomer");await Good(one,"/Customer/AddCustomer",Customer());
        var body=await one.GetStringAsync("/Customer");customerId=Ids(body,"EditCustomer").Except(before).Single();
        Need(body.Contains("Visitor One") && !(await two.GetStringAsync("/Customer")).Contains("Visitor One"),"Customer data crossed browsers.");
    });
    await Check("Invalid and duplicate customer forms show errors without creating records",async()=>
    {
        var count=Ids(await one.GetStringAsync("/Customer"),"EditCustomer").Count;
        await Invalid(one,"/Customer/AddCustomer",Customer("","invalid"));await Invalid(one,"/Customer/AddCustomer",Customer("Duplicate","VISITOR-ONE@EXAMPLE.INVALID"));
        Need(Ids(await one.GetStringAsync("/Customer"),"EditCustomer").Count==count,"Invalid customer was saved.");
    });
    await Check("Editable customer content is HTML-encoded when displayed",async()=>
    {
        await Good(one,"/Customer/AddCustomer",Customer("<script>alert(1)</script>","encoded@example.invalid"));
        var html=await one.GetStringAsync("/Customer");Need(html.Contains("&lt;script&gt;") && !html.Contains("<script>alert(1)</script>"),"Customer content was rendered as executable HTML.");
    });
    await Check("Customer editing detects stale versions and preserves the latest value",async()=>
    {
        var html=await one.GetStringAsync("/Customer/EditCustomer/"+customerId);var fields=Customer("Updated Visitor");fields["Id"]=customerId;fields["Version"]=Hidden(html,"Version");
        await Good(one,"/Customer/EditCustomer",fields);fields["Name"]="Stale Visitor";
        await Invalid(one,"/Customer/EditCustomer",fields);var body=await one.GetStringAsync("/Customer");
        Need(body.Contains("Updated Visitor") && !body.Contains("Stale Visitor"),"A stale customer edit replaced current data.");
    });
    await Check("Valid products save without an image and ignore forged storage fields",async()=>
    {
        var before=Ids(await one.GetStringAsync("/Product"),"EditProduct");var fields=Product();fields["ImageUrl"]="https://example.invalid/forged.png";fields["ExistingImageId"]="forged";
        await Good(one,"/Product/AddProduct",fields);var body=await one.GetStringAsync("/Product");productId=Ids(body,"EditProduct").Except(before).Single();
        Need(body.Contains("Fixture product") && !body.Contains("forged.png"),"Valid product or server-owned image handling failed.");
    });
    await Check("Malformed, negative, oversized and overprecise prices do not persist",async()=>
    {
        var count=Ids(await one.GetStringAsync("/Product"),"EditProduct").Count;
        foreach(var price in new[]{"-1","0","1000001","NaN","Infinity","0.001"})await Invalid(one,"/Product/AddProduct",Product("Invalid product",price));
        Need(Ids(await one.GetStringAsync("/Product"),"EditProduct").Count==count,"An unsafe price was saved.");
    });
    await Check("Orders use saved identities and prices rather than submitted totals",async()=>
    {
        var before=Ids(await one.GetStringAsync("/Order"),"EditOrder");var fields=Order();fields["UnitPrice"]="0.01";fields["Total"]="0.03";fields["CustomerName"]="Forged customer";
        await Good(one,"/Order/AddOrder",fields);var body=await one.GetStringAsync("/Order");orderId=Ids(body,"EditOrder").Except(before).Single();
        Need(body.Contains("37.50") && body.Contains("Updated Visitor") && !body.Contains("Forged customer"),"Order trusted submitted identity or price.");
    });
    await Check("Invalid quantities, dates and unknown references preserve orders",async()=>
    {
        var count=Ids(await one.GetStringAsync("/Order"),"EditOrder").Count;
        foreach(var quantity in new[]{"0","1000","bad"})await Invalid(one,"/Order/AddOrder",Order(quantity));
        var fields=Order();fields["OrderDate"]="2000-01-01";await Invalid(one,"/Order/AddOrder",fields);
        fields=Order();fields.Remove("OrderDate");await Invalid(one,"/Order/AddOrder",fields);
        fields=Order();fields["CustomerId"]=Guid.NewGuid().ToString("N");await Invalid(one,"/Order/AddOrder",fields);
        Need(Ids(await one.GetStringAsync("/Order"),"EditOrder").Count==count,"Invalid order persisted.");
    });
    await Check("GET confirmation pages are read-only and linked records cannot be deleted",async()=>
    {
        foreach(var pair in new[]{("Customer",customerId),("Product",productId)})
        {
            var html=await one.GetStringAsync($"/{pair.Item1}/Delete{pair.Item1}/{pair.Item2}");
            Need((await one.GetStringAsync("/"+pair.Item1)).Contains(pair.Item2),"GET deleted a record.");
            await Invalid(one,$"/{pair.Item1}/Delete{pair.Item1}",new(){["id"]=pair.Item2,["version"]=Hidden(html,"version")});
        }
    });
    await Check("Recorded order prices survive product changes and stale order edits fail",async()=>
    {
        var html=await one.GetStringAsync("/Product/EditProduct/"+productId);var product=Product(price:"20.00");product["Id"]=productId;product["Version"]=Hidden(html,"Version");await Good(one,"/Product/EditProduct",product);
        html=await one.GetStringAsync("/Order/EditOrder/"+orderId);var order=Order("4");order["Id"]=orderId;order["Version"]=Hidden(html,"Version");await Good(one,"/Order/EditOrder",order);
        Need((await one.GetStringAsync("/Order")).Contains("50.00"),"Quantity edit changed the saved unit price.");
        order["Quantity"]="5";await Invalid(one,"/Order/EditOrder",order);
    });
    await Check("Other browsers cannot read, change or delete private records",async()=>
    {
        foreach(var pair in new[]{("Customer",customerId),("Product",productId),("Order",orderId)})
        {
            foreach(var action in new[]{"Edit","Delete"}) { using var response=await two.GetAsync($"/{pair.Item1}/{action}{pair.Item1}/{pair.Item2}");Need(response.StatusCode==HttpStatusCode.NotFound,"A private record was exposed."); }
            using var deletion=await Post(two,$"/{pair.Item1}/Delete{pair.Item1}",new(){["id"]=pair.Item2,["version"]="1"});Need(deletion.StatusCode==HttpStatusCode.NotFound,"Cross-browser deletion succeeded.");
        }
        var fields=Customer("Foreign edit");fields["Id"]=customerId;fields["Version"]="2";using var edit=await Post(two,"/Customer/EditCustomer",fields);Need(edit.StatusCode==HttpStatusCode.NotFound,"Cross-browser update succeeded.");
    });
    var png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aG6cAAAAASUVORK5CYII=");
    await Check("Product images are stored privately and replacement removes old bytes",async()=>
    {
        async Task<string> SaveImage()
        {
            var html=await one.GetStringAsync("/Product/EditProduct/"+productId);var fields=Product(price:"20.00");fields["Id"]=productId;fields["Version"]=Hidden(html,"Version");
            using var response=await Upload(one,"/Product/EditProduct","imageFile","sample.png",png,fields);Need(response.StatusCode==HttpStatusCode.Redirect,"Valid image upload failed.");
            html=await one.GetStringAsync("/Product/EditProduct/"+productId);return Regex.Match(html,"/ABCRetail/Image/([a-f0-9]{32})").Groups[1].Value;
        }
        var old=await SaveImage();Need(old.Length==32,"Product image address missing.");imageId=await SaveImage();
        using var response=await one.GetAsync("/ABCRetail/Image/"+imageId);Need(response.StatusCode==HttpStatusCode.OK && (await response.Content.ReadAsByteArrayAsync()).SequenceEqual(png),"Uploaded image bytes changed.");
        using var gone=await one.GetAsync("/ABCRetail/Image/"+old);using var other=await two.GetAsync("/ABCRetail/Image/"+imageId);
        Need(gone.StatusCode==HttpStatusCode.NotFound && other.StatusCode==HttpStatusCode.NotFound,"Old or cross-browser image remained readable.");
    });
    await Check("Standalone image pages work and reject disguised or oversized files",async()=>
    {
        foreach(var bytes in new[]{Encoding.UTF8.GetBytes("<svg onload='alert(1)'></svg>"),new byte[2*1024*1024+1]})
        { using var response=await Upload(one,"/ABCRetail/UploadImage","imageFile","fake.png",bytes);Need(response.StatusCode==HttpStatusCode.OK && (await response.Content.ReadAsStringAsync()).Contains("validation-summary-errors"),"Unsafe upload accepted."); }
        using var accepted=await Upload(one,"/ABCRetail/UploadImage","imageFile","standalone.png",png);Need(accepted.StatusCode==HttpStatusCode.Redirect,"Standalone image action is missing.");
        Need((await one.GetStringAsync("/ABCRetail/ListImages")).Contains("standalone.png"),"Uploaded standalone image is absent.");
    });
    await Check("PDF contracts download as attachments and cannot cross browsers",async()=>
    {
        var pdf=await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory,"Fixtures","Sample-Contract.pdf"));
        using var upload=await Upload(one,"/ABCRetail/UploadContract","contractFile","../../sample-contract.pdf",pdf);Need(upload.StatusCode==HttpStatusCode.Redirect,"PDF upload failed.");
        var html=await one.GetStringAsync("/ABCRetail/ListContract");contractId=Regex.Match(html,"/ABCRetail/DownloadContract/([a-f0-9]{32})").Groups[1].Value;Need(contractId.Length==32,"Contract address missing.");
        using var download=await one.GetAsync("/ABCRetail/DownloadContract/"+contractId);Need(download.Content.Headers.ContentDisposition?.DispositionType=="attachment" && (await download.Content.ReadAsByteArrayAsync()).SequenceEqual(pdf),"Contract attachment bytes or headers are wrong.");
        using var cross=await two.GetAsync("/ABCRetail/DownloadContract/"+contractId);using var delete=await Post(two,"/ABCRetail/DeleteContract",new(){["id"]=contractId});
        Need(cross.StatusCode==HttpStatusCode.NotFound && delete.StatusCode==HttpStatusCode.NotFound,"Contract crossed browser workspaces.");
        using var invalid=await Upload(one,"/ABCRetail/UploadContract","contractFile","bad.pdf",Encoding.UTF8.GetBytes("<html>not a PDF</html>"));Need((await invalid.Content.ReadAsStringAsync()).Contains("validation-summary-errors"),"Disguised PDF accepted.");
        await Good(one,"/ABCRetail/DeleteContract",new(){["id"]=contractId});using var removed=await one.GetAsync("/ABCRetail/DownloadContract/"+contractId);Need(removed.StatusCode==HttpStatusCode.NotFound,"Deleted PDF remained readable.");
    });
    await Check("Queue processing changes the oldest pending event only",async()=>
    {
        var before=await one.GetStringAsync("/Demo");Need(before.Contains("Pending") && !before.Contains(">Processed</span>"),"Queue fixture is not pending.");
        await Good(one,"/Demo/ProcessNext",new());var html=await one.GetStringAsync("/Demo");
        var rows=Regex.Matches(html,"<tr><td>([0-9]+)</td>.*?</tr>",RegexOptions.Singleline).Select(m=>m.Value).ToArray();
        Need(rows.Length>=2 && rows[0].Contains(">Processed</span>") && rows.Skip(1).All(x=>x.Contains(">Pending</span>")),"Queue did not preserve first-in, first-out order.");
    });
    await Check("Order removal releases linked records and removes product image bytes",async()=>
    {
        foreach(var pair in new[]{("Order",orderId),("Customer",customerId),("Product",productId)})
        {
            var html=await one.GetStringAsync($"/{pair.Item1}/Delete{pair.Item1}/{pair.Item2}");await Good(one,$"/{pair.Item1}/Delete{pair.Item1}",new(){["id"]=pair.Item2,["version"]=Hidden(html,"version")});
            using var gone=await one.GetAsync($"/{pair.Item1}/Edit{pair.Item1}/{pair.Item2}");Need(gone.StatusCode==HttpStatusCode.NotFound,"Deleted record survived.");
        }
        using var image=await one.GetAsync("/ABCRetail/Image/"+imageId);Need(image.StatusCode==HttpStatusCode.NotFound,"Product deletion left image bytes.");
    });
    await Check("Reset clears one browser's records, files and events without changing another",async()=>
    {
        await Good(two,"/Customer/AddCustomer",Customer("Second Browser","second@example.invalid"));
        await Good(one,"/Customer/AddCustomer",Customer("Reset Me","reset@example.invalid"));await Good(one,"/Demo/Reset",new());
        Need(!(await one.GetStringAsync("/Customer")).Contains("Reset Me") && (await two.GetStringAsync("/Customer")).Contains("Second Browser"),"Reset crossed workspaces.");
        Need(!(await one.GetStringAsync("/ABCRetail/ListImages")).Contains("standalone.png") && !(await one.GetStringAsync("/Demo")).Contains(">Pending</span>"),"Reset left files or events.");
    });
    await Check("Workspace capacity and expiry are bounded without reusing old data",()=>
    {
        var clock=new TestClock();var bounded=new WorkspaceRegistry(clock);var first="";
        for(var i=0;i<20;i++) { using var lease=bounded.Enter(null,out var workspace);Need(lease is not null && workspace is not null,"Capacity was too small.");if(i==0)first=workspace!.Id; }
        using(var full=bounded.Enter(null,out _))Need(full is null,"Capacity was not enforced.");
        clock.Now=clock.Now.AddMinutes(46);using var fresh=bounded.Enter(first,out var replacement);Need(fresh is not null && replacement?.Id!=first && bounded.Count==1,"Expired data remained accessible.");return Task.CompletedTask;
    });
    await Check("Cumulative file quota rejects overflow without partial uploads",async()=>
    {
        using var client=Client();var bytes=new byte[2*1024*1024];png.CopyTo(bytes,0);
        for(var i=0;i<3;i++) { using var response=await Upload(client,"/ABCRetail/UploadImage","imageFile",$"quota-{i}.png",bytes);Need(response.StatusCode==HttpStatusCode.Redirect,"Upload below the quota failed."); }
        using var rejected=await Upload(client,"/ABCRetail/UploadImage","imageFile","overflow.png",png);
        Need(rejected.StatusCode==HttpStatusCode.OK && (await rejected.Content.ReadAsStringAsync()).Contains("validation-summary-errors"),"Cumulative quota was not enforced.");
        Need(!(await client.GetStringAsync("/ABCRetail/ListImages")).Contains("overflow.png"),"Rejected upload left a file record.");
    });
    Console.WriteLine($"ABC Retail checks passed: {passed}. Browser and Docker acceptance remain separate.");
    await app.StopAsync();
}
catch(Exception ex) { Console.Error.WriteLine($"FAIL after {passed} checks: {ex}");Environment.ExitCode=1; }
finally { try { Directory.Delete(fixture,true); } catch(IOException) { } catch(UnauthorizedAccessException) { } }

sealed class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; }=DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow()=>Now;
}
sealed class CookieJar(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    private readonly CookieContainer _cookies=new();
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
    {
        var uri=request.RequestUri ?? throw new InvalidOperationException("Request URI missing.");var header=_cookies.GetCookieHeader(uri);
        if(header.Length>0)request.Headers.TryAddWithoutValidation("Cookie",header);
        var response=await base.SendAsync(request,cancellationToken);
        if(response.Headers.TryGetValues("Set-Cookie",out var values))foreach(var value in values)_cookies.SetCookies(uri,value);
        return response;
    }
}
