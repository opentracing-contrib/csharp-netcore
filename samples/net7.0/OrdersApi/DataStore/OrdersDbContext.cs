﻿using Microsoft.EntityFrameworkCore;

namespace OrdersApi.DataStore
{
    public class OrdersDbContext : DbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options)
            : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }

        public void Seed()
        {
            if (Database.EnsureCreated())
            {
                Database.Migrate();

                SaveChanges();
            }
        }
    }
}
