# Environment Spanner

A modern WPF application for managing Windows environment variables with a clean, dark-themed interface.

## Features

- **User and System Environment Variables**: Manage both user-level and system-level environment variables in separate tabs
- **List Value Editor**: Special editor for semicolon-delimited list values (like PATH) with an intuitive interface
- **Elevation Awareness**: Automatically detects administrator privileges and enables system variable editing when elevated
- **Change Tracking**: Only saves modified entries to minimize unnecessary writes
- **Dark Theme**: Modern Fluent Design System dark theme by default
- **Comprehensive Logging**: Detailed logging to both console and file for debugging
- **Busy Indicators**: Visual feedback during I/O operations

## Requirements

- .NET 9.0
- Windows (WPF application)
- Administrator privileges (optional, for editing system variables)

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run
```

Or run the executable from `bin/Debug/net9.0-windows/EnvironmentSpanner.exe`

## Usage

1. **Viewing Variables**: The application displays all environment variables in a table format, separated into User and System tabs
2. **Editing Variables**: 
   - Click on any cell to edit the value directly
   - For semicolon-delimited list values, right-click on the Value cell and select "Edit List Values..." from the context menu
3. **Adding Variables**: Click the "Add Variable" button to create a new environment variable
4. **Deleting Variables**: Click the "Delete" button in the Actions column
5. **Saving Changes**: Click "Save" to persist your changes. The application will only save modified entries
6. **System Variables**: To edit system variables, run the application as Administrator

## Architecture

- **MVVM Pattern**: Uses CommunityToolkit.Mvvm for clean separation of concerns
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection for service management
- **Logging**: Serilog for structured logging with console and file sinks
- **Modern UI**: ModernWpfUI for Fluent Design System styling

## Logs

Logs are written to:
- Console (during development)
- `%LocalAppData%\EnvironmentSpanner\EnvironmentSpanner_YYYYMMDD.log` (daily rolling files, 7-day retention)

## License

MIT License - see LICENSE file for details
