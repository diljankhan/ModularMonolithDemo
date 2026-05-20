using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Demo.Modules.Customers
{
    public class CustomersDbContext : DbContext
    {
        public CustomersDbContext(DbContextOptions<CustomersDbContext> options) : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Tells EF Core that these tables live inside the 'Customers' schema
            modelBuilder.HasDefaultSchema("Customers");
            base.OnModelCreating(modelBuilder);
        }

    }
}
