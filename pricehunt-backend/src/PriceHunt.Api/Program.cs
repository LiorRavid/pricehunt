using PriceHunt.Api.ErrorHandling;
using PriceHunt.Application;
using PriceHunt.Application.Searches;
using PriceHunt.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

builder.Services.AddOptions<SearchOptions>()
    .Bind(builder.Configuration.GetSection(SearchOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);

WebApplication app = builder.Build();

// Create or migrate the SQLite database before the server accepts requests.
await app.Services.InitializeDatabaseAsync(app.Lifetime.ApplicationStopping);

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

await app.RunAsync();
