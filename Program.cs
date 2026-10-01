using DotNetEnv;
using Marten;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No se encontró SUPABASE_CONNECTION_STRING en el archivo .env."
    );
}

builder.Services
    .AddMarten(options =>
    {
        options.Connection(connectionString);
    })
    .UseLightweightSessions()
    .ApplyAllDatabaseChangesOnStartup();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/clientes", async (Cliente cliente, IDocumentSession session) =>
{
    session.Store(cliente);
    await session.SaveChangesAsync();

    return Results.Ok(cliente);
});

app.MapGet("/clientes", async (IQuerySession session) =>
{
    var clientes = await session.Query<Cliente>().ToListAsync();

    return Results.Ok(clientes);
});

app.Run();

public record Cliente(
    Guid Id,
    string Nombre,
    string Email
);