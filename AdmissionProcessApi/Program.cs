using AdmissionProcessApi.DependencyInjection;
using AdmissionProcessApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add all Admission Process services (repositories, Service Bus, application services)
builder.Services.AddAdmissionProcessServices(builder.Configuration);

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
