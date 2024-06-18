namespace Quoter.Api.Helper
{
    public static class ErrorMessages
    {
        public static string CryptocurrencySymbol = "Cryptocurrency symbol";
        public static string IsRequired(string parameter) => $"{parameter} is required.";
        public static string MustBeLessThan(string parameter, int length) => $"{parameter} must be less than {length} characters";

        public static string MustBeAlphaNumeric(string parameter) =>
            $"{parameter} must contain only alphanumeric characters.";
    }
}
