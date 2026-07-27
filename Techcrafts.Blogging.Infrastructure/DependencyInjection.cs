using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Infrastructure.Persistence;
using Techcrafts.Blogging.Infrastructure.Services;

namespace Techcrafts.Blogging.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BloggingDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IBloggingDbContext>(provider => provider.GetRequiredService<BloggingDbContext>());
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
