using AdmissionProcessBL.Services;
using AdmissionProcessBL.Services.Interfaces;
using AdmissionProcessDAL.Repositories.Interfaces;
using AdmissionProcessDAL.Repositories.Mock;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IUserRepository, MockUserRepository>();
builder.Services.AddSingleton<IFlowRepository, MockFlowRepository>();
builder.Services.AddSingleton<IProgressRepository, MockProgressRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFlowService, FlowService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddSingleton<IPassEvaluator, PassEvaluator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
