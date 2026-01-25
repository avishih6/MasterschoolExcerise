# Running the Application in VS Code

## Prerequisites

1. **Install .NET 8 SDK**
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Or use Homebrew on macOS: `brew install dotnet`

2. **Install VS Code Extensions**
   - Open VS Code in this folder
   - Press `Cmd+Shift+P` (macOS) or `Ctrl+Shift+P` (Windows/Linux)
   - Type "Extensions: Show Recommended Extensions"
   - Install the recommended extensions:
     - **C#** (ms-dotnettools.csharp)
     - **.NET Install Tool** (ms-dotnettools.vscode-dotnet-runtime)

## Running the Application

### Option 1: Using VS Code Debugger (Recommended)
1. Press `F5` or go to Run and Debug panel (`Cmd+Shift+D`)
2. Select ".NET Core Launch (web)" from the dropdown
3. Click the green play button or press `F5`
4. Swagger will automatically open in your browser at `http://localhost:5000/swagger`

### Option 2: Using Terminal
1. Open integrated terminal in VS Code (`Ctrl+` ` or `View > Terminal`)
2. Run:
   ```bash
   dotnet restore
   dotnet run
   ```
3. Open browser and navigate to: `http://localhost:5000/swagger`

### Option 3: Using Watch Mode (Auto-reload on changes)
1. Press `Cmd+Shift+P` and type "Tasks: Run Task"
2. Select "watch"
3. The app will restart automatically when you make code changes

## Verifying .NET Installation

Run this in the terminal to check if .NET is installed:
```bash
dotnet --version
```

You should see something like: `8.0.xxx`

If not installed, follow the prerequisites above.

## Troubleshooting

- **"dotnet command not found"**: Make sure .NET SDK is installed and added to PATH
- **Extensions not working**: Reload VS Code after installing extensions
- **Port already in use**: Change the port in `Properties/launchSettings.json` or kill the process using port 5000
