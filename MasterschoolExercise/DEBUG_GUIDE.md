# Debugging Guide

## How to Debug in VS Code

1. **Open the project in VS Code**
   ```bash
   cd /Users/avishayhadar/Repos/MasterschoolExercise
   code .
   ```

2. **Set Breakpoints**
   - Click in the left margin next to any line of code to set a breakpoint
   - Red dots indicate breakpoints

3. **Start Debugging**
   - Press `F5` OR
   - Go to Run and Debug panel (`Cmd+Shift+D`)
   - Select ".NET Core Launch (web)" from the dropdown
   - Click the green play button

4. **Access the Application**
   - Swagger UI will automatically open at: `http://localhost:8080/swagger`
   - API endpoints: `http://localhost:8080/api/...`

## Troubleshooting

### Port 8080 not working?
- Make sure no other application is using port 8080
- Check if the debugger is actually running (look for the debug toolbar)
- Try stopping all processes: `pkill -f MasterschoolExercise`

### Breakpoints not hitting?
- Make sure you're using the Debug build (not Release)
- Verify the code matches what's running
- Check the Debug Console for errors

### Application won't start?
- Check the Debug Console for error messages
- Verify .NET 10 SDK is installed: `dotnet --version`
- Rebuild: `dotnet clean && dotnet build`

## Quick Test

Once debugging:
1. Open Swagger: http://localhost:8080/swagger
2. Try creating a user: POST `/api/users` with `{"email": "test@example.com"}`
3. Set a breakpoint in `UserService.CreateUserAsync` to see it in action!
