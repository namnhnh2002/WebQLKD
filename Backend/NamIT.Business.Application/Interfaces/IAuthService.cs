using NamIT.Business.Application.DTOs;

namespace NamIT.Business.Application.Interfaces;

public interface IAuthService
{
    Task<Guid> RegisterTenantAsync(RegisterTenantRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(Guid userId, string refreshToken);
}
