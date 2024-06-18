using CSharpFunctionalExtensions;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Quoter.Api.Repositories;

public interface ICryptocurrencyRepository
{
    Task<Result<decimal>> GetCryptocurrencyPriceInUsdAsync(string code);
    Task<Result<Dictionary<string, decimal>>> GetExchangeRatesAsync();

}

public class CryptocurrencyRepository : ICryptocurrencyRepository
{
    private readonly AppSettings _appSettings;

    public CryptocurrencyRepository(AppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public async Task<Result<decimal>> GetCryptocurrencyPriceInUsdAsync(string code)
    {
        try
        {
            var capCode = code.ToUpper();
            var client = new RestClient(_appSettings.CoinMarketCapApiUrl!);
            const string requestUrl = "/v1/cryptocurrency/quotes/latest";
            var request = new RestRequest(requestUrl);
            request.AddParameter("symbol", capCode);
            request.AddHeader("X-CMC_PRO_API_KEY", _appSettings.CoinMarketCapApiKey!);
            var response = await client.ExecuteAsync(request);
            var json = JObject.Parse(response.Content!);
            var data = json["data"];
            if (json?["data"]?[capCode] == null)
            {
                return Result.Failure<decimal>($"No Cryptocurrency symbol found: {code}");
            }

            return Result.Success(json["data"]![capCode]!["quote"]!["USD"]!["price"]!.ToObject<decimal>());

        }
        catch (Exception e)
        {
            return Result.Failure<decimal>(e.Message);
        }
    }

    public async Task<Result<Dictionary<string, decimal>>> GetExchangeRatesAsync()
    {
        try
        {
            
            var client = new RestClient(_appSettings.ExchangeRatesApiUrl!);
            const string requestUrl = "/latest";
            var request = new RestRequest(requestUrl, method: Method.Get);
            request.AddParameter("access_key", _appSettings.ExchangeRatesApiKey);
            var response = await client.ExecuteAsync(request);
            var json = JObject.Parse(response.Content!);


            return Result.Success(json["rates"].ToObject<Dictionary<string, decimal>>());
        }
        catch (Exception e)
        {
            return Result.Failure<Dictionary<string, decimal>>(e.Message);
        }
    }
}