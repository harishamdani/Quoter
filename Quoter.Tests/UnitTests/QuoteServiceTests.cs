using AutoFixture;
using AutoFixture.AutoMoq;
using CSharpFunctionalExtensions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Quoter.Api.Helper;
using Quoter.Api.Repositories;
using Quoter.Api.Services;
using Quoter.Api.Validators;
using Quoter.Tests.UnitTests.Common;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Quoter.Tests.UnitTests;

[TestFixture]
public class QuoteValidatorService
{
    private GetQuotesRequestValidator _sut;
    private IFixture _fixture;

    [SetUp]
    public void Setup()
    {
        _sut = new GetQuotesRequestValidator();
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
    }

    [TestCase("")]
    public void When_Symbol_IsEmpty_Should_Fail_Validation(string? code)
    {
        var result = _sut.Validate(code!);

        result
            .Errors
            .Should()
            .NotBeEmpty();

        result.Errors.Any(x => x.ErrorMessage.Contains(ErrorMessages.IsRequired(ErrorMessages.CryptocurrencySymbol))).Should().BeTrue();
    }

    [Test]
    public void When_Symbol_IsEmpty_Should_Fail_Validation()
    {
        var code = string.Empty;
        while (code.Length < 51)
        {
            code = _fixture
                .Create<string>();
        }

        code = code[..51];

        var result = _sut.Validate(code);

        result
            .Errors
            .Should()
            .NotBeEmpty();

        result.Errors.Any(x => x.ErrorMessage.Contains(ErrorMessages.MustBeLessThan(ErrorMessages.CryptocurrencySymbol, 50))).Should().BeTrue();
    }
}

[TestFixture]
public class QuotesServiceTests
{
    private QuoteServiceMockService _mockService;
    private QuoteService _sut;

    [SetUp]
    public void Setup()
    {
        _mockService = new QuoteServiceMockService();
        _sut = _mockService.Create();
    }

    [Test]
    public async Task GetQuotesAsync_When_Validation_Failed_Should_Return_Failure()
    {
        _mockService
            .WithValidation(new ValidationResult()
            {
                Errors =
                    [
                        new ValidationFailure
                        {
                            PropertyName = null,
                            ErrorMessage = "someError",
                            AttemptedValue = null,
                            CustomState = null,
                            Severity = Severity.Error,
                            ErrorCode = null,
                            FormattedMessagePlaceholderValues = null
                        }
                    ]
            }
            );

        const string code = "btc";
        var result = await _sut.GetQuotesAsync(code);

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();

        _mockService.Validator.Verify(x => x.Validate(It.IsAny<string>()), Times.Once);
        _mockService.CryptocurrencyRepository.Verify(x => x.GetCryptocurrencyPriceInUsdAsync(It.IsAny<string>()), Times.Never);
        _mockService.CryptocurrencyRepository.Verify(x => x.GetExchangeRatesAsync(), Times.Never);
    }

    [Test]
    public async Task GetQuotesAsync_When_Fetch_Value_Failed_Should_Return_Failure()
    {
        const string errorMessage = "someError";
        var getPriceResult = Result.Failure<decimal>(errorMessage);
        _mockService
            .WithValidation(new ValidationResult())
            .WithGetPrice(getPriceResult);

        const string code = "btc";
        var result = await _sut.GetQuotesAsync(code);

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);

        _mockService.Validator.Verify(x => x.Validate(It.IsAny<string>()), Times.Once);
        _mockService.CryptocurrencyRepository.Verify(x => x.GetCryptocurrencyPriceInUsdAsync(It.IsAny<string>()), Times.Once);
        _mockService.CryptocurrencyRepository.Verify(x => x.GetExchangeRatesAsync(), Times.Never);
    }

    [Test]
    public async Task GetQuotesAsync_When_Fetch_ExchangeRates_Failed_Should_Return_Failure()
    {
        var errorMessage = _mockService.Fixture.Create<string>();
        var getPriceResult = Result.Success(_mockService.Fixture.Create<decimal>());
        var getRatesResult = Result.Failure<Dictionary<string, decimal>>(errorMessage);
        _mockService
            .WithValidation(new ValidationResult())
            .WithGetPrice(getPriceResult)
            .WithGetRates(getRatesResult);

        const string code = "btc";
        var result = await _sut.GetQuotesAsync(code);

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);

        _mockService.Validator.Verify(x => x.Validate(It.IsAny<string>()), Times.Once);
        _mockService.CryptocurrencyRepository.Verify(x => x.GetCryptocurrencyPriceInUsdAsync(It.IsAny<string>()), Times.Once);
        _mockService.CryptocurrencyRepository.Verify(x => x.GetExchangeRatesAsync(), Times.Once);
    }

    [Test]
    public async Task GetQuotesAsync_When_Fetch_Success_Should_Return_All_Expected_Values()
    {
        var getPriceResult = Result.Success(_mockService.Fixture.Create<decimal>());
        HashSet<string> currencies = ["USD", "EUR", "BRL", "AUD", "GBP"];
        var rates = new Dictionary<string, decimal>
        {
            { "USD", 1000 },
            { "EUR", 700 },
            { "BRL", 50 },
            { "AUD", 501 },
            { "GBP", 502 },
            { "CHF", 503 },
        }
        .OrderBy(x => x.Key)
        .ToDictionary(x => x.Key, x => x.Value);

        var expectedResult = rates
            .Where(x => currencies.Contains(x.Key))
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value * getPriceResult.Value);

        var getRatesResult = Result.Success(rates);
        _mockService
            .WithValidation(new ValidationResult())
            .WithGetPrice(getPriceResult)
            .WithGetRates(getRatesResult);

        const string code = "btc";
        var result = await _sut.GetQuotesAsync(code);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().NotBeEmpty();
        result.Value.Count.Should().Be(expectedResult.Count);
        result
            .Value
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value)
            .ToJson()
            .Should()
            .Be(expectedResult.ToJson());

        _mockService.Validator.Verify(x => x.Validate(It.IsAny<string>()), Times.Once);
        _mockService.CryptocurrencyRepository.Verify(x => x.GetCryptocurrencyPriceInUsdAsync(It.IsAny<string>()), Times.Once);
        _mockService.CryptocurrencyRepository.Verify(x => x.GetExchangeRatesAsync(), Times.Once);
    }
}

public class QuoteServiceMockService : MockService<QuoteService>
{
    public Mock<IValidator<string>> Validator => Get<IValidator<string>>();
    public Mock<ICryptocurrencyRepository> CryptocurrencyRepository => Get<ICryptocurrencyRepository>();

    public QuoteServiceMockService WithValidation(ValidationResult result)
    {
        Validator
            .Setup(x => x.Validate(It.IsAny<string>()))
            .Returns(result);

        return this;
    }

    public QuoteServiceMockService WithGetPrice(Result<decimal> result)
    {
        CryptocurrencyRepository
            .Setup(x => x.GetCryptocurrencyPriceInUsdAsync(It.IsAny<string>()))
            .ReturnsAsync(result);

        return this;
    }

    public QuoteServiceMockService WithGetRates(Result<Dictionary<string, decimal>> result)
    {
        CryptocurrencyRepository
            .Setup(x => x.GetExchangeRatesAsync())
            .ReturnsAsync(result);

        return this;
    }
}
