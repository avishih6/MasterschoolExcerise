using System.Net.Http.Headers;
using CycodeExerciseApi.Interfaces;
using CycodeExerciseApi.Services;
using CycodeExerciseApi.Services.FileParsers;
using CycodeExerciseApi.Services.VulnerabilityProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register file parsers (add more here for new ecosystems)
builder.Services.AddSingleton<IFileParser, NpmFileParser>();
// Future: builder.Services.AddSingleton<IFileParser, PipFileParser>();
// Future: builder.Services.AddSingleton<IFileParser, NugetFileParser>();

// Register vulnerability provider with configured HttpClient
builder.Services.AddHttpClient<IVulnerabilityProvider, GitHubVulnerabilityProvider>((sp, client) =>
{
    // Get GitHub token from environment variable (support both hyphen and underscore versions)
    var token = Environment.GetEnvironmentVariable("GITHUB-ACCESS-TOKEN") 
        ?? Environment.GetEnvironmentVariable("GITHUB_ACCESS_TOKEN");
    if (string.IsNullOrEmpty(token))
    {
        throw new InvalidOperationException(
            "GitHub access token not found. Please set the GITHUB_ACCESS_TOKEN environment variable.");
    }
    
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CycodeScanner", "1.0"));
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// Register scan service
builder.Services.AddScoped<IScanService, ScanService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
