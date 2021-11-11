using Microsoft.EntityFrameworkCore;
using Shared;

namespace Samples.CustomersApi.DataStore
{
    public class CustomerDbContext : DbContext
    {
        public CustomerDbContext(DbContextOptions<CustomerDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }

        public void Seed()
        {
            if (Database.EnsureCreated())
            {
                Database.Migrate();

                Customers.Add(new Customer(1, "Marcel Belding"));
                Customers.Add(new Customer(2, "Phyllis Schriver"));
                Customers.Add(new Customer(3, "Estefana Balderrama"));
                Customers.Add(new Customer(4, "Kenyetta Lone"));
                Customers.Add(new Customer(5, "Vernita Fernald"));
                Customers.Add(new Customer(6, "Tessie Storrs"));

                SaveChanges();
            }
        }
    }
}
