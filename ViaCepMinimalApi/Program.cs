using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ViaCepMinimalApi.Infrastructure;
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

    if (!Regex.IsMatch(cep, Constants.CepRegexPattern))
        return Results.BadRequest(new { Message = "CEP must be in the format 00000-000." });

    using var httpClient = new HttpClient();

    try
    {
        var response = await httpClient.GetAsync($"{Constants.ViaCepApiUrl}/{cep}/json/");

        if (!response.IsSuccessStatusCode)
            return Results.Json(new { Message = "Error when searching the postal code." }, statusCode: (int)response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, s_readOptions);
        if (errorResponse?.Erro == true)
            return Results.NotFound(new { Message = "Postal code not found." });

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
.WithDescription("Search CEP")
.WithOpenApi(op => new(op)
{
    Summary = "Search postal code information",
    Description = "Fetch details for a given postal code in the format 00000-000.",
    Parameters = [
        new() {
            Name = "cep",
            In = ParameterLocation.Path,
            Required = true,
            Schema = new OpenApiSchema { Type = "string", Example = new OpenApiString("01001-000") },
            Example = new OpenApiString("01001-000"),
        }
    ]
});

app.MapGet("/search-cep/{uf}/{cidade}/{logradouro}", async (string uf, string cidade, string logradouro) =>
{
    if (string.IsNullOrWhiteSpace(uf) || string.IsNullOrWhiteSpace(cidade) || string.IsNullOrWhiteSpace(logradouro))
        return Results.BadRequest(new { Message = "State, city, and street are required." });

    if (!Regex.IsMatch(uf, Constants.UfRegexPattern))
        return Results.BadRequest(new { Message = "The state must be a valid two-letter abbreviation." });

    using var httpClient = new HttpClient();

    try
    {
        // Chamada à API do ViaCEP
        var response = await httpClient.GetAsync($"{Constants.ViaCepApiUrl}/{uf}/{cidade}/{logradouro}/json/");

        if (!response.IsSuccessStatusCode)
            return Results.Json(new { Message = "Error when searching the address." }, statusCode: (int)response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<List<CepResponse>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (data == null || data.Count == 0)
            return Results.NotFound(new { Message = "Address not found." });

        return Results.Ok(data);
    }
    catch (HttpRequestException ex)
    {
        return Results.Json(new { Message = "Error connecting to the address service.", Details = ex.Message }, statusCode: 500);
    }
})

.WithName("SearchAddress")
.WithTags("Address")
.WithOpenApi(op => new(op)
{
    Summary = "Search address information",
    Description = "Fetch details for addresses matching the provided state, city, and street.",
    Parameters = [
        new() {
            Name = "uf",
            In = ParameterLocation.Path,
            Required = true,
            Schema = new OpenApiSchema { Type = "string", Example = new OpenApiString("SP") },
            Example = new OpenApiString("SP"),
        },
        new() {
            Name = "cidade",
            In = ParameterLocation.Path,
            Required = true,
            Schema = new OpenApiSchema { Type = "string", Example = new OpenApiString("Sao Paulo") },
            Example = new OpenApiString("Sao Paulo"),
        },
        new() {
            Name = "logradouro",
            In = ParameterLocation.Path,
            Required = true,
            Schema = new OpenApiSchema { Type = "string", Example = new OpenApiString("Praça") },
            Example = new OpenApiString("Praça"),
        }
    ]
});

app.Run();