using MasterschoolExercise.Services;
using MasterschoolExercise.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register repositories (mock DB tables)
builder.Services.AddSingleton<IStepRepository, MockStepRepository>();
builder.Services.AddSingleton<IFlowTaskRepository, MockFlowTaskRepository>();
builder.Services.AddSingleton<IStepTaskRepository, MockStepTaskRepository>();
builder.Services.AddSingleton<IUserTaskAssignmentRepository, MockUserTaskAssignmentRepository>();
builder.Services.AddSingleton<IUserRepository, MockUserRepository>();
builder.Services.AddSingleton<IUserProgressRepository, MockUserProgressRepository>();

// Register services
builder.Services.AddSingleton<IConditionEvaluator, ConditionEvaluator>();
builder.Services.AddSingleton<IFlowDataSeeder, FlowDataSeeder>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFlowService, FlowService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<IStepManagementService, StepManagementService>();
builder.Services.AddScoped<ITaskManagementService, TaskManagementService>();

var app = builder.Build();

// Seed initial data
var seeder = app.Services.GetRequiredService<IFlowDataSeeder>();
await seeder.SeedInitialDataAsync();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Disabled for HTTP-only debugging on port 8080
app.UseAuthorization();
app.MapControllers();

app.Run();
