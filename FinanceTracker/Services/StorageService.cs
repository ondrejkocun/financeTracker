using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Semestralka.Models;

namespace Semestralka.Services;

/// <summary>
/// Handles loading and saving application profiles with optional AES-GCM encryption
/// and OneDrive-aware storage paths.
/// </summary>
public static class StorageService
{
    /// <summary>
    /// Gets the primary storage path (OneDrive when available, otherwise Documents).
    /// </summary>
    public static readonly string DefaultFilePath = ResolveDefaultFilePath();
    private static readonly string LocalBackupFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "wallet_data.json");
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    private static string? _sessionPassword;

    private static string ResolveDefaultFilePath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var oneDrivePath = Path.Combine(userProfile, "OneDrive", "wallet_data.json");
        if (Directory.Exists(Path.GetDirectoryName(oneDrivePath))) return oneDrivePath;
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "wallet_data.json");
    }

    private sealed class EncryptedPayload
    {
        public bool IsEncrypted { get; set; }
        public string Salt { get; set; } = string.Empty;
        public string Nonce { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Cipher { get; set; } = string.Empty;
    }

    /// <summary>
    /// Sets the in-memory session password used for encrypting/decrypting profiles.
    /// </summary>
    public static void SetSessionPassword(string? password)
    {
        _sessionPassword = string.IsNullOrWhiteSpace(password) ? null : password;
    }

    /// <summary>
    /// Attempts to unlock an encrypted profile using the provided password.
    /// </summary>
    public static bool TryUnlockEncryptedProfile(string password, string? filePath = null)
    {
        filePath ??= DefaultFilePath;
        if (!File.Exists(filePath)) return false;
        try
        {
            var json = File.ReadAllText(filePath);
            if (!TryGetEncryptedPayload(json, out var payload)) return false;
            _ = DecryptJson(payload, password);
            _sessionPassword = password;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determines whether the stored file contains encrypted payload data.
    /// </summary>
    public static bool IsEncryptedFile(string? filePath = null)
    {
        filePath ??= DefaultFilePath;
        if (!File.Exists(filePath)) return false;
        try
        {
            var json = File.ReadAllText(filePath);
            return TryGetEncryptedPayload(json, out _);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Saves the profile, encrypting it if a password is provided, and creates a local backup.
    /// </summary>
    public static void SaveProfile(AppProfile profile, string? filePath = null, string? password = null)
    {
        filePath ??= DefaultFilePath;
        var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
        password ??= _sessionPassword;
        if (string.IsNullOrWhiteSpace(password))
        {
            File.WriteAllText(filePath, json);
            if (!string.Equals(filePath, LocalBackupFilePath, StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(LocalBackupFilePath, json);
            }
            return;
        }

        var encrypted = EncryptJson(json, password);
        File.WriteAllText(filePath, encrypted);
        if (!string.Equals(filePath, LocalBackupFilePath, StringComparison.OrdinalIgnoreCase))
        {
            File.WriteAllText(LocalBackupFilePath, encrypted);
        }
    }

    /// <summary>
    /// Loads the profile from storage, decrypting it when needed and migrating legacy data.
    /// </summary>
    public static AppProfile LoadProfile(string? filePath = null, string? password = null, bool throwOnDecryptFailure = false)
    {
        filePath ??= DefaultFilePath;
        if (!File.Exists(filePath)) return new AppProfile();
        try
        {
            var json = File.ReadAllText(filePath);
            if (TryGetEncryptedPayload(json, out var payload))
            {
                password ??= _sessionPassword;
                if (string.IsNullOrWhiteSpace(password)) return new AppProfile();
                try
                {
                    json = DecryptJson(payload, password);
                }
                catch (Exception) when (!throwOnDecryptFailure)
                {
                    return new AppProfile();
                }
                _sessionPassword = password;
            }

            if (json.Contains("\"Wallets\""))
            {
                var profile = JsonSerializer.Deserialize<AppProfile>(json) ?? new AppProfile();
                foreach (var w in profile.Wallets) w.ProcessRecurringTransactions();
                SaveProfile(profile, filePath, password);
                return profile;
            }

            var oldWallet = JsonSerializer.Deserialize<Wallet>(json) ?? new Wallet();
            oldWallet.Name = "Osobný účet";
            oldWallet.ProcessRecurringTransactions();
            var prof = new AppProfile { Wallets = new List<Wallet> { oldWallet } };
            SaveProfile(prof, filePath, password);
            return prof;
        }
        catch
        {
            if (throwOnDecryptFailure) throw;
            return new AppProfile();
        }
    }

    /// <summary>
    /// Saves a single wallet into the profile.
    /// </summary>
    public static void SaveWallet(Wallet wallet, string? filePath = null)
    {
        var profile = LoadProfile(filePath);
        var idx = profile.Wallets.FindIndex(w => w.Id == wallet.Id);
        if (idx >= 0) profile.Wallets[idx] = wallet;
        else profile.Wallets.Add(wallet);
        SaveProfile(profile, filePath);
    }

    /// <summary>
    /// Loads the first wallet from the profile or returns a new wallet.
    /// </summary>
    public static Wallet LoadWallet(string? filePath = null)
    {
        var profile = LoadProfile(filePath);
        return profile.Wallets.Count > 0 ? profile.Wallets[0] : new Wallet();
    }

    private static bool TryGetEncryptedPayload(string json, out EncryptedPayload payload)
    {
        payload = new EncryptedPayload();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("IsEncrypted", out var enc) || !enc.GetBoolean()) return false;
            var data = JsonSerializer.Deserialize<EncryptedPayload>(json);
            if (data == null || string.IsNullOrWhiteSpace(data.Cipher)) return false;
            payload = data;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string EncryptJson(string json, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(json);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        var payload = new EncryptedPayload
        {
            IsEncrypted = true,
            Salt = Convert.ToBase64String(salt),
            Nonce = Convert.ToBase64String(nonce),
            Tag = Convert.ToBase64String(tag),
            Cipher = Convert.ToBase64String(cipherBytes)
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string DecryptJson(EncryptedPayload payload, string password)
    {
        var salt = Convert.FromBase64String(payload.Salt);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        var nonce = Convert.FromBase64String(payload.Nonce);
        var tag = Convert.FromBase64String(payload.Tag);
        var cipher = Convert.FromBase64String(payload.Cipher);
        var plain = new byte[cipher.Length];

        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Decrypt(nonce, cipher, tag, plain);
        }

        return Encoding.UTF8.GetString(plain);
    }
}

