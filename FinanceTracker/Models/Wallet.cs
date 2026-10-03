using System;
using System.Collections.Generic;
using System.Linq;

namespace Semestralka.Models;

/// <summary>
/// Represents a wallet with transactions, categories, and budgets.
/// </summary>
public class Wallet
{
    /// <summary>
    /// Wallet identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Wallet display name.
    /// </summary>
    public string Name { get; set; } = "Osobný účet";
    /// <summary>
    /// List of recorded transactions.
    /// </summary>
    public List<Transaction> Transactions { get; set; } = new();
    /// <summary>
    /// Available categories for transactions.
    /// </summary>
    public List<string> Categories { get; set; } = new() { "Jedlo", "Nákupy", "Bývanie", "Doprava", "Zábava", "Výplata" };
    /// <summary>
    /// Per-category budget limits.
    /// </summary>
    public Dictionary<string, decimal> CategoryBudgets { get; set; } = new();
    /// <summary>
    /// Monthly budget limit for all expenses.
    /// </summary>
    public decimal? MonthlyBudget { get; set; }

    /// <summary>
    /// Adds a transaction and registers a new category if needed.
    /// </summary>
    public void AddTransaction(Transaction transaction)
    {
        if (!Categories.Contains(transaction.Category))
        {
            Categories.Add(transaction.Category);
        }
        Transactions.Add(transaction);
    }

    /// <summary>
    /// Removes a transaction by identifier.
    /// </summary>
    public void RemoveTransaction(Guid id)
    {
        Transactions.RemoveAll(t => t.Id == id);
    }

    /// <summary>
    /// Computes the current balance.
    /// </summary>
    public decimal GetTotalBalance()
    {
        var income = Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expense = Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        return income - expense;
    }

    /// <summary>
    /// Returns transactions for a specific year and month.
    /// </summary>
    public IEnumerable<Transaction> GetTransactionsByMonth(int year, int month)
    {
        return Transactions.Where(t => t.Date.Year == year && t.Date.Month == month);
    }

    /// <summary>
    /// Generates missing occurrences of recurring transactions.
    /// </summary>
    public void ProcessRecurringTransactions()
    {
        var recurring = Transactions.Where(t => t.IsRecurring).ToList();
        foreach (var req in recurring)
        {
            if (!req.LastProcessed.HasValue) req.LastProcessed = req.Date;
            while (req.LastProcessed.Value.AddMonths(1) <= DateTime.Now)
            {
                req.LastProcessed = req.LastProcessed.Value.AddMonths(1);
                var newT = new Transaction
                {
                    Amount = req.Amount,
                    Category = req.Category,
                    Description = req.Description + " (Opakujúca platba)",
                    Type = req.Type,
                    Date = req.LastProcessed.Value,
                    IsRecurring = false
                };
                Transactions.Add(newT);
            }
        }
    }
}

