using Dapper;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Conexión a PostgreSQL: puerto 5432 y SIN contraseña
var connectionString = "Host=localhost;Port=5432;Database=empleados;Username=postgres";

builder.Services.AddScoped(_ => new NpgsqlConnection(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// ====== CREA LA TABLA AUTOMÁTICAMENTE AL ENCENDER LA API ======
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
    await db.ExecuteAsync(@"
        CREATE TABLE IF NOT EXISTS empleados (
            id SERIAL PRIMARY KEY,
            nombre VARCHAR(100) NOT NULL,
            apellido VARCHAR(100) NOT NULL,
            puesto VARCHAR(100) NOT NULL,
            salario NUMERIC(10,2) NOT NULL
        )");
}

// ====== ENDPOINTS (los "botones" de tu API) ======

// Ver todos
app.MapGet("/empleados", async (NpgsqlConnection db) =>
{
    var lista = await db.QueryAsync<Empleado>(
        "SELECT id, nombre, apellido, puesto, salario FROM empleados ORDER BY id");
    return Results.Ok(lista);
});

// Ver uno por id
app.MapGet("/empleados/{id}", async (int id, NpgsqlConnection db) =>
{
    var emp = await db.QueryFirstOrDefaultAsync<Empleado>(
        "SELECT id, nombre, apellido, puesto, salario FROM empleados WHERE id = @id",
        new { id });
    return emp is null ? Results.NotFound() : Results.Ok(emp);
});

// Crear
app.MapPost("/empleados", async (Empleado emp, NpgsqlConnection db) =>
{
    var nuevoId = await db.ExecuteScalarAsync<int>(
        @"INSERT INTO empleados (nombre, apellido, puesto, salario)
          VALUES (@Nombre, @Apellido, @Puesto, @Salario)
          RETURNING id", emp);
    emp.Id = nuevoId;
    return Results.Created($"/empleados/{nuevoId}", emp);
});

// Editar
app.MapPut("/empleados/{id}", async (int id, Empleado emp, NpgsqlConnection db) =>
{
    var filas = await db.ExecuteAsync(
        @"UPDATE empleados
          SET nombre = @Nombre, apellido = @Apellido, puesto = @Puesto, salario = @Salario
          WHERE id = @id",
        new { emp.Nombre, emp.Apellido, emp.Puesto, emp.Salario, id });
    return filas == 0 ? Results.NotFound() : Results.NoContent();
});

// Borrar
app.MapDelete("/empleados/{id}", async (int id, NpgsqlConnection db) =>
{
    var filas = await db.ExecuteAsync("DELETE FROM empleados WHERE id = @id", new { id });
    return filas == 0 ? Results.NotFound() : Results.NoContent();
});

app.Run();

// ====== MODELO (siempre al final del archivo) ======
public class Empleado
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Puesto { get; set; } = "";
    public decimal Salario { get; set; }
}