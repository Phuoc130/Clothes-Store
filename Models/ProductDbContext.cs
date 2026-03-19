using Microsoft.EntityFrameworkCore;

namespace ProductStore.Models
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options)
            : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Subcontractor> Subcontractors { get; set; }
        public DbSet<Event> Events { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define relationships based on the ERD.
            // Note: Some relationships in the diagram are one-to-one, which can be complex.
            // Here, we'll set them up as one-to-many for simplicity, which is more common.
            // A customer can have multiple orders, invoices, etc.

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.Order)
                .WithMany()
                .HasForeignKey(c => c.Order_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.Invoice)
                .WithMany()
                .HasForeignKey(c => c.Invoice_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.Event)
                .WithMany()
                .HasForeignKey(c => c.Event_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Product)
                .WithMany()
                .HasForeignKey(o => o.Product_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Material)
                .WithMany()
                .HasForeignKey(p => p.Material_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Subcontractor)
                .WithMany()
                .HasForeignKey(p => p.Subcontractor_ID)
                .OnDelete(DeleteBehavior.Restrict);

             modelBuilder.Entity<Material>()
                .HasOne(m => m.Subcontractor)
                .WithMany()
                .HasForeignKey(m => m.Subcontractor_ID)
                .OnDelete(DeleteBehavior.NoAction); // Avoid multiple cascade paths
        }
    }
}
