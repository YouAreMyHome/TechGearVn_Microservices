using Product.Domain.Common;

namespace Product.Domain.Events
{
    /// Domain Event: Thông tin chi tiết sản phẩm đã thay đổi
    /// Event này trigger khi:
    /// - Tên sản phẩm thay đổi (ảnh hưởng SEO)
    /// - Mô tả thay đổi (content marketing)
    /// - Thông tin quan trọng khác thay đổi
    public record ProductDetailsChangedEvent(
         Guid ProductId,
         string OldName,
         string NewName,
         string UpdatedBy
     ) : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;

        /// Kiểm tra có thay đổi tên không
        public bool NameChanged => !string.Equals(OldName, NewName, StringComparison.OrdinalIgnoreCase);
    }
}