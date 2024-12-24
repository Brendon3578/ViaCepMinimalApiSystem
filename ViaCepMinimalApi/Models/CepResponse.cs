using System.Text.Json.Serialization;

namespace ViaCepMinimalApi.Models
{
    public record CepResponse(
        string Cep,
        string Logradouro,
        string Complemento,
        string Bairro,
        string Localidade,
        string Uf,
        string Ibge,
        string Gia,
        string Ddd,
        string Siafi
    );

    public record ErrorResponse(
        [property: JsonPropertyName("erro")]
        bool Erro
    );
}