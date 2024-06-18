namespace Quoter.Api;

public class AppSettings
{
    public string? CoinMarketCapApiKey { get; set; }
    public string? CoinMarketCapApiUrl { get; set; }
    public string? ExchangeRatesApiUrl { get; set; }

    public string? ExchangeRatesApiKey { get; set; }
    public string? BaseCurrency { get; set; }
    public string[]? TargetCurrencies { get; set; }
    public int CacheSlidingExpirationMinutes { get; set; }
}