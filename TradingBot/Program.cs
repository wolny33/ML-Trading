using System.Reflection;
using Flurl.Http.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Quartz;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using TradingBot.Configuration;
using TradingBot.Database;
using TradingBot.Exceptions;
using TradingBot.Services;

namespace TradingBot;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, loggerConfiguration) =>
            loggerConfiguration.ReadFrom.Configuration(context.Configuration)
        );

        ConfigureConfiguration(builder.Services, builder.Configuration);
        ConfigureServices(builder.Services, builder.Configuration);
        ConfigureAuth(builder.Services);

        builder.Services.AddQuartz(options =>
        {
            TradingTaskJob.RegisterJob(options);
            AssetsStateJob.RegisterJob(options);
        });
        builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(ConfigureSwagger);

        builder.Services.AddHealthChecks().AddSqlite(GetConnectionString(builder.Configuration), name: "sqlite",
            timeout: TimeSpan.FromSeconds(10));

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend",
                policyBuilder =>
                {
                    policyBuilder.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseCors("AllowFrontend");
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseMiddleware<ExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous();

        await ApplyMigrationsAsync(app.Services);
        await SetDefaultCredentials(app.Services);

        await app.RunAsync();
    }

    private static string GetConnectionString(IConfiguration config)
    {
        return config.GetConnectionString("Data") ?? throw new InvalidOperationException(
            "Configuration does not contain required connection string: section 'ConnectionStrings:Data'");
    }

    private static void ConfigureSwagger(SwaggerGenOptions c)
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Trading Bot", Version = "v1" });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);

        c.AddSecurityDefinition("basic", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "basic",
            In = ParameterLocation.Header,
            Description = "Basic Authorization header."
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme, Id = "basic"
                    }
                },
                Array.Empty<string>()
            }
        });
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

    private static Task SetDefaultCredentials(IServiceProvider services)
    {
        var command = services.GetRequiredService<CredentialsCommand>();
        return command.CreateDefaultUserAsync();
    }

    private static void ConfigureConfiguration(IServiceCollection services, IConfiguration config)
    {
        services.Configure<AlpacaConfiguration>(config.GetSection(AlpacaConfiguration.SectionName));
        services.Configure<SeedCredentialsConfiguration>(config.GetSection(SeedCredentialsConfiguration.SectionName));
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(GetConnectionString(config)));

        services.AddMemoryCache(options => options.TrackStatistics = true);

        services.AddSingleton<IFlurlClientFactory, PerBaseUrlFlurlClientFactory>();
        services.AddSingleton<IAlpacaClientFactory, AlpacaClientFactory>();

        services.AddScoped<IMarketDataSource, MarketDataSource>();
        services.AddScoped<IPricePredictor, PricePredictor>();
        services.AddScoped<IAssetsDataSource, AssetsDataSource>();
        services.AddScoped<IStrategy, Strategy>();
        services.AddScoped<IActionExecutor, ActionExecutor>();
        services.AddScoped<IExchangeCalendar, ExchangeCalendar>();
        services.AddScoped<ICurrentTradingTask, CurrentTradingTask>();
        services.AddScoped<TradingTaskExecutor>();

        services.AddTransient<CredentialsCommand>();
        services.AddTransient<ITestModeConfigService, TestModeConfigService>();
        services.AddTransient<IInvestmentConfigService, InvestmentConfigService>();
        services.AddTransient<IStrategyParametersService, StrategyParametersService>();
        services.AddTransient<ITradingActionQuery, TradingActionQuery>();
        services.AddTransient<ITradingActionCommand, TradingActionCommand>();
        services.AddTransient<ITradingTaskCommand, TradingTaskCommand>();
        services.AddTransient<ITradingTaskQuery, TradingTaskQuery>();
        services.AddTransient<IAssetsStateCommand, AssetsStateCommand>();
        services.AddTransient<IAssetsStateQuery, AssetsStateQuery>();
        services.AddTransient<IBacktestCommand, BacktestCommand>();

        services.AddSingleton<IMarketDataCache, MarketDataCache>();
        services.AddSingleton<IAlpacaCallQueue, AlpacaCallQueue>();

        services.AddTransient<BacktestExecutor>();
        services.AddSingleton<IBacktestAssets, BacktestAssets>();
        services.AddSingleton<IBacktestActionExecutor, BacktestActionExecutor>();
    }
}
