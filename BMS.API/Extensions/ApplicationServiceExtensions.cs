using BMS.Application.Interfaces;
using BMS.Application.Services;
using FluentValidation.AspNetCore;
using FluentValidation;
using System.Reflection;

namespace BMS.API.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService,         AuthService>();
        services.AddScoped<IUnitService,         UnitService>();
        services.AddScoped<ITenantService,       TenantService>();
        services.AddScoped<ILeaseService,        LeaseService>();
        services.AddScoped<IInvoiceService,      InvoiceService>();
        services.AddScoped<IPaymentService,      PaymentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReportService,       ReportService>();
        services.AddScoped<IDashboardService,    DashboardService>();

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssembly(Assembly.Load("BMS.Application"));

        return services;
    }
}
