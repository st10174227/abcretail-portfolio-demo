using ABCRetail.Models;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ABCRetail.Demo;
public sealed class DemoContext { public Workspace Current { get; set; } = null!; }
public sealed class Workspace
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset ExpiresAt { get; init; }
    public int Leases { get; set; }
    public SemaphoreSlim Gate { get; } = new(1,1);
    public List<CustomerEntity> Customers { get; } = [];
    public List<ProductEntity> Products { get; } = [];
    public List<OrderEntity> Orders { get; } = [];
    public List<StoredFile> Files { get; } = [];
    public List<QueueEvent> Events { get; } = [];
    public long NextSequence { get; set; }
    public void Reset()
    {
        Customers.Clear(); Products.Clear(); Orders.Clear(); Files.Clear(); Events.Clear(); NextSequence = 0;
        Customers.AddRange([
            new() { Name = "Amina Example", Email = "amina@example.invalid" },
            new() { Name = "Luca Example", Email = "luca@example.invalid" },
            new() { Name = "Sam Example", Email = "sam@example.invalid" }
        ]);
        Products.AddRange([
            new() { Name = "Everyday tote", Description = "Reusable cotton tote with a roomy everyday fit.", Price = 149.90m },
            new() { Name = "Desk notebook", Description = "A5 notebook for notes, plans and new ideas.", Price = 89.50m },
            new() { Name = "Ceramic mug", Description = "A simple ceramic mug for a slower morning.", Price = 129.00m },
            new() { Name = "Canvas pouch", Description = "A compact zip pouch for the essentials.", Price = 79.00m }
        ]);
        Orders.Add(new() { CustomerId = Customers[0].Id, ProductId = Products[0].Id, CustomerName = Customers[0].Name, ProductName = Products[0].Name,
            UnitPrice = Products[0].Price, Quantity = 2, OrderDate = DateTime.UtcNow.Date });
    }
}
public sealed class WorkspaceRegistry(TimeProvider clock)
{
    private readonly object _sync = new();
    private readonly Dictionary<string,Workspace> _items = new(StringComparer.Ordinal);
    public int Count { get { lock (_sync) return _items.Count; } }
    public IDisposable? Enter(string? id, out Workspace? workspace)
    {
        lock (_sync)
        {
            foreach (var stale in _items.Values.Where(w => w.ExpiresAt <= clock.GetUtcNow() && w.Leases == 0).ToArray())
            { _items.Remove(stale.Id); stale.Files.Clear(); }
            if (id is null || !_items.TryGetValue(id, out workspace) || workspace.ExpiresAt <= clock.GetUtcNow())
            {
                if (_items.Count >= 20) { workspace = null; return null; }
                workspace = new() { ExpiresAt = clock.GetUtcNow().AddMinutes(45) }; workspace.Reset(); _items.Add(workspace.Id,workspace);
            }
            workspace.Leases++;
            return new Lease(this,workspace);
        }
    }
    private void Release(Workspace workspace) { lock (_sync) workspace.Leases--; }
    private sealed class Lease(WorkspaceRegistry owner, Workspace workspace) : IDisposable
    {
        private bool _disposed;
        public void Dispose() { if (!_disposed) { _disposed = true; owner.Release(workspace); } }
    }
}
public sealed class WorkspaceMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext http, WorkspaceRegistry registry, DemoContext context)
    {
        // Static assets, health probes and unmatched URLs do not consume visitor capacity.
        if (http.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() is null) { await next(http); return; }
        await http.Session.LoadAsync(http.RequestAborted);
        using var lease = registry.Enter(http.Session.GetString("Workspace"),out var workspace);
        if (lease is null || workspace is null) { http.Response.StatusCode = 503; await http.Response.WriteAsync("The demo is busy. Please try again later."); return; }
        context.Current = workspace;
        http.Session.SetString("Workspace",workspace.Id);
        http.Response.Headers.CacheControl = "no-store";
        await workspace.Gate.WaitAsync(http.RequestAborted);
        try { await next(http); }
        finally { workspace.Gate.Release(); }
    }
}
