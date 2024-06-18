using CSharpFunctionalExtensions;
using FluentValidation;
using Quoter.Api.Repositories;

namespace Quoter.Api.Services;

public interface IQuoteService
{
    Task<Result<Dictionary<string, decimal>>> GetQuotesAsync(string code);
}

public class QuoteService(
    IValidator<string> validator,
    ICryptocurrencyRepository currencyRepository)
    : IQuoteService
{
    public async Task<Result<Dictionary<string, decimal>>> GetQuotesAsync(string code)
    {
        var validateResult = ValidateCode(code);
        if (!validateResult.IsSuccess)
        {
            return Result.Failure<Dictionary<string, decimal>>(validateResult.Error);
        }

        return await GetCryptocurrencyPrice(code)
            .Bind(async quote => 
                await GetExchangeRates()
                    .Map(rates => CreateResponse(quote, rates)));
    }

    private Result ValidateCode(string code)
    {
        var validationResult = validator.Validate(code);
        return validationResult.IsValid ? Result.Success() : Result.Failure<string>(validationResult.ToString());
    }

    private async Task<Result<decimal>> GetCryptocurrencyPrice(string code)
    {
        var quoteResult = await currencyRepository.GetCryptocurrencyPriceInUsdAsync(code);
        return quoteResult.IsSuccess ? Result.Success(quoteResult.Value) : Result.Failure<decimal>(quoteResult.Error);
    }

    private async Task<Result<Dictionary<string, decimal>>> GetExchangeRates()
    {
        var exchangeRatesResult = await currencyRepository.GetExchangeRatesAsync();
        return exchangeRatesResult.IsSuccess ? Result.Success(exchangeRatesResult.Value) : Result.Failure<Dictionary<string, decimal>>(exchangeRatesResult.Error);
    }

    private static Dictionary<string, decimal> CreateResponse(decimal quote, Dictionary<string, decimal> exchangeRates)
    {
        return Helper.Constants.Currencies
            .ToHashSet()
            .ToDictionary(currency => currency, currency => (quote * exchangeRates[currency]));
    }
}