using Microsoft.OpenApi.Models;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ViaCepMinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal API para buscar CEP",
        Version = "v1",
        Description = "Minimal API RestFUL para busca de CEP",
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

JsonSerializerOptions s_readOptions = new()
{
    PropertyNameCaseInsensitive = true
};

app.MapGet("/search-cep/{cep}", async (string cep) =>
{
    if (string.IsNullOrEmpty(cep))
        return Results.BadRequest(new { Message = "CEP is required." });

    var cepPattern = @"^\d{5}-\d{3}$";

    if (!Regex.IsMatch(cep, cepPattern))
        return Results.BadRequest(new { Message = "CEP must be in the format 00000-000." });

    using var httpClient = new HttpClient();

    try
    {
        var response = await httpClient.GetAsync($"https://viacep.com.br/ws/{cep}/json/");

        if (!response.IsSuccessStatusCode)
            return Results.Json(new { Message = "Error when searching the postal code." }, statusCode: (int)response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<CepResponse>(content, s_readOptions);

        if (data == null || string.IsNullOrWhiteSpace(data.Cep))
            return Results.NotFound(new { Message = "CEP not found." });

        return Results.Ok(data);
    }
    catch (HttpRequestException ex)
    {
        return Results.Json(new { Message = "Error connecting to the CEP service.", Details = ex.Message }, statusCode: ((int)HttpStatusCode.InternalServerError));
    }
})
    .WithName("SearchCEP")
    .WithTags("CEP")
    .WithDescription("Search CEP");

app.Run();