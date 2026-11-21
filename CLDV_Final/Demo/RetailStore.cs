using ABCRetail.Models;
using System.Text;

namespace ABCRetail.Demo;
public sealed class RetailException(string message) : Exception(message);
public sealed class RetailStore(DemoContext context, TimeProvider clock)
{
    public Workspace Data => context.Current;
    public CustomerEntity? Customer(string id) => Data.Customers.FirstOrDefault(x => x.Id == id);
    public ProductEntity? Product(string id) => Data.Products.FirstOrDefault(x => x.Id == id);
    public OrderEntity? Order(string id) => Data.Orders.FirstOrDefault(x => x.Id == id);
    public void Event(string description)
    {
        if (Data.Events.Count == 60) Data.Events.RemoveAt(0);
        Data.Events.Add(new() { Sequence = ++Data.NextSequence, Description = description, CreatedAt = clock.GetUtcNow() });
    }
    private static void Current(int actual, int submitted)
    { if (actual != submitted) throw new RetailException("This record changed in another tab. Reload it before saving again."); }
    public void SaveCustomer(CustomerForm form, CustomerEntity? existing)
    {
        var name = form.Name.Trim(); var email = form.Email.Trim();
        if (Data.Customers.Any(x => x.Id != existing?.Id && string.Equals(x.Email,email,StringComparison.OrdinalIgnoreCase)))
            throw new RetailException("That email already belongs to a customer in your demo.");
        if (existing is null && Data.Customers.Count >= 50) throw new RetailException("This demo allows 50 customers. Remove an unused customer or reset the demo.");
        if (existing is not null) Current(existing.Version,form.Version);
        var customer = existing ?? new CustomerEntity(); customer.Name = name; customer.Email = email;
        if (existing is null) Data.Customers.Add(customer); else customer.Version++;
        Event(existing is null ? "Customer added" : "Customer updated");
    }
    public async Task<StoredFile?> ReadFile(IFormFile? file, bool image)
    {
        if (file is null || file.Length == 0) return null;
        if (file.Length > 2 * 1024 * 1024) throw new RetailException("Choose a file no larger than 2 MB.");
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var bytes = stream.ToArray();
        if (bytes.Length > 2 * 1024 * 1024) throw new RetailException("Choose a file no larger than 2 MB.");
        string type; string extension;
        if (image)
        {
            if (bytes.AsSpan().StartsWith(new byte[] {137,80,78,71,13,10,26,10})) { type = "image/png"; extension = ".png"; }
            else if (bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[^2] == 255 && bytes[^1] == 217) { type = "image/jpeg"; extension = ".jpg"; }
            else if (bytes.Length >= 12 && Encoding.ASCII.GetString(bytes,0,4) == "RIFF" && Encoding.ASCII.GetString(bytes,8,4) == "WEBP") { type = "image/webp"; extension = ".webp"; }
            else throw new RetailException("Choose a PNG, JPEG or WebP image. SVG and other file types are not accepted.");
        }
        else
        {
            if (bytes.Length < 12 || !Encoding.ASCII.GetString(bytes,0,5).Equals("%PDF-",StringComparison.Ordinal) ||
                !Encoding.ASCII.GetString(bytes,Math.Max(0,bytes.Length-1024),Math.Min(1024,bytes.Length)).Contains("%%EOF",StringComparison.Ordinal))
                throw new RetailException("Choose a PDF contract with a valid PDF header and end marker.");
            type = "application/pdf"; extension = ".pdf";
        }
        var name = Path.GetFileName(file.FileName.Replace('\\','/'));
        name = new string(name.Where(c => !char.IsControl(c) && c != '"').Take(90).ToArray());
        if (string.IsNullOrWhiteSpace(name)) name = "sample" + extension;
        // The generated filename controls the extension, regardless of the submitted name.
        name = Path.GetFileNameWithoutExtension(name) + extension;
        return new(Guid.NewGuid().ToString("N"),name,type,bytes,image);
    }
    private void FileRoom(StoredFile? file, string? replacing = null)
    {
        if (file is null) return;
        var kept = Data.Files.Where(x => x.Id != replacing).ToArray();
        if (kept.Length >= 20 || kept.Sum(x => (long)x.Bytes.Length) + file.Bytes.Length > 6 * 1024 * 1024)
            throw new RetailException("Your demo file allowance is full (20 files / 6 MB). Remove files or reset the demo.");
    }
    public void SaveProduct(ProductForm form, ProductEntity? existing, StoredFile? image)
    {
        var name = form.Name.Trim();
        if (decimal.Round(form.Price,2) != form.Price) throw new RetailException("Use a price with no more than two decimal places.");
        if (Data.Products.Any(x => x.Id != existing?.Id && string.Equals(x.Name,name,StringComparison.OrdinalIgnoreCase))) throw new RetailException("A product already uses that name.");
        if (existing is null && Data.Products.Count >= 50) throw new RetailException("This demo allows 50 products. Remove an unused product or reset the demo.");
        if (existing is not null) Current(existing.Version,form.Version);
        FileRoom(image,existing?.ImageId);
        var product = existing ?? new ProductEntity(); product.Name = name; product.Description = form.Description.Trim(); product.Price = form.Price;
        if (image is not null)
        {
            Data.Files.RemoveAll(x => x.Id == product.ImageId); Data.Files.Add(image); product.ImageId = image.Id;
        }
        if (existing is null) Data.Products.Add(product); else product.Version++;
        Event(existing is null ? "Product added" : "Product updated");
    }
    public void SaveOrder(OrderForm form, OrderEntity? existing)
    {
        var customer = Customer(form.CustomerId) ?? throw new RetailException("Choose a customer from this demo workspace.");
        var product = Product(form.ProductId) ?? throw new RetailException("Choose a product from this demo workspace.");
        if (form.OrderDate.Date < DateTime.UtcNow.Date.AddYears(-1) || form.OrderDate.Date > DateTime.UtcNow.Date.AddDays(30))
            throw new RetailException("Choose an order date between one year ago and 30 days from today.");
        if (existing is null && Data.Orders.Count >= 100) throw new RetailException("This demo allows 100 orders. Remove an order or reset the demo.");
        if (existing is not null) Current(existing.Version,form.Version);
        var order = existing ?? new OrderEntity();
        // Quantity edits keep the recorded unit price; choosing another product uses its saved price.
        if (existing is null || order.ProductId != product.Id) order.UnitPrice = product.Price;
        order.CustomerId = customer.Id; order.CustomerName = customer.Name; order.ProductId = product.Id; order.ProductName = product.Name;
        order.Quantity = form.Quantity; order.OrderDate = form.OrderDate.Date;
        if (existing is null) Data.Orders.Add(order); else order.Version++;
        Event(existing is null ? "Order recorded" : "Order updated");
    }
    public void DeleteCustomer(CustomerEntity customer, int version)
    {
        Current(customer.Version,version);
        if (Data.Orders.Any(x => x.CustomerId == customer.Id)) throw new RetailException("This customer has orders. Remove those demo orders first.");
        Data.Customers.Remove(customer); Event("Customer removed");
    }
    public void DeleteProduct(ProductEntity product, int version)
    {
        Current(product.Version,version);
        if (Data.Orders.Any(x => x.ProductId == product.Id)) throw new RetailException("This product has orders. Remove those demo orders first.");
        Data.Products.Remove(product); Data.Files.RemoveAll(x => x.Id == product.ImageId); Event("Product removed");
    }
    public void DeleteOrder(OrderEntity order, int version)
    { Current(order.Version,version); Data.Orders.Remove(order); Event("Order removed"); }
    public void AddFile(StoredFile file) { FileRoom(file); Data.Files.Add(file); Event(file.IsImage ? "Image uploaded" : "Contract uploaded"); }
    public void DeleteFile(StoredFile file)
    {
        foreach (var product in Data.Products.Where(x => x.ImageId == file.Id)) { product.ImageId = null; product.Version++; }
        Data.Files.Remove(file); Event(file.IsImage ? "Image removed" : "Contract removed");
    }
    public void ProcessNext()
    {
        var pending = Data.Events.FirstOrDefault(x => x.ProcessedAt is null);
        if (pending is not null) pending.ProcessedAt = clock.GetUtcNow();
    }
}
