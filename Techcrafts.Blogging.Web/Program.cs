using Techcrafts.Blogging.Application.Features.Posts.Commands;
using Techcrafts.Blogging.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

// Configure initial Serilog before builder for early exceptions
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Setup Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Setup OpenTelemetry
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("Techcrafts.Blogging.Web"))
        .WithTracing(tracing => 
        {
            tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(opts => 
                {
                    var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                    if (!string.IsNullOrEmpty(endpoint))
                    {
                        opts.Endpoint = new Uri(endpoint);
                    }
                })
                .AddConsoleExporter();

            if (builder.Configuration.GetValue<bool>("Telemetry:Enabled", true))
            {
                tracing.AddProcessor(sp => new OpenTelemetry.BatchActivityExportProcessor(
                    new Techcrafts.Blogging.Infrastructure.Telemetry.DatabaseTelemetryExporter(sp)));
            }
        })
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter(opts => 
            {
                var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                if (!string.IsNullOrEmpty(endpoint))
                {
                    opts.Endpoint = new Uri(endpoint);
                }
            })
            .AddConsoleExporter());

// Add Architecture Layers
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<Techcrafts.Blogging.Infrastructure.Persistence.BloggingDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/access-denied";
});

// Add Authentication & OAuth Providers
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "mock-client-id";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "mock-client-secret";
    })
    .AddGitHub(options =>
    {
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"] ?? "mock-client-id";
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? "mock-client-secret";
        options.Scope.Add("user:email");
    });

// Add services to the container.
builder.Services.AddControllersWithViews();

// Setup MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CreatePostCommand).Assembly);
});

// Setup Infrastructure (DbContext, Services)

// Add AnalyticsBufferService as a Background HostedService
builder.Services.AddSingleton<Techcrafts.Blogging.Infrastructure.Services.AnalyticsBufferService>();
builder.Services.AddSingleton<Techcrafts.Blogging.Application.Interfaces.IAnalyticsBufferService>(provider => provider.GetRequiredService<Techcrafts.Blogging.Infrastructure.Services.AnalyticsBufferService>());
builder.Services.AddHostedService(provider => provider.GetRequiredService<Techcrafts.Blogging.Infrastructure.Services.AnalyticsBufferService>());

// Add HybridCache (.NET 9)
#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018

var app = builder.Build();

// Add the setup middleware early in the pipeline to intercept requests if not configured
app.UseMiddleware<Techcrafts.Blogging.Web.Middlewares.SetupMiddleware>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Replace custom logging middleware with Serilog's standard request logging
app.UseSerilogRequestLogging();
// app.UseMiddleware<Techcrafts.Blogging.Web.Middlewares.RequestLoggingMiddleware>();

// We'll map default route to Posts/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Posts}/{action=Index}/{id?}");

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
