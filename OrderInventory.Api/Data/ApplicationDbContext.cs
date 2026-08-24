using Microsoft.EntityFrameworkCore;
using OrderInventory.Api.Models; 
namespace OrderInventory.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(150);
                entity.Property(p => p.SKU).IsRequired().HasMaxLength(50);
                entity.HasIndex(p => p.SKU).IsUnique(); // Unique index requirement
                entity.Property(p => p.Price).HasColumnType("decimal(18,2)"); // Precision for money
            });

            // 2. Category configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            });

            // 3. Customer configuration
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(cu => cu.Id);
                entity.Property(cu => cu.FullName).IsRequired().HasMaxLength(100);
                entity.Property(cu => cu.Email).IsRequired().HasMaxLength(150);
                entity.HasIndex(cu => cu.Email).IsUnique(); // Optional unique index for safety
            });

            // 4. Order configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Status).IsRequired().HasMaxLength(50);
                entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");

                // Delete Behavior: Restrict/NoAction taake customer delete hone par orders ghalti se delete na hon
                entity.HasOne(o => o.Customer)
                      .WithMany(c => c.Orders)
                      .HasForeignKey(o => o.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 5. OrderItem configuration
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(oi => oi.Id);
                entity.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(oi => oi.LineTotal).HasColumnType("decimal(18,2)");

                
                entity.HasOne(oi => oi.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(oi => oi.Product)
                      .WithMany(p => p.OrderItems)
                      .HasForeignKey(oi => oi.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}