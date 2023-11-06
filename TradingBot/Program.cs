using System.Reflection;
using Alpaca.Markets;
using Environments = Alpaca.Markets.Environments;

var builder = WebApplication.CreateBuilder(args);

var alpacaClient = Environments.Paper
    .GetAlpacaTradingClient(
        new SecretKey(
            builder.Configuration["AlpacaApi:Key"] ?? throw new InvalidOperationException("Alpaca API key is missing"),
            builder.Configuration["AlpacaApi:Secret"] ??
            throw new InvalidOperationException("Alpaca API secret is missing")
        ));

var alpacaAccount = await alpacaClient.GetAccountAsync();
Console.WriteLine(alpacaAccount.ToString());

// Add services to the container.

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
