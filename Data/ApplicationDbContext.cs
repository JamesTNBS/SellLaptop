using Microsoft.EntityFrameworkCore;
using Laptop.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;   // Add this line

namespace Laptop.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductFeature> ProductFeatures { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Price precision
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CartItem>()
                .Property(c => c.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.Price)
                .HasPrecision(18, 2);

            // Unique constraint for CartItem (Product + User)
            modelBuilder.Entity<CartItem>()
                .HasIndex(c => new { c.ProductId, c.Username })
                .IsUnique();

            // Username must be unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Important: Tell EF that CartItem uses Username as FK to User.Username (not UserId)
            modelBuilder.Entity<CartItem>()
                .HasOne<User>()                    // No navigation property on CartItem side
                .WithMany(u => u.CartItems)
                .HasPrincipalKey(u => u.Username)   // Link to Username instead of Id
                .HasForeignKey(c => c.Username);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
