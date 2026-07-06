using BMS.API.Extensions;
using BMS.API.Middleware;
using BMS.API.Services;
using BMS.Application.Interfaces;
using BMS.Infrastructure.BackgroundServices;
using BMS.Infrastructure.Seeders;

var builder = WebApplication.CreateBuilder(args);

// ── Service Registrations ────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerWithJwt();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddJwtAuthentication(builder.Configuration);

// ── Background Services ──────────────────────────────────────────────────────
builder.Services.AddHostedService<NotificationGeneratorService>();

// CORS — allow Angular dev server during development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── Build App ────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Seed Roles, Default Admin, and Business Data ────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await RoleSeeder.SeedAsync(services);
    await AdminSeeder.SeedAsync(services, app.Configuration);
    await DataSeeder.SeedAsync(services);
}

// ── Middleware Pipeline ──────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BMS API v1");
        c.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");

app.UseAuthentication(); // Must come BEFORE UseAuthorization
app.UseAuthorization();

app.MapControllers();
app.Run();
