using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using sitemind_shared.Abstractions;
using sitemind_shared.Data;
using sitemind_shared.Infrastructure;

namespace sitemind_shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSiteMindDbContext(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DefaultConnection")
    {
        var connectionString = configuration.GetConnectionString(connectionStringName);
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{connectionStringName}' not found in configuration.");
        }

        services.AddDbContext<SiteMindDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        // Register HttpContextAccessor for OrganizationProvider
        // Note: AddHttpContextAccessor is available in Microsoft.AspNetCore.Http package
        services.TryAddSingleton<IHttpContextAccessor, Microsoft.AspNetCore.Http.HttpContextAccessor>();

        // Register OrganizationProvider as scoped
        services.AddScoped<IOrganizationProvider, OrganizationProvider>();

        return services;
    }
}

