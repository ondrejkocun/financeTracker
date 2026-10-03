using System.Collections.Generic;

namespace Semestralka.Models;

/// <summary>
/// Root profile that stores user settings and wallets.
/// </summary>
public class AppProfile
{
    /// <summary>
    /// Stored password hash for app unlock.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    /// <summary>
    /// UI theme selection (Light/Dark).
    /// </summary>
    public string Theme { get; set; } = "Light";
    /// <summary>
    /// Collection of user wallets.
    /// </summary>
    public List<Wallet> Wallets { get; set; } = new() { new Wallet { Name = "Osobný účet" } };
}
