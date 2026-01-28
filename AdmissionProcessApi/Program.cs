using AdmissionProcessBL;
using AdmissionProcessBL.Interfaces;
using AdmissionProcessDAL.Repositories.Interfaces;
using AdmissionProcessDAL.Repositories.Mock;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IUserRepository, MockUserRepository>();
builder.Services.AddSingleton<IFlowRepository, MockFlowRepository>();
builder.Services.AddSingleton<IProgressRepository, MockProgressRepository>();

builder.Services.AddScoped<IUserLogic, UserLogic>();
builder.Services.AddScoped<IFlowLogic, FlowLogic>();
builder.Services.AddScoped<IProgressLogic, ProgressLogic>();
builder.Services.AddScoped<IStatusLogic, StatusLogic>();
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
