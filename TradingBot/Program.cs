using System.Reflection;
using TradingBot.Configuration;
using TradingBot.Services;

var builder = WebApplication.CreateBuilder(args);

ConfigureConfiguration(builder.Services, builder.Configuration);
ConfigureServices(builder.Services);

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
app.Run();

void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<IMarketDataSource, MarketDataSource>();
    services.AddScoped<IPricePredictor, PricePredictor>();
    services.AddScoped<IStrategy, Strategy>();
    services.AddScoped<IActionExecutor, ActionExecutor>();
}

void ConfigureConfiguration(IServiceCollection services, IConfiguration config)
{
    services.Configure<AlpacaConfiguration>(config.GetSection(AlpacaConfiguration.SectionName));
}
