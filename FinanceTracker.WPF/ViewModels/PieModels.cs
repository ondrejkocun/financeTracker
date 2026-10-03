using System.Windows.Media;

namespace Semestralka.WPF.ViewModels;

/// <summary>
/// Represents one slice of the pie chart.
/// </summary>
public class PieSliceDto
{
    /// <summary>
    /// Starting angle of the slice in degrees.
    /// </summary>
    public double StartAngle { get; set; }

    /// <summary>
    /// Sweep angle of the slice in degrees.
    /// </summary>
    public double SweepAngle { get; set; }

    /// <summary>
    /// Fill brush used to draw the slice.
    /// </summary>
    public Brush Fill { get; set; } = Brushes.Transparent;
}

/// <summary>
/// Represents one legend item for the pie chart.
/// </summary>
public class LegendItemDto
{
    /// <summary>
    /// Text displayed in the legend.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Brush displayed near the legend title.
    /// </summary>
    public Brush ColorBrush { get; set; } = Brushes.Transparent;
}

/// <summary>
/// Represents computed budget status for one category.
/// </summary>
public class BudgetStatusDto
{
    /// <summary>
    /// Budget category name.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Planned budget value.
    /// </summary>
    public decimal Budget { get; set; }

    /// <summary>
    /// Already spent value.
    /// </summary>
    public decimal Spent { get; set; }

    /// <summary>
    /// Remaining amount in the budget.
    /// </summary>
    public decimal Remaining => Budget - Spent;

    /// <summary>
    /// Indicates whether the budget was exceeded.
    /// </summary>
    public bool IsOverBudget => Remaining < 0;

    /// <summary>
    /// Human-readable budget status text.
    /// </summary>
    public string StatusText => IsOverBudget ? $"Prečerpané o {Math.Abs(Remaining):F2} €" : $"Zostáva {Remaining:F2} €";

    /// <summary>
    /// UI brush indicating safe/over-budget state.
    /// </summary>
    public Brush StatusBrush => IsOverBudget ? Brushes.IndianRed : Brushes.SeaGreen;
}

/// <summary>
/// Represents one month summary row.
/// </summary>
public class MonthSummaryDto
{
    /// <summary>
    /// Month label in UI format.
    /// </summary>
    public string MonthLabel { get; set; } = string.Empty;

    /// <summary>
    /// Sum of incomes for the month.
    /// </summary>
    public decimal Income { get; set; }

    /// <summary>
    /// Sum of expenses for the month.
    /// </summary>
    public decimal Expense { get; set; }

    /// <summary>
    /// Net balance for the month.
    /// </summary>
    public decimal Balance => Income - Expense;
}

/// <summary>
/// Represents one overview statistic displayed in UI.
/// </summary>
public class OverviewStatDto
{
    /// <summary>
    /// Statistic title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Statistic value text.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
