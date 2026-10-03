using System;
using System.Security.Cryptography;
using System.Text;

namespace Semestralka.Services;

/// <summary>
/// Provides basic password hashing and verification helpers.
/// </summary>
public static class SecurityHelper
{
    /// <summary>
    /// Hashes a password using SHA-256 and a fixed salt string.
    /// </summary>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) return string.Empty;
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password + "S3cr3t_S4lt");
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Verifies the input password against a stored hash.
    /// </summary>
    public static bool VerifyPassword(string input, string hash)
    {
        if (string.IsNullOrEmpty(hash)) return true;
        return HashPassword(input) == hash;
    }
}
