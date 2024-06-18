using FluentValidation;
using Quoter.Api.Helper;


namespace Quoter.Api.Validators;

public class GetQuotesRequestValidator : AbstractValidator<string>
{
    public GetQuotesRequestValidator()
    {
        RuleFor(quote => quote)
            .NotNull().WithMessage(ErrorMessages.IsRequired(ErrorMessages.CryptocurrencySymbol))
            .NotEmpty().WithMessage(ErrorMessages.IsRequired(ErrorMessages.CryptocurrencySymbol))
            .MaximumLength(50).WithMessage(ErrorMessages.MustBeLessThan(ErrorMessages.CryptocurrencySymbol, 50))
            .Matches("^[a-zA-Z0-9]+$").WithMessage(ErrorMessages.MustBeAlphaNumeric);
    }
}
