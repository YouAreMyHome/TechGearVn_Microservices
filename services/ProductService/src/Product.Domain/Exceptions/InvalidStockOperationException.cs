namespace Product.Domain.Exceptions;

/// <summary>
/// Exception khi thao tác stock không hợp lệ
/// Ví dụ: bán nhiều hơn số lượng có sẵn
/// </summary>
public class InvalidStockOperationException : DomainException
{
    public Guid ProductId { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public InvalidStockOperationException(
        Guid productId,
        int requestedQuantity,
        int availableQuantity)
        : base($"Không thể thực hiện thao tác stock. Yêu cầu: {requestedQuantity}, Có sẵn: {availableQuantity}")
    {
        ProductId = productId;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
    }
}