using Bogus;
using Microsoft.EntityFrameworkCore;
using MiniShop.Product.API.Context;
using MiniShop.Product.API.Dtos;
using MiniShop.Product.API.Models;
using System;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL baglantisi ve DbContext yapilandirmasi
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/api/products/{id:guid}", async (Guid id, ProductDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        var product = await dbContext.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null)
        {
            return Results.NotFound(new { Message = "Ürün bulunamadý." });
        }

        var productDto = new ProductDto(product.Name, product.Description, product.Price, product.Stock);
        return Results.Ok(productDto);
    }
    catch (OperationCanceledException)
    {
        return Results.StatusCode(499); // Client Closed Request
    }
});


// Define the minimal API endpoint with cancellation token support
app.MapGet("/api/products", async (ProductDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        var products = await dbContext.Products.ToListAsync(cancellationToken);
        return Results.Ok(products);
    }
    catch (OperationCanceledException)
    {
        return Results.StatusCode(499); // HTTP Status 499: Client Closed Request
    }
});

// Define the minimal API endpoint to create a new product using ProductDto
app.MapPost("/api/products", async (ProductDbContext dbContext, ProductDto productDto, CancellationToken cancellationToken) =>
{
    try
    {
        // Map ProductDto to Product entity
        var product = new Product
        {
            Name = productDto.Name,
            Description = productDto.Description,
            Price = productDto.Price,
            Stock = productDto.Stock
        };

        // Add the new product to the database
        await dbContext.Products.AddAsync(product, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Return the created product with 201 status
        return Results.Created($"/api/products/{product.Id}", product);
    }
    catch (OperationCanceledException)
    {
        return Results.StatusCode(499); // Client Closed Request
    }
});

// Minimal API endpoint: Sahte ürünler ekle
app.MapGet("/api/seed-products", async (ProductDbContext dbContext, CancellationToken cancellationToken) =>
{
    // Veritabanýnda daha önce ürün var mý kontrol ediliyor
    if (!await dbContext.Products.AnyAsync(cancellationToken))
    {
        // Bogus kullanarak sahte ürünler oluþturuluyor
        var faker = new Faker<Product>("tr")
            .RuleFor(p => p.Id, f => Guid.NewGuid())
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Lorem.Sentence())
            .RuleFor(p => p.Price, f => f.Random.Decimal(10, 500))
            .RuleFor(p => p.Stock, f => f.Random.Int(1, 100));

        var fakeProducts = faker.Generate(10); // 10 ürün oluþtur
        await dbContext.Products.AddRangeAsync(fakeProducts, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { Message = "10 sahte ürün baþarýyla eklendi." });
    }

    return Results.BadRequest(new { Message = "Ürünler zaten mevcut." });
});

// Migration'larý otomatik olarak uygula
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await dbContext.Database.MigrateAsync(); // Migration'larý uygula
}

app.Run();
