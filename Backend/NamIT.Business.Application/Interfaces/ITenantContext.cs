namespace NamIT.Business.Application.Interfaces;

/// <summary>
/// Cung cấp TenantId của request hiện tại, được resolve từ JWT claims ở tầng API/Middleware.
/// TUYỆT ĐỐI không lấy TenantId từ query string / body do frontend gửi lên.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    Guid? UserId { get; }
    bool IsSuperAdmin { get; }
    bool HasTenant { get; }
}
