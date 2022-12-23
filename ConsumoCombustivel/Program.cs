using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

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
}).WithName("CreateConsumo")
 .ProducesValidationProblem()
 .Produces<Consumo>(StatusCodes.Status201Created);

app.MapGet("/consumo/media", async (DatabaseContext db) =>
{
    List<Consumo> lista = await Task.Run(() => db.Consumos.ToListAsync().Result.OrderByDescending(o => o.DataAbastecido).Take(2).ToList());

    if (lista.Count != 2) return Results.BadRequest("Cadastre mais de 2 históricos");

    Consumo ultimo = lista.First();
    Consumo penultimo = lista.Last();

    double consumoMedio = (ultimo.Kilometragem - penultimo.Kilometragem) / ultimo.Litragem;

    return Results.Ok(Math.Round(consumoMedio, 2));
}).WithName("ObterMediaConsumo")
 .Produces(StatusCodes.Status200OK);

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

public class Consumo
{
    public Consumo(double valor, double capacidadeTotal, int kilometragem, double litragem, DateTime dataAbastecido)
    {
        Valor = valor;
        CapacidadeTotal = capacidadeTotal;
        Kilometragem = kilometragem;
        Litragem = litragem;
        DataAbastecido = dataAbastecido;
    }

    public int Id { get; set; }
    public double Valor { get; set; }
    public double CapacidadeTotal { get; set; }
    public int Kilometragem { get; set; }
    public double Litragem { get; set; }
    public DateTime DataAbastecido { get; set; }
}

class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options)
        : base(options) { }
    public DbSet<Consumo> Consumos { get; set; }
}
