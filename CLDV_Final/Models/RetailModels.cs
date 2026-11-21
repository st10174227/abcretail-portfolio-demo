using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace ABCRetail.Models;
public sealed class CustomerEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Version { get; set; } = 1;
}
public sealed class ProductEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string? ImageId { get; set; }
    public int Version { get; set; } = 1;
}
public sealed class OrderEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CustomerId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Total => UnitPrice * Quantity;
    public int Version { get; set; } = 1;
}
public sealed class CustomerForm
{
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public string Id { get; set; } = "";
    public int Version { get; set; }
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(160)] public string Email { get; set; } = "";
}
public sealed class ProductForm
{
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public string Id { get; set; } = "";
    public int Version { get; set; }
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [Required, StringLength(600)] public string Description { get; set; } = "";
    [Range(typeof(decimal), "0.01", "1000000")] public decimal Price { get; set; }
    [BindNever] public string? ExistingImageId { get; set; }
}
public sealed class OrderForm
{
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public string Id { get; set; } = "";
    public int Version { get; set; }
    [Required, StringLength(32), Display(Name = "Customer")] public string CustomerId { get; set; } = "";
    [Required, StringLength(32), Display(Name = "Product")] public string ProductId { get; set; } = "";
    [BindRequired, Range(1,999)] public int Quantity { get; set; } = 1;
    [BindRequired, DataType(DataType.Date), Display(Name = "Order date")] public DateTime OrderDate { get; set; } = DateTime.UtcNow.Date;
    [BindNever] public IReadOnlyList<CustomerEntity> Customers { get; set; } = [];
    [BindNever] public IReadOnlyList<ProductEntity> Products { get; set; } = [];
}
public sealed record StoredFile(string Id, string Name, string ContentType, byte[] Bytes, bool IsImage);
public sealed class QueueEvent
{
    public long Sequence { get; init; }
    public string Description { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
public sealed record Dashboard(int Customers, int Products, int Orders, int Contracts, int PendingEvents, decimal OrderTotal);
public sealed class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
