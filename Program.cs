using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyStore.Models;
using Newtonsoft.Json.Linq;
using NuGet.Protocol;
using NuGet.Versioning;
using BCr = BCrypt.Net;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder
            .Services
            .AddControllers(options =>
            {
                options
                    .ModelMetadataDetailsProviders
                    .Add(new SystemTextJsonValidationMetadataProvider());
            });
        var Configuration = builder.Configuration;

        builder
            .Services
            .AddDbContext<StoreDb>(
                options => options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"))
            );
        builder.Services.AddEndpointsApiExplorer();

        // builder.Services.AddAuthorization();
        // builder.Services.AddAuthentication().AddJwtBearer().AddJwtBearer("LocalAuthIssuer");

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

        // app.UseCors();
        // app.UseAuthentication();
        // app.UseAuthorization();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Store API");
        });

        app.MapGet("/getItems", async (StoreDb db) => await db.Stores.ToListAsync());
        app.MapPost(
            "/addItems",
            async (StoreDb Db, Store store) =>
            {
                await Db.Stores.AddAsync(store);
                store.Password = BCr.BCrypt.HashPassword(store.Password);
                await Db.SaveChangesAsync();
                return Results.Created("Good", store);
            }
        );
        app.MapPost(
            "/Login",
            async (StoreDb db, Store log) =>
            {
                var check = db.Stores.FirstOrDefault(s => s.Name == log.Name);
                if (check is null)
                    return Results.NotFound("User not found");

                var passCheck = BCr.BCrypt.Verify(log.Password, check.Password);

                if (!passCheck)
                    return Results.BadRequest("Password Not Valid");
                await db.Stores.ToListAsync();
                
                ;
                // var ss = 
                var token = check.ToJToken();
                Console.WriteLine(token);
                return Results.Ok(token);
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

                // static void MyMethod()
                // {
                //     var passwordHash =

                //     var result = BCr.BCrypt.Verify("234", passwordHash);
                //     Console.WriteLine(result);
                // }
                // MyMethod();
                store.Password = update.Password is null
                    ? store.Password
                    : BCr.BCrypt.HashPassword(update.Password);
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
    }
}
