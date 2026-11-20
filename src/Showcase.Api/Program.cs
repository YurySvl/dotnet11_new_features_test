using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Showcase.Api;
using Showcase.Api.Data;
using Showcase.Api.Telemetry;
using Showcase.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

// (b) Native OpenTelemetry tags: just listen to the "Microsoft.AspNetCore" ActivitySource.
builder.Services.AddSingleton<SpanRecorder>();

// (a) Async validation + localized messages.
builder.Services.AddSingleton<IUserDirectory, InMemoryUserDirectory>();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddValidation(options =>
{
    // Look up every validation message in Resources/ValidationMessages(.de).resx.
    options.LocalizerProvider = (_, factory) => factory.Create(typeof(ValidationMessages));
});

// (c) OpenAPI document.
builder.Services.AddOpenApi();

// (optional) EF Core 11 on SQLite. Generated SQL is logged through the normal ILogger pipeline
// ("Microsoft.EntityFrameworkCore.Database.Command": "Information" in appsettings.json).
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "shop.db");
builder.Services.AddDbContext<ShopContext>(options => options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();
app.Services.GetRequiredService<SpanRecorder>(); // start listening before the first request

app.UseRequestLocalization(options =>
{
    CultureInfo[] cultures = [new("en"), new("de")];
    options.DefaultRequestCulture = new("en");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
});

app.MapOpenApi(); // GET /openapi/v1.json

app.MapGet("/", () => "Showcase.Api: POST /users, GET /users/{name}, GET /debug/last-spans, GET /openapi/v1.json");

app.MapPost("/users", async (UserRegistration registration, IUserDirectory users, CancellationToken ct) =>
{
    // Only reached when validation (including the async uniqueness check) passed.
    await users.AddAsync(registration.Username, ct);
    return TypedResults.Created($"/users/{registration.Username}", registration);
});

app.MapGet("/users/{name}", async Task<IResult> (string name, IUserDirectory users, CancellationToken ct) =>
    await users.ExistsAsync(name, ct) ? TypedResults.Ok(new { name }) : TypedResults.NotFound());

app.MapGet("/debug/last-spans", (SpanRecorder recorder) => recorder.LastSpans)
   .ExcludeFromDescription();

app.MapEfEndpoints();

app.Run();
