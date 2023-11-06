using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TradingBot.Configuration;
using TradingBot.Database;
using TradingBot.Services;

var builder = WebApplication.CreateBuilder(args);

ConfigureConfiguration(builder.Services, builder.Configuration);
ConfigureServices(builder.Services, builder.Configuration);

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
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>()
        .CreateDbContextAsync();
    await context.Database.MigrateAsync();
}

app.Run();

void ConfigureServices(IServiceCollection services, IConfiguration config)
{
    services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(config.GetConnectionString("Data")));
    services.AddSingleton<IAlpacaClientFactory, AlpacaClientFactory>();
    services.AddScoped<IMarketDataSource, MarketDataSource>();
    services.AddScoped<IPricePredictor, PricePredictor>();
    services.AddScoped<IStrategy, Strategy>();
    services.AddScoped<IActionExecutor, ActionExecutor>();
}

void ConfigureConfiguration(IServiceCollection services, IConfiguration config)
{
    services.Configure<AlpacaConfiguration>(config.GetSection(AlpacaConfiguration.SectionName));
}
