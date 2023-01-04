using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Net.Http.Headers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "APIConsumoCombustivel" }));

builder.Services.AddDbContext<DatabaseContext>(options => options.UseSqlite("Data Source=database.db"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();
app.UseHttpsRedirection();
using (DatabaseContext db = app.Services.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>())
{
    await db.Database.EnsureCreatedAsync();
    await db.Database.MigrateAsync();
}

app.MapPost("/consumo/criar", async (Consumo consumo, DatabaseContext db) =>
{
    if (consumo == null) return Results.BadRequest("Request Inválido");

    db.Consumos.Add(consumo);
    await db.SaveChangesAsync();
    return Results.Created($"/consumo/{consumo.Id}", consumo);
}).WithName("CriarConsumo");

app.MapGet("/consumo/media", async (DatabaseContext db) =>
{
    List<Consumo> lista = await Task.Run(() => db.Consumos.ToListAsync().Result.OrderByDescending(o => o.DataAbastecido).Take(2).ToList());

    if (lista.Count != 2) return Results.BadRequest("Cadastre mais de 2 históricos");

    Consumo ultimo = lista.First();
    Consumo penultimo = lista.Last();

    double consumoMedio = (ultimo.Quilometragem - penultimo.Quilometragem) / ultimo.Litragem;

    return Results.Ok(Math.Round(consumoMedio, 2));

}).WithName("ObterMediaConsumo");

app.MapGet("/consumo/autonomia", async (DatabaseContext db) =>
{
    string retorno;
    double media;
    double tanque = 19.2;

    try
    {
        retorno = await new HttpClient().GetStringAsync("https://localhost:44364/consumo/media");
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Falha ao obter média: {ex.Message}");
    }

    try
    {
        media = Convert.ToDouble(retorno.Replace(".", ","));
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Falha ao converter valor: {ex.Message}");
    }

    return Results.Ok(media * tanque);
}).WithName("ObterAutonomia");

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

class Consumo
{
    public Consumo(double valor, int quilometragem, double litragem, DateTime dataAbastecido)
    {
        Valor = valor;
        Quilometragem = quilometragem;
        Litragem = litragem;
        DataAbastecido = dataAbastecido;
    }

    public int Id { get; set; }
    public double Valor { get; set; }
    public int Quilometragem { get; set; }
    public double Litragem { get; set; }
    public DateTime DataAbastecido { get; set; }
}

class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options)
        : base(options) { }
    public DbSet<Consumo> Consumos { get; set; }
}
