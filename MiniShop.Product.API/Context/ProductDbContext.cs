using Microsoft.EntityFrameworkCore;

namespace MiniShop.Product.API.Context;

public sealed class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }
    public DbSet<MiniShop.Product.API.Models.Product> Products { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MiniShop.Product.API.Models.Product>(entity =>
        {
            entity.Property(p => p.Price)
                  .HasColumnType("money");
        });
    }
}
