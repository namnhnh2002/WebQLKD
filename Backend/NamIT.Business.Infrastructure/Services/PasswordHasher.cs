using NamIT.Business.Application.Interfaces;

namespace NamIT.Business.Infrastructure.Services;

/// <summary>
/// Hash password bằng BCrypt (adaptive, có salt tự động). KHÔNG bao giờ lưu plain text.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
