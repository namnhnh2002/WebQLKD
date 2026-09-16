using NamIT.Business.Domain.Common;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Domain.Entities;

/// <summary>
/// Tài khoản đăng nhập. Thuộc về đúng một Tenant (trừ SUPER_ADMIN có thể TenantId = null).
/// </summary>
public class User : BaseEntity, ITenantEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!; // KHÔNG bao giờ lưu plain text
    public string? Phone { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

/// <summary>
/// Bảng trung gian User <-> Branch (một nhân viên có thể thuộc nhiều chi nhánh).
/// </summary>
public class UserBranch : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = default!;
}

/// <summary>
/// Refresh token cho cơ chế JWT access + refresh.
/// Token được hash trước khi lưu; không log token dạng plain text.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public string TokenHash { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}
