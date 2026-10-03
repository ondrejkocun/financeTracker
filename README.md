# FinanceTracker

Personal finance tracker built with C# and .NET. The project provides both a
Windows desktop interface (WPF) and a command-line interface (CLI) for
recording and reviewing personal finances.

## Features

- Manage multiple wallets and transactions
- Track income and expenses by category
- Calculate the current balance and monthly summaries
- Set monthly and per-category budgets
- Mark transactions as recurring
- Filter transactions by text, category, type, and amount
- Display income and expense statistics with charts in the WPF application
- Import and export transactions as Excel `.xlsx` files
- Store application data locally

## Technology

- C#
- .NET 10
- WPF for the desktop application
- CLI for terminal usage
- ClosedXML for Excel import and export

## Project structure

```text
FinanceTracker/
├── FinanceTracker/       Shared models and services
├── FinanceTracker.CLI/   Command-line application
└── FinanceTracker.WPF/   WPF desktop application
```

## Requirements

- .NET 10 SDK
- Windows for running the WPF application

Check the installed SDK with:

```bash
dotnet --version
```

## Running the CLI

From the repository root, use:

```bash
dotnet run --project FinanceTracker.CLI/FinanceTracker.CLI.csproj -- --help
```

Examples:

```bash
# Add an expense
dotnet run --project FinanceTracker.CLI/FinanceTracker.CLI.csproj -- add --amount 25.50 --category Food --desc "Lunch" --type Expense

# Add an income
dotnet run --project FinanceTracker.CLI/FinanceTracker.CLI.csproj -- add --amount 1500 --category Salary --type Income

# List transactions
dotnet run --project FinanceTracker.CLI/FinanceTracker.CLI.csproj -- list

# Show a monthly summary
dotnet run --project FinanceTracker.CLI/FinanceTracker.CLI.csproj -- summary --month 10 --year 2026

# Export transactions to Excel
dotnet run --project FinanceTracker.CLI/FinanceTracker.CLI.csproj -- export --file transactions.xlsx
```

Available commands:

| Command | Description |
| --- | --- |
| `add` | Add a new income or expense |
| `list` | List recorded transactions |
| `delete` | Delete a transaction by ID |
| `summary` | Show income, expenses, and balance |
| `export` | Export transactions to an Excel file |
| `categories` | List available categories |

## Running the WPF application

On Windows, start the desktop application with:

```bash
dotnet run --project FinanceTracker.WPF/FinanceTracker.WPF.csproj
```

The WPF application provides wallet management, transaction filtering,
budget tracking, summaries, charts, and Excel import/export through a graphical
interface.

## Current project note

The solution file and project references still contain the previous
`Semestralka` directory names. Before building the complete solution, update
those references to the current `FinanceTracker` directory names.
