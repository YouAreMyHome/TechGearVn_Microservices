using Product.Domain.Common;

namespace Product.Domain.Events
{
    /// Domain Event: Sản phẩm sắp hết hàng (low stock)
    /// Cảnh báo quan trọng cho business operations:
    /// - Tự động tạo purchase order
    /// - Notify procurement team
    /// - Update product visibility
    /// - Trigger supplier communication
    public record ProductLowStockEvent(
         Guid ProductId,
         string ProductName,
         string ProductSku,
         int CurrentStock,
         string UpdatedBy) : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;


        ///Mức độ quan trọng của Low Stock
        public StockLevel Severity => CurrentStock switch
        {
            0 => StockLevel.OutOfStock, //Hết hàng
            <= 3 => StockLevel.Critical, //Nguy cấp
            <= 10 => StockLevel.Low, //Thấp
            _ => StockLevel.Normal //Bình thường
        };
        //Enum định nghĩa mức độ tồn kho
        public enum StockLevel
        {
            OutOfStock,
            Critical,
            Low,
            Normal
        }
    }
}