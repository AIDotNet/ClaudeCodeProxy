using ClaudeCodeProxy.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeCodeProxy.EntityFrameworkCore.PostgreSQL;

public static class ServiceExtensions
{
    /// <summary>
    /// Adds Entity Framework Core with SQLite support to the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddEntityFrameworkCorePostgreSQL(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register the DbContext with SQLite provider
        services.AddDbContext<IContext, PostgreSQLDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}