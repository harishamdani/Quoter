using FluentValidation;
using Quoter.Api;
using Quoter.Api.Repositories;
using Quoter.Api.Services;
using Quoter.Api.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
DotNetEnv.Env.Load();

var appSettings = new AppSettings
{
    CoinMarketCapApiKey = Environment.GetEnvironmentVariable("COINMARKETCAP_API_KEY"),
    ExchangeRatesApiKey = Environment.GetEnvironmentVariable("EXCHANGERATESAPI_API_KEY"),
    BaseCurrency = Environment.GetEnvironmentVariable("BASE_CURRENCY"),
    TargetCurrencies = Environment.GetEnvironmentVariable("TARGET_CURRENCIES")?.Split(',') ?? [],
    CacheSlidingExpirationMinutes = int.Parse(Environment.GetEnvironmentVariable("CACHE_SLIDING_EXPIRATION_MINUTES")),
    CoinMarketCapApiUrl = Environment.GetEnvironmentVariable("COINMARKETCAP_API_URL"),
    ExchangeRatesApiUrl = Environment.GetEnvironmentVariable("EXCHANGERATESAPI_URL") ?? string.Empty
};

builder.Services.AddSingleton(appSettings);
builder.Services.AddScoped<ICryptocurrencyRepository, CryptocurrencyRepository>();
builder.Services.AddScoped<IValidator<string>, GetQuotesRequestValidator>();
builder.Services.AddScoped<IQuoteService, QuoteService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();