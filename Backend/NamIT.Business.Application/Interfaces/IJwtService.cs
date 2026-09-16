using NamIT.Business.Domain.Entities;

namespace NamIT.Business.Application.Interfaces;

public interface IJwtService
{
    (string token, DateTime expiresAt) GenerateAccessToken(User user, Guid tenantId, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshTokenPlainText();
    string HashToken(string plainText);
}
