using System;

namespace Semestralka.Models;

/// <summary>
/// Represents a single financial transaction.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Transaction identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Monetary amount of the transaction.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Category label.
    /// </summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>
    /// Optional description text.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Date when the transaction occurred.
    /// </summary>
    public DateTime Date { get; set; } = DateTime.Now;
    /// <summary>
    /// Transaction type (income or expense).
    /// </summary>
    public TransactionType Type { get; set; }
    /// <summary>
    /// Indicates whether the transaction is recurring.
    /// </summary>
    public bool IsRecurring { get; set; }
    /// <summary>
    /// Last processed date for recurring transactions.
    /// </summary>
    public DateTime? LastProcessed { get; set; }
}

