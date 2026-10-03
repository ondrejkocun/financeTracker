using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Semestralka.Models;

namespace Semestralka.Services;

/// <summary>
/// Provides Excel export/import helpers for transactions.
/// </summary>
public static class ExportImportService
{
    /// <summary>
    /// Exports transactions to an Excel workbook (.xlsx).
    /// </summary>
    public static void ExportToExcel(IEnumerable<Transaction> transactions, string filePath)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Transactions");
        sheet.Cell(1, 1).Value = "Date";
        sheet.Cell(1, 2).Value = "Amount";
        sheet.Cell(1, 3).Value = "Category";
        sheet.Cell(1, 4).Value = "Description";
        sheet.Cell(1, 5).Value = "Type";
        sheet.Cell(1, 6).Value = "IsRecurring";

        var row = 2;
        foreach (var t in transactions)
        {
            sheet.Cell(row, 1).Value = t.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sheet.Cell(row, 2).Value = t.Amount;
            sheet.Cell(row, 3).Value = t.Category;
            sheet.Cell(row, 4).Value = t.Description;
            sheet.Cell(row, 5).Value = t.Type.ToString();
            sheet.Cell(row, 6).Value = t.IsRecurring;
            row++;
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    /// <summary>
    /// Imports transactions from an Excel workbook (.xlsx).
    /// </summary>
    public static List<Transaction> ImportFromExcel(string filePath)
    {
        var result = new List<Transaction>();
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheet(1);
        var rows = sheet.RowsUsed().Skip(1);
        foreach (var row in rows)
        {
            var dateText = row.Cell(1).GetString();
            var amountText = row.Cell(2).GetString();
            var category = row.Cell(3).GetString();
            var description = row.Cell(4).GetString();
            var typeText = row.Cell(5).GetString();
            var recurringText = row.Cell(6).GetString();

            if (!DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                date = DateTime.Now;
            if (!decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) &&
                !decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                continue;

            var type = TransactionType.Expense;
            if (Enum.TryParse(typeText, true, out TransactionType parsedType))
                type = parsedType;

            var isRecurring = bool.TryParse(recurringText, out var r) && r;

            result.Add(new Transaction
            {
                Amount = amount,
                Category = string.IsNullOrWhiteSpace(category) ? "Iné" : category,
                Description = description,
                Type = type,
                Date = date,
                IsRecurring = isRecurring,
                LastProcessed = isRecurring ? date : null
            });
        }

        return result;
    }
}
