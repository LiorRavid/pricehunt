using Microsoft.EntityFrameworkCore;
using PriceHunt.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("PriceHunt")
    ?? throw new InvalidOperationException("Connection string 'PriceHunt' not found.");
builder.Services.AddDbContext<PriceHuntDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

// Create or upgrade the SQLite database on startup, so a fresh clone runs without manual setup.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<PriceHuntDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/suppliers", async (PriceHuntDbContext db) =>
    await db.Suppliers.AsNoTracking().OrderBy(s => s.Name).ToListAsync())
.WithName("GetSuppliers");

app.Run();
