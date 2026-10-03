using System;
using System.Linq;
using Semestralka.Models;
using Semestralka.Services;

namespace Semestralka.CLI;

/// <summary>
/// Main class for the console application that manages personal finances.
/// Provides a simple command-line interface (CLI) for working with transactions and export.
/// </summary>
class Program
{
    /// <summary>
    /// Program entry point.
    /// Parses command-line arguments and executes the corresponding command.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code: 0 on success, non-zero on error.</returns>
    static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            PrintHelp();
            return 0;
        }

        var command = args[0].ToLower();

        try
        {
            switch (command)
            {
                case "add":
                    return HandleAdd(args);
                case "list":
                    return HandleList(args);
                case "delete":
                    return HandleDelete(args);
                case "summary":
                    return HandleSummary(args);
                case "export":
                    return HandleExport(args);
                case "categories":
                    return HandleCategories(args);
                default:
                    Console.WriteLine($"Neznámy príkaz: {command}");
                    PrintHelp();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Writes application help text to the console.
    /// </summary>
    private static void PrintHelp()
    {
        Console.WriteLine("Správca osobných financií (CLI aplikácia)");
        Console.WriteLine("Použitie:");
        Console.WriteLine("  add --amount <čiastka> --category <text> [--desc <text>] --type <Income|Expense>");
        Console.WriteLine("  list");
        Console.WriteLine("  delete --id <guid>");
        Console.WriteLine("  summary [--month <1-12>] [--year <rok>]");
        Console.WriteLine("  export [--file <nazov_suboru.xlsx>]");
        Console.WriteLine("  categories");
    }

    /// <summary>
    /// Handles the "add" command and adds a new transaction to the wallet.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code: 0 on success, 1 on error or missing parameters.</returns>
    private static int HandleAdd(string[] args)
    {
        decimal amount = 0;
        string? category = null;
        string desc = string.Empty;
        TransactionType type = TransactionType.Expense;
        bool hasAmount = false;
        bool hasCategory = false;
        bool hasType = false;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--amount" && i + 1 < args.Length)
            {
                if (decimal.TryParse(args[++i], out amount)) hasAmount = true;
                else throw new ArgumentException("Neplatná suma.");
            }
            else if (args[i] == "--category" && i + 1 < args.Length)
            {
                category = args[++i];
                hasCategory = true;
            }
            else if (args[i] == "--desc" && i + 1 < args.Length)
            {
                desc = args[++i];
            }
            else if (args[i] == "--type" && i + 1 < args.Length)
            {
                if (Enum.TryParse(args[++i], true, out type)) hasType = true;
                else throw new ArgumentException("Neplatný typ transakcie. Zvoľte Income alebo Expense.");
            }
        }

        if (!hasAmount || !hasCategory || !hasType)
        {
            Console.WriteLine("Chýbajú povinné parametre pre 'add'.");
            return 1;
        }

        var wallet = StorageService.LoadWallet();
        wallet.AddTransaction(new Transaction
        {
            Amount = amount,
            Category = category!,
            Description = desc,
            Type = type,
            Date = DateTime.Now
        });
        StorageService.SaveWallet(wallet);

        Console.WriteLine($"[+] Transakcia pridaná! Aktuálny zostatok: {wallet.GetTotalBalance()} EUR");
        return 0;
    }

    /// <summary>
    /// Handles the "list" command and prints the list of transactions.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code (always 0 for success).</returns>
    private static int HandleList(string[] args)
    {
        var wallet = StorageService.LoadWallet();
        Console.WriteLine("--- Zoznam transakcií ---");
        foreach (var t in wallet.Transactions.OrderByDescending(x => x.Date))
        {
            var symbol = t.Type == TransactionType.Income ? "+" : "-";
            Console.WriteLine($"{t.Id} | {t.Date:d} | {symbol}{t.Amount:C} | {t.Category} | {t.Description}");
        }
        return 0;
    }

    /// <summary>
    /// Handles the "delete" command and removes a transaction by ID.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code: 0 on success, 1 on invalid format.</returns>
    private static int HandleDelete(string[] args)
    {
        if (args.Length < 3 || args[1] != "--id" || !Guid.TryParse(args[2], out Guid id))
        {
            Console.WriteLine("Na zmazanie použite formát: delete --id <guid>");
            return 1;
        }

        var wallet = StorageService.LoadWallet();
        wallet.RemoveTransaction(id);
        StorageService.SaveWallet(wallet);
        Console.WriteLine($"[i] Transakcia s ID {id} bola zmazaná (ak existovala).");
        return 0;
    }

    /// <summary>
    /// Handles the "summary" command and prints income/expense summary for a given month/year.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code (always 0 for success).</returns>
    private static int HandleSummary(string[] args)
    {
        int month = DateTime.Now.Month;
        int year = DateTime.Now.Year;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--month" && i + 1 < args.Length)
            {
                int.TryParse(args[++i], out month);
            }
            else if (args[i] == "--year" && i + 1 < args.Length)
            {
                int.TryParse(args[++i], out year);
            }
        }

        var wallet = StorageService.LoadWallet();
        var trans = wallet.GetTransactionsByMonth(year, month).ToList();
        var income = trans.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expense = trans.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        Console.WriteLine($"--- Súhrn za {month}/{year} ---");
        Console.WriteLine($"Príjmy:  {income,8:C}");
        Console.WriteLine($"Výdavky: {expense,8:C}");
        Console.WriteLine($"Rozdiel: {(income - expense),8:C}");
        Console.WriteLine($"---------------------------");
        Console.WriteLine($"Celkový zostatok: {wallet.GetTotalBalance():C}");
        return 0;
    }

    /// <summary>
    /// Handles the "export" command and exports transactions to an Excel file.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code (always 0 for success).</returns>
    private static int HandleExport(string[] args)
    {
        string file = "export.xlsx";

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--file" && i + 1 < args.Length)
            {
                file = args[++i];
            }
        }

        var wallet = StorageService.LoadWallet();
        ExportImportService.ExportToExcel(wallet.Transactions, file);
        Console.WriteLine($"[i] Údaje úspešne exportované do {file}");
        return 0;
    }

    /// <summary>
    /// Handles the "categories" command and prints available categories.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code (always 0 for success).</returns>
    private static int HandleCategories(string[] args)
    {
        var wallet = StorageService.LoadWallet();
        Console.WriteLine("--- Dostupné kategórie ---");
        foreach (var cat in wallet.Categories)
        {
            Console.WriteLine($"- {cat}");
        }
        return 0;
    }
}
