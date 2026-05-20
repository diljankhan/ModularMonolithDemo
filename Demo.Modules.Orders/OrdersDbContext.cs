using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Demo.Modules.Orders
{
    public class OrdersDbContext : DbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Tells EF Core that these tables live inside the 'Orders' schema
            modelBuilder.HasDefaultSchema("Orders");
            base.OnModelCreating(modelBuilder);
        }
    }
}
