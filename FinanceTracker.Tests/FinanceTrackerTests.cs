using Semestralka.Models;
using Semestralka.Services;
using Xunit;

namespace FinanceTracker.Tests;

public class WalletTests
{
    [Fact]
    public void GetTotalBalance_ReturnsIncomeMinusExpenses()
    {
        var wallet = new Wallet();
        wallet.AddTransaction(new Transaction { Amount = 2_000m, Type = TransactionType.Income, Category = "Salary" });
        wallet.AddTransaction(new Transaction { Amount = 350.50m, Type = TransactionType.Expense, Category = "Rent" });

        Assert.Equal(1_649.50m, wallet.GetTotalBalance());
    }

    [Fact]
    public void AddTransaction_AddsUnknownCategory()
    {
        var wallet = new Wallet();
        var transaction = new Transaction { Amount = 10m, Type = TransactionType.Expense, Category = "Books" };

        wallet.AddTransaction(transaction);

        Assert.Contains("Books", wallet.Categories);
        Assert.Contains(transaction, wallet.Transactions);
    }

    [Fact]
    public void GetTransactionsByMonth_ReturnsOnlyRequestedMonth()
    {
        var wallet = new Wallet();
        wallet.Transactions.Add(new Transaction { Date = new DateTime(2026, 5, 10), Amount = 1m });
        wallet.Transactions.Add(new Transaction { Date = new DateTime(2026, 6, 10), Amount = 2m });

        var result = wallet.GetTransactionsByMonth(2026, 5).ToList();

        var transaction = Assert.Single(result);
        Assert.Equal(1m, transaction.Amount);
    }
}

public class SecurityHelperTests
{
    [Fact]
    public void HashPassword_IsDeterministicButDoesNotExposePassword()
    {
        var hash = SecurityHelper.HashPassword("correct horse battery staple");

        Assert.NotEqual("correct horse battery staple", hash);
        Assert.Equal(hash, SecurityHelper.HashPassword("correct horse battery staple"));
    }

    [Fact]
    public void VerifyPassword_RejectsDifferentPassword()
    {
        var hash = SecurityHelper.HashPassword("secret");

        Assert.True(SecurityHelper.VerifyPassword("secret", hash));
        Assert.False(SecurityHelper.VerifyPassword("wrong", hash));
    }
}

public class ExportImportServiceTests
{
    [Fact]
    public void ExportAndImport_RoundTripsTransactions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"finance-tracker-{Guid.NewGuid():N}.xlsx");
        try
        {
            var original = new[]
            {
                new Transaction
                {
                    Amount = 42.75m,
                    Category = "Food",
                    Description = "Lunch",
                    Date = new DateTime(2026, 10, 3),
                    Type = TransactionType.Expense,
                    IsRecurring = true
                }
            };

            ExportImportService.ExportToExcel(original, path);
            var imported = ExportImportService.ImportFromExcel(path);

            var transaction = Assert.Single(imported);
            Assert.Equal(42.75m, transaction.Amount);
            Assert.Equal("Food", transaction.Category);
            Assert.Equal("Lunch", transaction.Description);
            Assert.Equal(new DateTime(2026, 10, 3), transaction.Date);
            Assert.Equal(TransactionType.Expense, transaction.Type);
            Assert.True(transaction.IsRecurring);
            Assert.Equal(transaction.Date, transaction.LastProcessed);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
