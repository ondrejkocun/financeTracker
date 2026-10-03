# FinanceTracker

[![CI](https://github.com/ondrejkocun/financeTracker/actions/workflows/ci.yml/badge.svg)](https://github.com/ondrejkocun/financeTracker/actions/workflows/ci.yml)

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
- `System.Security.Cryptography` for data encryption and password hashing

## Data security

Profile files can be encrypted when a password is configured. The application
uses AES-GCM authenticated encryption with a 256-bit key, a random salt, a
random nonce, and an authentication tag. The encryption key is derived from
the password using PBKDF2-HMAC-SHA256 with 100,000 iterations.

Passwords are stored as SHA-256 hashes and are never stored in plain text.
Encrypted profile data is stored locally in `wallet_data.json`, in the user's
OneDrive folder when available or otherwise in the Documents folder.

## Project structure

```text
FinanceTracker/
├── FinanceTracker/       Shared models and services
├── FinanceTracker.CLI/   Command-line application
├── FinanceTracker.WPF/   WPF desktop application
└── FinanceTracker.Tests/ Unit tests
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

## Testing

Run all unit tests from the repository root:

```bash
dotnet test FInanceTracker.slnx --configuration Release
```

The tests cover wallet balances and month filtering, password hashing and
verification, and an Excel export/import round trip.

## Continuous integration

GitHub Actions restores, builds, and tests the solution on Windows for pushes
and pull requests. Windows is used because the solution includes a WPF project.

## License

This project is licensed under the [MIT License](LICENSE).
