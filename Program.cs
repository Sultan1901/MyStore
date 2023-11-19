using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using MyStore.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers(options =>
{
    options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
});
var Configuration = builder.Configuration;
builder.Services.AddDbContext<StoreDb>(options =>options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddDbContext<StoreDb>(options => options.UseNpgsql(Configuration.GetConnectionString("items")));
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Store API",
        Description = "Store API",
        Version = "v1"
    });
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Store API");
});
app.MapGet("/getItems", async (StoreDb db) => await db.Stores.ToListAsync());
app.MapPost("/addItems", async (StoreDb Db, Store store) =>
{

    await Db.Stores.AddAsync(store);
    await Db.SaveChangesAsync();
    return Results.Created("Good", store);
});
app.MapGet("/item/{id}", async (StoreDb db, int id) => await db.Stores.FindAsync(id));
app.MapPut("/updateItem/{id}", async (StoreDb db, Store update, int id) =>
{
    var store = await db.Stores.FindAsync(id);
    if (store is null) return Results.NotFound();
    store.Name = update.Name is null ? store.Name : update.Name;
    store.Description = update.Description is null ? store.Description : update.Description;
    await db.SaveChangesAsync();
    return Results.NoContent();

});
app.MapDelete("/delItems/{id}", async (StoreDb db, int id) =>
{
    var store = await db.Stores.FindAsync(id);
    if (store is null) return Results.NotFound();
    db.Stores.Remove(store);
    await db.SaveChangesAsync();
    return Results.Ok();
});
app.Run();
