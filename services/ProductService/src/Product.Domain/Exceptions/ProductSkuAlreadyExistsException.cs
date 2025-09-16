namespace Product.Domain.Exceptions;

/// <summary>
/// Exception khi SKU đã tồn tại trong hệ thống
/// SKU phải unique để đảm bảo inventory management chính xác
/// </summary>
public class ProductSkuAlreadyExistsException : Exception
{
    public string Sku { get; }

    public ProductSkuAlreadyExistsException(string sku)
        : base($"SKU '{sku}' đã tồn tại trong hệ thống")
    {
        Sku = sku;
    }

    public ProductSkuAlreadyExistsException(string sku, string message)
        : base(message)
    {
        Sku = sku;
    }

    public ProductSkuAlreadyExistsException(string sku, string message, Exception innerException)
        : base(message, innerException)
    {
        Sku = sku;
    }
}