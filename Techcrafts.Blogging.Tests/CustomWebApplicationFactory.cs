using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Techcrafts.Blogging.Infrastructure.Persistence;

namespace Techcrafts.Blogging.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string TestConnectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING") ?? 
        "Server=localhost;Database=Techcrafts_Certification_Test;Integrated Security=True;TrustServerCertificate=True;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
                ["Telemetry:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Ensure DbContext is configured for the isolated test database
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<BloggingDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<BloggingDbContext>(options =>
            {
                options.UseSqlServer(TestConnectionString);
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BloggingDbContext>();
            try { db.Database.Migrate(); } catch { db.Database.EnsureCreated(); }
        });
    }
}
