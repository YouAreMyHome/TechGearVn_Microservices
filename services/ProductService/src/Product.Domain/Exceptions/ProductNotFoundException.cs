namespace Product.Domain.Exceptions;

/// <summary>
/// Exception khi không tìm thấy sản phẩm
/// Thường throw từ Application layer khi GetById return null
/// </summary>
public class ProductNotFoundException : DomainException
{
    public Guid ProductId { get; }

    public ProductNotFoundException(Guid productId)
        : base($"Không tìm thấy sản phẩm với ID: {productId}")
    {
        ProductId = productId;
    }

    public ProductNotFoundException(string sku)
        : base($"Không tìm thấy sản phẩm với SKU: {sku}")
    {
    }
    public ProductNotFoundException(Guid productId, string message)
        : base(message)
    {
        ProductId = productId;
    }

    public ProductNotFoundException(Guid productId, string message, Exception innerException)
        : base(message, innerException)
    {
        ProductId = productId;
    }
}