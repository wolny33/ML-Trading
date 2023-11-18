using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TradingBot.Configuration;
using TradingBot.Database;
using TradingBot.Services;

namespace TradingBot;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureConfiguration(builder.Services, builder.Configuration);
        ConfigureServices(builder.Services, builder.Configuration);
        ConfigureAuth(builder.Services);

        builder.Services.AddControllers();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        await ApplyMigrationsAsync(app.Services);

        await app.RunAsync();
    }

    private static void ConfigureAuth(IServiceCollection services)
    {
        services.AddAuthentication(BasicAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions,
                BasicAuthenticationHandler>(BasicAuthenticationHandler.SchemeName, null);
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
    }

    private static async Task ApplyMigrationsAsync(IServiceProvider services)
    {
        await using var context =
            await services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();
        await context.Database.MigrateAsync();
    }

    private static void ConfigureConfiguration(IServiceCollection services, IConfiguration config)
    {
        services.Configure<AlpacaConfiguration>(config.GetSection(AlpacaConfiguration.SectionName));
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(config.GetConnectionString("Data")));
        services.AddSingleton<IAlpacaClientFactory, AlpacaClientFactory>();
        services.AddScoped<IMarketDataSource, MarketDataSource>();
        services.AddScoped<IPricePredictor, PricePredictor>();
        services.AddScoped<IStrategy, Strategy>();
        services.AddScoped<IActionExecutor, ActionExecutor>();

        services.AddScoped<CredentialsCommand>();
    }
}
