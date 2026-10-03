using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Semestralka.Models;
using Semestralka.Services;

namespace Semestralka.WPF.ViewModels;

/// <summary>
/// View model for the main application window.
/// </summary>
public class AppWindowViewModel : ObservableObject
{
    /// <summary>
    /// Raised when pie chart data needs to be redrawn.
    /// </summary>
    public Action? PieChartChanged;
    /// <summary>
    /// Wallets in the current profile.
    /// </summary>
    public ObservableCollection<Wallet> Wallets { get; } = new();
    private AppProfile _profile;

    private Wallet? _selectedWallet;
    /// <summary>
    /// Currently selected wallet.
    /// </summary>
    public Wallet? SelectedWallet
    {
        get => _selectedWallet;
        set
        {
            _selectedWallet = value;
            OnPropertyChanged();
            // update dependent collections
            UpdateDerivedCollections();
            // Notify commands that depend on SelectedWallet
            CommandManager.InvalidateRequerySuggested();
            // Rebuild pie chart data when selection changes
            RebuildPieChartData();
        }
    }

    /// <summary>
    /// Pie chart slices for the statistics view.
    /// </summary>
    public ObservableCollection<PieSliceDto> PieSlices { get; } = new();
    /// <summary>
    /// Legend items for the pie chart.
    /// </summary>
    public ObservableCollection<LegendItemDto> LegendItems { get; } = new();

    /// <summary>
    /// Transactions for the selected wallet.
    /// </summary>
    public ObservableCollection<Transaction> Transactions { get; } = new();
    /// <summary>
    /// Collection view for filtering transactions.
    /// </summary>
    public ICollectionView TransactionsView { get; }
    /// <summary>
    /// Available categories for input.
    /// </summary>
    public ObservableCollection<string> Categories { get; } = new();
    /// <summary>
    /// Categories available for filtering.
    /// </summary>
    public ObservableCollection<string> FilterCategories { get; } = new();
    /// <summary>
    /// Filterable transaction types.
    /// </summary>
    public ObservableCollection<string> FilterTypes { get; } = new() { "Všetky", "Výdavok", "Príjem" };
    private Transaction? _selectedTransaction;
    /// <summary>
    /// Currently selected transaction.
    /// </summary>
    public Transaction? SelectedTransaction
    {
        get => _selectedTransaction;
        set { _selectedTransaction = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    private string _filterText = string.Empty;
    /// <summary>
    /// Text filter for descriptions and categories.
    /// </summary>
    public string FilterText
    {
        get => _filterText;
        set { Set(ref _filterText, value); ApplyFilter(); }
    }

    private string _selectedFilterCategory = "Všetky";
    /// <summary>
    /// Selected category filter.
    /// </summary>
    public string SelectedFilterCategory
    {
        get => _selectedFilterCategory;
        set { Set(ref _selectedFilterCategory, value); ApplyFilter(); }
    }

    private string _selectedFilterType = "Všetky";
    /// <summary>
    /// Selected transaction type filter.
    /// </summary>
    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set { Set(ref _selectedFilterType, value); ApplyFilter(); }
    }

    private string _filterAmountText = string.Empty;
    /// <summary>
    /// Amount filter text.
    /// </summary>
    public string FilterAmountText
    {
        get => _filterAmountText;
        set { Set(ref _filterAmountText, value); ApplyFilter(); }
    }

    private string _budgetCategory = string.Empty;
    /// <summary>
    /// Input category for budgeting.
    /// </summary>
    public string BudgetCategory
    {
        get => _budgetCategory;
        set { Set(ref _budgetCategory, value); }
    }

    private string _budgetAmountText = string.Empty;
    /// <summary>
    /// Input amount for category budgets.
    /// </summary>
    public string BudgetAmountText
    {
        get => _budgetAmountText;
        set { Set(ref _budgetAmountText, value); }
    }

    private string _monthlyBudgetText = string.Empty;
    /// <summary>
    /// Input amount for monthly budget.
    /// </summary>
    public string MonthlyBudgetText
    {
        get => _monthlyBudgetText;
        set { Set(ref _monthlyBudgetText, value); }
    }

    private string _monthlyBudgetWarning = string.Empty;
    /// <summary>
    /// Warning text for monthly budget status.
    /// </summary>
    public string MonthlyBudgetWarning
    {
        get => _monthlyBudgetWarning;
        set => Set(ref _monthlyBudgetWarning, value);
    }

    private Brush _monthlyBudgetWarningBrush = Brushes.SeaGreen;
    /// <summary>
    /// Warning brush for monthly budget status.
    /// </summary>
    public Brush MonthlyBudgetWarningBrush
    {
        get => _monthlyBudgetWarningBrush;
        set => Set(ref _monthlyBudgetWarningBrush, value);
    }

    /// <summary>
    /// Budget status list for the UI.
    /// </summary>
    public ObservableCollection<BudgetStatusDto> BudgetStatuses { get; } = new();
    /// <summary>
    /// Monthly summary data for the UI.
    /// </summary>
    public ObservableCollection<MonthSummaryDto> MonthlySummaries { get; } = new();
    /// <summary>
    /// Overview statistics list for the UI.
    /// </summary>
    public ObservableCollection<OverviewStatDto> OverviewStats { get; } = new();
    /// <summary>
    /// Available chart modes.
    /// </summary>
    public ObservableCollection<string> ChartModes { get; } = new() { "Výdavky", "Príjmy" };
    private string _selectedChartMode = "Príjmy";
    /// <summary>
    /// Selected chart mode.
    /// </summary>
    public string SelectedChartMode
    {
        get => _selectedChartMode;
        set { Set(ref _selectedChartMode, value); RebuildPieChartData(); }
    }


    private string _amountText = string.Empty;
    /// <summary>
    /// Input amount text.
    /// </summary>
    public string AmountText { get => _amountText; set => Set(ref _amountText, value); }

    private string _categoryText = string.Empty;
    /// <summary>
    /// Input category text.
    /// </summary>
    public string CategoryText { get => _categoryText; set => Set(ref _categoryText, value); }

    private string _descriptionText = string.Empty;
    /// <summary>
    /// Input description text.
    /// </summary>
    public string DescriptionText { get => _descriptionText; set => Set(ref _descriptionText, value); }

    private TransactionType _selectedType = TransactionType.Expense;
    /// <summary>
    /// Input transaction type.
    /// </summary>
    public TransactionType SelectedType { get => _selectedType; set => Set(ref _selectedType, value); }

    private bool _isRecurring;
    /// <summary>
    /// Indicates whether the new transaction is recurring.
    /// </summary>
    public bool IsRecurring { get => _isRecurring; set => Set(ref _isRecurring, value); }

    /// <summary>
    /// Current balance formatted for display.
    /// </summary>
    public string BalanceText => SelectedWallet == null ? "0.00 EUR" : $"{SelectedWallet.GetTotalBalance():F2} EUR";

    /// <summary>
    /// Balance text color based on positive/negative balance.
    /// </summary>
    public Brush BalanceForeground
    {
        get
        {
            if (SelectedWallet == null) return Brushes.Black;
            var bal = SelectedWallet.GetTotalBalance();
            return bal < 0 ? Brushes.DarkRed : Brushes.DarkBlue;
        }
    }

    /// <summary>
    /// Command to add a new wallet.
    /// </summary>
    public ICommand AddWalletCommand { get; }
    /// <summary>
    /// Command to delete the selected wallet.
    /// </summary>
    public ICommand DeleteWalletCommand { get; }

    /// <summary>
    /// Command to add a transaction.
    /// </summary>
    public ICommand AddTransactionCommand { get; }
    /// <summary>
    /// Command to delete the selected transaction.
    /// </summary>
    public ICommand DeleteTransactionCommand { get; }
    /// <summary>
    /// Command to show the new wallet overlay.
    /// </summary>
    public ICommand ShowNewWalletCommand { get; }
    /// <summary>
    /// Command to confirm creation of a new wallet.
    /// </summary>
    public ICommand ConfirmNewWalletCommand { get; }
    /// <summary>
    /// Command to cancel new wallet creation.
    /// </summary>
    public ICommand CancelNewWalletCommand { get; }
    /// <summary>
    /// Command to set category budgets.
    /// </summary>
    public ICommand SetBudgetCommand { get; }
    /// <summary>
    /// Command to set monthly budget.
    /// </summary>
    public ICommand SetMonthlyBudgetCommand { get; }
    /// <summary>
    /// Command to export transactions to Excel.
    /// </summary>
    public ICommand ExportExcelCommand { get; }
    /// <summary>
    /// Command to export wallet data to JSON.
    /// </summary>
    public ICommand ExportJsonCommand { get; }
    /// <summary>
    /// Command to import wallet data from JSON.
    /// </summary>
    public ICommand ImportJsonCommand { get; }
    /// <summary>
    /// Command to import transactions from Excel.
    /// </summary>
    public ICommand ImportExcelCommand { get; }

    /// <summary>
    /// Available transaction types.
    /// </summary>
    public IEnumerable<TransactionType> TransactionTypes => Enum.GetValues(typeof(TransactionType)).Cast<TransactionType>();

    /// <summary>
    /// Initializes the view model and loads the profile.
    /// </summary>
    public AppWindowViewModel()
    {
        TransactionsView = CollectionViewSource.GetDefaultView(Transactions);
        TransactionsView.Filter = FilterTransaction;

        _profile = StorageService.LoadProfile();
        foreach (var w in _profile.Wallets) Wallets.Add(w);
        SelectedWallet = Wallets.FirstOrDefault();
        SelectedChartMode = "Príjmy";

        // Theme from profile
        _theme = _profile.Theme ?? "Light";

        // Locked state depending on password
        IsLocked = !string.IsNullOrEmpty(_profile.PasswordHash);

        AddWalletCommand = new RelayCommand(_ =>
        {
            // open overlay to create a named wallet
            NewWalletName = $"Nová peňaženka {Wallets.Count + 1}";
            IsNewWalletOverlayVisible = true;
        });
        DeleteWalletCommand = new RelayCommand(_ => DeleteWallet(), _ => SelectedWallet != null);

        ShowNewWalletCommand = new RelayCommand(_ =>
        {
            NewWalletName = $"Nová peňaženka {Wallets.Count + 1}";
            IsNewWalletOverlayVisible = true;
        });
        ConfirmNewWalletCommand = new RelayCommand(_ =>
        {
            ConfirmNewWallet(NewWalletName);
            IsNewWalletOverlayVisible = false;
        }, _ => true);
        CancelNewWalletCommand = new RelayCommand(_ => { IsNewWalletOverlayVisible = false; });

        AddTransactionCommand = new RelayCommand(_ =>
        {
            var ok = AddTransactionFromInputs(AmountText, CategoryText, DescriptionText, SelectedType, IsRecurring);
            if (ok)
            {
                AmountText = string.Empty;
                DescriptionText = string.Empty;
                IsRecurring = false;
            }
        }, _ => SelectedWallet != null);

        DeleteTransactionCommand = new RelayCommand(_ => DeleteSelectedTransaction(), _ => SelectedTransaction != null);

        SetBudgetCommand = new RelayCommand(_ => SetBudgetFromInputs(), _ => SelectedWallet != null);
        SetMonthlyBudgetCommand = new RelayCommand(_ => SetMonthlyBudgetFromInputs(), _ => SelectedWallet != null);
        ExportExcelCommand = new RelayCommand(_ => ExportExcel(), _ => SelectedWallet != null);
        ExportJsonCommand = new RelayCommand(_ => ExportJson(), _ => SelectedWallet != null);
        ImportExcelCommand = new RelayCommand(_ => ImportExcel(), _ => SelectedWallet != null);
        ImportJsonCommand = new RelayCommand(_ => ImportJson(), _ => SelectedWallet != null);
    }

    private string _theme = "Light";
    public string Theme
    {
        get => _theme;
        set
        {
            Set(ref _theme, value);
            if (_profile != null)
            {
                _profile.Theme = value;
                // persist profile (keep other fields intact)
                _profile.Wallets = Wallets.ToList();
                StorageService.SaveProfile(_profile);
            }
        }
    }

    private int _themeIndex;
    public int ThemeIndex { get => _themeIndex; set { Set(ref _themeIndex, value); Theme = value == 1 ? "Dark" : "Light"; } }

    private bool _isLocked;
    public bool IsLocked { get => _isLocked; set => Set(ref _isLocked, value); }

    /// <summary>
    /// Validates the provided password against the stored hash.
    /// </summary>
    public bool VerifyLogin(string password)
    {
        if (string.IsNullOrEmpty(_profile.PasswordHash))
        {
            IsLocked = false;
            return true;
        }
        var ok = SecurityHelper.VerifyPassword(password, _profile.PasswordHash);
        if (ok) IsLocked = false;
        return ok;
    }

    /// <summary>
    /// Changes the stored password hash.
    /// </summary>
    public (bool Ok, string Message) ChangePassword(string current, string? neu)
    {
        if (!string.IsNullOrEmpty(_profile.PasswordHash))
        {
            if (!SecurityHelper.VerifyPassword(current, _profile.PasswordHash)) return (false, "Súčasné heslo je nesprávne!");
        }
        _profile.PasswordHash = SecurityHelper.HashPassword(neu ?? string.Empty);
        StorageService.SaveProfile(_profile);
        return (true, "Bezpečnostné nastavenia boli úspešne aktualizované!");
    }

    private string _newWalletName = string.Empty;
    /// <summary>
    /// New wallet name input.
    /// </summary>
    public string NewWalletName { get => _newWalletName; set => Set(ref _newWalletName, value); }

    private bool _isNewWalletOverlayVisible;
    /// <summary>
    /// Controls visibility of the new wallet overlay.
    /// </summary>
    public bool IsNewWalletOverlayVisible { get => _isNewWalletOverlayVisible; set => Set(ref _isNewWalletOverlayVisible, value); }

    /// <summary>
    /// Adds a new wallet with a default name.
    /// </summary>
    private void AddWallet()
    {
        var w = new Wallet { Name = "Nová peňaženka " + (Wallets.Count + 1) };
        Wallets.Add(w);
        SelectedWallet = w;
        SaveProfile();
    }

    /// <summary>
    /// Deletes the currently selected wallet.
    /// </summary>
    private void DeleteWallet()
    {
        if (SelectedWallet == null) return;
        Wallets.Remove(SelectedWallet);
        if (Wallets.Count == 0) Wallets.Add(new Wallet { Name = "Osobný účet" });
        SelectedWallet = Wallets.FirstOrDefault();
        SaveProfile();
    }

    /// <summary>
    /// Persists the current profile to storage.
    /// </summary>
    private void SaveProfile()
    {
        var profile = new AppProfile
        {
            Wallets = Wallets.ToList(),
            PasswordHash = _profile.PasswordHash,
            Theme = _profile.Theme
        };
        StorageService.SaveProfile(profile);
    }

    /// <summary>
    /// Refreshes collections derived from the selected wallet.
    /// </summary>
    private void UpdateDerivedCollections()
    {
        Transactions.Clear();
        Categories.Clear();
        FilterCategories.Clear();
        FilterCategories.Add("Všetky");
        if (SelectedWallet != null)
        {
            foreach (var t in SelectedWallet.Transactions.OrderByDescending(t => t.Date)) Transactions.Add(t);
            foreach (var c in SelectedWallet.Categories) Categories.Add(c);
            foreach (var c in SelectedWallet.Categories) FilterCategories.Add(c);
        }
        if (!FilterCategories.Contains(SelectedFilterCategory)) SelectedFilterCategory = "Všetky";
        OnPropertyChanged(nameof(BalanceText));
        OnPropertyChanged(nameof(BalanceForeground));
        RefreshBudgetStatuses();
        RefreshMonthlyBudgetStatus();
        RefreshMonthlySummaries();
        RefreshOverviewStats();
        ApplyFilter();
    }

    /// <summary>
    /// Adds a transaction from user input fields.
    /// </summary>
    public bool AddTransactionFromInputs(string amountText, string category, string description, TransactionType type, bool isRecurring)
    {
        if (SelectedWallet == null) return false;
        if (!decimal.TryParse(amountText, out var amount)) return false;
        var t = new Transaction
        {
            Amount = amount,
            Category = string.IsNullOrWhiteSpace(category) ? "Iné" : category.Trim(),
            Description = description ?? string.Empty,
            Type = type,
            Date = DateTime.Now,
            IsRecurring = isRecurring,
            LastProcessed = isRecurring ? DateTime.Now : null
        };
        SelectedWallet.AddTransaction(t);
        if (!SelectedWallet.Categories.Contains(t.Category)) SelectedWallet.Categories.Add(t.Category);
        Transactions.Insert(0, t);
        if (!Categories.Contains(t.Category)) Categories.Add(t.Category);
        if (!FilterCategories.Contains(t.Category)) FilterCategories.Add(t.Category);
        OnPropertyChanged(nameof(BalanceText));
        OnPropertyChanged(nameof(BalanceForeground));
        RebuildPieChartData();
        RefreshBudgetStatuses();
        RefreshMonthlyBudgetStatus();
        RefreshMonthlySummaries();
        RefreshOverviewStats();
        SaveProfile();
        return true;
    }

    /// <summary>
    /// Deletes the selected transaction from the wallet.
    /// </summary>
    public bool DeleteSelectedTransaction()
    {
        if (SelectedWallet == null || SelectedTransaction == null) return false;
        SelectedWallet.RemoveTransaction(SelectedTransaction.Id);
        Transactions.Remove(SelectedTransaction);
        SelectedTransaction = null;
        OnPropertyChanged(nameof(BalanceText));
        OnPropertyChanged(nameof(BalanceForeground));
        RebuildPieChartData();
        RefreshBudgetStatuses();
        RefreshMonthlyBudgetStatus();
        RefreshMonthlySummaries();
        RefreshOverviewStats();
        SaveProfile();
        return true;
    }

    /// <summary>
    /// Confirms creation of a wallet with the provided name.
    /// </summary>
    public void ConfirmNewWallet(string name)
    {
        var w = new Wallet { Name = string.IsNullOrWhiteSpace(name) ? $"Nová peňaženka {Wallets.Count + 1}" : name.Trim() };
        Wallets.Add(w);
        SelectedWallet = w;
        SaveProfile();
    }

    /// <summary>
    /// Deletes the specified wallet.
    /// </summary>
    public void DeleteWalletBySelection(Wallet w)
    {
        if (w == null) return;
        Wallets.Remove(w);
        if (Wallets.Count == 0) Wallets.Add(new Wallet { Name = "Osobný účet" });
        SelectedWallet = Wallets.FirstOrDefault();
        SaveProfile();
    }

    /// <summary>
    /// Rebuilds pie chart data based on the selected wallet and chart mode.
    /// </summary>
    public void RebuildPieChartData()
    {
        PieSlices.Clear();
        LegendItems.Clear();
        if (SelectedWallet == null) { PieChartChanged?.Invoke(); return; }
        var all = SelectedWallet.Transactions.ToList();
        if (!all.Any()) { PieChartChanged?.Invoke(); return; }
        var filtered = SelectedChartMode switch
        {
            "Výdavky" => all.Where(t => t.Type == TransactionType.Expense).ToList(),
            "Príjmy" => all.Where(t => t.Type == TransactionType.Income).ToList(),
            _ => all.Where(t => t.Type == TransactionType.Income).ToList()
        };
        if (!filtered.Any()) { PieChartChanged?.Invoke(); return; }
        decimal total = filtered.Sum(t => t.Amount);
        var grouped = filtered.GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Amount = g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Amount)
            .ToList();
        var brushes = new[] { Brushes.Tomato, Brushes.SteelBlue, Brushes.MediumSeaGreen, Brushes.Gold, Brushes.MediumPurple, Brushes.Coral, Brushes.DarkOrange };
        double angle = 0;
        for (int i = 0; i < grouped.Count; i++)
        {
            var g = grouped[i];
            double sweep = total == 0 ? 0 : (double)(g.Amount / total) * 360.0;
            var brush = brushes[i % brushes.Length];
            PieSlices.Add(new PieSliceDto { StartAngle = angle, SweepAngle = sweep, Fill = brush });
            LegendItems.Add(new LegendItemDto { Title = total == 0 ? $"{g.Category} - {g.Amount:F2} €" : $"{g.Category} - {g.Amount:F2} € ({(g.Amount / total):P1})", ColorBrush = brush });
            angle += sweep;
        }
        OnPropertyChanged(nameof(PieSlices));
        OnPropertyChanged(nameof(LegendItems));
        PieChartChanged?.Invoke();
    }

    /// <summary>
    /// Applies filter rules to a transaction for the collection view.
    /// </summary>
    private bool FilterTransaction(object obj)
    {
        if (obj is not Transaction t) return false;

        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            var text = FilterText.Trim();
            if (!(t.Description?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) &&
                !(t.Category?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false))
                return false;
        }

        if (!string.IsNullOrWhiteSpace(SelectedFilterCategory) && SelectedFilterCategory != "Všetky")
        {
            if (!string.Equals(t.Category, SelectedFilterCategory, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (SelectedFilterType == "Výdavok" && t.Type != TransactionType.Expense)
            return false;
        if (SelectedFilterType == "Príjem" && t.Type != TransactionType.Income)
            return false;

        if (!string.IsNullOrWhiteSpace(FilterAmountText))
        {
            if (!decimal.TryParse(FilterAmountText, out var amount)) return true;
            if (t.Amount != amount) return false;
        }

        return true;
    }

    /// <summary>
    /// Refreshes the filtered transaction view.
    /// </summary>
    private void ApplyFilter()
    {
        TransactionsView.Refresh();
    }

    /// <summary>
    /// Updates category budget based on user input.
    /// </summary>
    private void SetBudgetFromInputs()
    {
        if (SelectedWallet == null) return;
        if (string.IsNullOrWhiteSpace(BudgetCategory)) return;
        if (!decimal.TryParse(BudgetAmountText, out var amount)) return;
        SelectedWallet.CategoryBudgets[BudgetCategory] = amount;
        RefreshBudgetStatuses();
        SaveProfile();
    }

    /// <summary>
    /// Updates monthly budget based on user input.
    /// </summary>
    private void SetMonthlyBudgetFromInputs()
    {
        if (SelectedWallet == null) return;
        if (!decimal.TryParse(MonthlyBudgetText, out var amount)) return;
        SelectedWallet.MonthlyBudget = amount;
        RefreshMonthlyBudgetStatus();
        SaveProfile();
    }

    /// <summary>
    /// Recalculates category budget statuses.
    /// </summary>
    private void RefreshBudgetStatuses()
    {
        BudgetStatuses.Clear();
        if (SelectedWallet == null) return;
        foreach (var kvp in SelectedWallet.CategoryBudgets.OrderBy(k => k.Key))
        {
            var spent = SelectedWallet.Transactions
                .Where(t => t.Type == TransactionType.Expense && string.Equals(t.Category, kvp.Key, StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);
            BudgetStatuses.Add(new BudgetStatusDto { Category = kvp.Key, Budget = kvp.Value, Spent = spent });
        }
        OnPropertyChanged(nameof(BudgetStatuses));
    }

    /// <summary>
    /// Recalculates the monthly budget warning.
    /// </summary>
    private void RefreshMonthlyBudgetStatus()
    {
        if (SelectedWallet == null)
        {
            MonthlyBudgetWarning = string.Empty;
            return;
        }

        if (SelectedWallet.MonthlyBudget is null)
        {
            MonthlyBudgetWarning = "";
            return;
        }

        var now = DateTime.Now;
        var spent = SelectedWallet.Transactions
            .Where(t => t.Type == TransactionType.Expense && t.Date.Year == now.Year && t.Date.Month == now.Month)
            .Sum(t => t.Amount);

        var remaining = SelectedWallet.MonthlyBudget.Value - spent;
        if (remaining < 0)
        {
            MonthlyBudgetWarning = $"Mesačný rozpočet prekročený o {Math.Abs(remaining):F2} €";
            MonthlyBudgetWarningBrush = Brushes.IndianRed;
        }
        else
        {
            MonthlyBudgetWarning = $"Zostáva {remaining:F2} € do mesačného limitu";
            MonthlyBudgetWarningBrush = Brushes.SeaGreen;
        }
    }

    /// <summary>
    /// Rebuilds the monthly summary list.
    /// </summary>
    private void RefreshMonthlySummaries()
    {
        MonthlySummaries.Clear();
        if (SelectedWallet == null) return;
        var grouped = SelectedWallet.Transactions
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .Take(12)
            .Select(g => new MonthSummaryDto
            {
                MonthLabel = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MM/yyyy"),
                Income = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                Expense = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
            });

        foreach (var item in grouped) MonthlySummaries.Add(item);
        OnPropertyChanged(nameof(MonthlySummaries));
    }

    /// <summary>
    /// Rebuilds overview statistics.
    /// </summary>
    private void RefreshOverviewStats()
    {
        OverviewStats.Clear();
        if (SelectedWallet == null) return;
        var expenses = SelectedWallet.Transactions.Where(t => t.Type == TransactionType.Expense).ToList();
        var incomes = SelectedWallet.Transactions.Where(t => t.Type == TransactionType.Income).ToList();
        var topCategories = expenses
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Amount = g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Amount)
            .Take(5)
            .ToList();

        var biggest = SelectedWallet.Transactions.OrderByDescending(t => t.Amount).FirstOrDefault();

        OverviewStats.Add(new OverviewStatDto { Title = "Top 5 kategórií", Value = topCategories.Count == 0 ? "-" : string.Join(", ", topCategories.Select(c => $"{c.Category} ({c.Amount:F0}€)")) });
        OverviewStats.Add(new OverviewStatDto { Title = "Najväčšia transakcia", Value = biggest == null ? "-" : $"{biggest.Category} {biggest.Amount:F2} €" });
        OverviewStats.Add(new OverviewStatDto { Title = "Spolu príjmy", Value = incomes.Sum(t => t.Amount).ToString("F2") + " €" });
        OverviewStats.Add(new OverviewStatDto { Title = "Spolu výdavky", Value = expenses.Sum(t => t.Amount).ToString("F2") + " €" });
        OnPropertyChanged(nameof(OverviewStats));
    }

    /// <summary>
    /// Exports transactions to an Excel file.
    /// </summary>
    private void ExportExcel()
    {
        if (SelectedWallet == null) return;
        var dialog = new SaveFileDialog
        {
            Filter = "Excel súbor (*.xlsx)|*.xlsx",
            FileName = $"transactions_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() == true)
        {
            ExportImportService.ExportToExcel(SelectedWallet.Transactions, dialog.FileName);
        }
    }

    /// <summary>
    /// Exports the current wallet to a JSON file.
    /// </summary>
    private void ExportJson()
    {
        if (SelectedWallet == null) return;
        var dialog = new SaveFileDialog
        {
            Filter = "JSON súbor (*.json)|*.json",
            FileName = $"wallet_{DateTime.Now:yyyyMMdd_HHmm}.json"
        };
        if (dialog.ShowDialog() == true)
        {
            var json = JsonSerializer.Serialize(SelectedWallet, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dialog.FileName, json);
        }
    }

    /// <summary>
    /// Imports transactions from an Excel file.
    /// </summary>
    private void ImportExcel()
    {
        if (SelectedWallet == null) return;
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel súbor (*.xlsx)|*.xlsx"
            };
            if (dialog.ShowDialog() != true) return;
            var transactions = ExportImportService.ImportFromExcel(dialog.FileName);
            SelectedWallet.Transactions = transactions;
            SelectedWallet.Categories = transactions.Select(t => t.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();
            UpdateDerivedCollections();
            RebuildPieChartData();
            SaveProfile();
        }
        catch
        {
            // ignore malformed import
        }
    }

    /// <summary>
    /// Imports a wallet from a JSON file.
    /// </summary>
    private void ImportJson()
    {
        if (SelectedWallet == null) return;
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON súbor (*.json)|*.json"
            };
            if (dialog.ShowDialog() != true) return;
            var json = File.ReadAllText(dialog.FileName);
            var wallet = JsonSerializer.Deserialize<Wallet>(json);
            if (wallet == null) return;
            SelectedWallet.Transactions = wallet.Transactions ?? new List<Transaction>();
            SelectedWallet.Categories = wallet.Categories ?? new List<string>();
            SelectedWallet.CategoryBudgets = wallet.CategoryBudgets ?? new Dictionary<string, decimal>();
            UpdateDerivedCollections();
            RebuildPieChartData();
            SaveProfile();
        }
        catch
        {
            // ignore malformed import
        }
    }
}
