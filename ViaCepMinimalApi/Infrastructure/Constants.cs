namespace ViaCepMinimalApi.Infrastructure
{
    static public class Constants
    {
        public const string ViaCepApiUrl = "https://viacep.com.br/ws";
        public const string CepRegexPattern = @"^\d{5}-\d{3}$";
        public const string UfRegexPattern = @"^[A-Z]{2}$";
    }
}
