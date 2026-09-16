namespace NamIT.Business.Application.DTOs;

public record RegisterTenantRequest(
    string TenantName,
    string BusinessTypeCode,
    string AdminFullName,
    string AdminEmail,
    string AdminPassword);

public record LoginRequest(string Email, string Password, string? TenantCode);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    UserProfileDto User);

public record RefreshTokenRequest(string RefreshToken);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record UserProfileDto(
    Guid Id,
    string FullName,
    string Email,
    Guid TenantId,
    string TenantName,
    List<string> Roles,
    List<string> Permissions);

public record TenantModuleContextDto(
    Guid TenantId,
    string TenantName,
    string BusinessTypeCode,
    List<string> EnabledModules,
    List<string> Permissions);
