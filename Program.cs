using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;
using Microsoft.OpenApi.Models;
using MyStore.Models;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services
    .AddControllers(options =>
    {
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
    });
var Configuration = builder.Configuration;

builder
    .Services
    .AddDbContext<StoreDb>(
        options => options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"))
    );
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication().AddJwtBearer().AddJwtBearer("LocalAuthIssuer");

builder
    .Services
    .AddAuthorizationBuilder()
    .AddPolicy(
        "admin_greetings",
        policy => policy.RequireRole("admin").RequireClaim("scope", "greetings_api")
    );
builder
    .Services
    .AddSwaggerGen(c =>
    {
        c.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title = "Store API",
                Description = "Store API",
                Version = "v1"
            }
        );
    });
builder.Logging.AddSimpleConsole(i => i.ColorBehavior = LoggerColorBehavior.Disabled);
var app = builder.Build();
app.UseSwagger();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Store API");
});
app.MapGet(
    "/Test",
    async (ILogger<Program> logger, HttpResponse response) =>
    {
        logger.LogInformation("Testing logging in Program.cs");
        await response.WriteAsync("Testing");
    }
);
app.MapGet("/getItems", async (StoreDb db) => await db.Stores.ToListAsync());
app.MapPost(
    "/addItems",
    async (StoreDb Db, Store store) =>
    {
        await Db.Stores.AddAsync(store);
        byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
        string hashed = Convert.ToBase64String(
            KeyDerivation.Pbkdf2(
                password: store.Password!,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8
            )
        );

        store.Password = hashed;
        await Db.SaveChangesAsync();
        return Results.Created("Good", store);
    }
);
app.MapPost(
    "/Login",
    async (StoreDb db, Store log) =>
    {
        var store = db.Stores.Where(s => s.Name == log.Name & s.Password == log.Password);

        if (store is null)
            return Results.NotFound();

        await db.Stores.ToListAsync();
        return Results.Ok(store);
    }
);

app.MapGet("/item/{id}", async (StoreDb db, int id) => await db.Stores.FindAsync(id));
app.MapPut(
    "/updateItem/{id}",
    async (StoreDb db, Store update, int id) =>
    {
        var store = await db.Stores.FindAsync(id);
        if (store is null)
            return Results.NotFound();
        store.Name = update.Name is null ? store.Name : update.Name;
        byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
        string hashed = Convert.ToBase64String(
            KeyDerivation.Pbkdf2(
                password: update.Password!,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8
            )
        );

        store.Password = hashed;
        store.Email = update.Email is null ? store.Email : update.Email;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
);
app.MapDelete(
    "/delItems/{id}",
    async (StoreDb db, int id) =>
    {
        var store = await db.Stores.FindAsync(id);
        if (store is null)
            return Results.NotFound();
        db.Stores.Remove(store);
        await db.SaveChangesAsync();
        return Results.Ok();
    }
);
app.Run();
