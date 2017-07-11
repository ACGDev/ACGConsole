﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using AutoCarConsole.Model;

namespace AutoCarConsole
{
    [DbConfigurationType(typeof(MySql.Data.Entity.MySqlEFConfiguration))]
    public class AutoCareDataContext : DbContext
    {
        public AutoCareDataContext()
            : base("mysqlconnection")
        {
        }

        public DbSet<customers> Customers { get; set; }
        public DbSet<customer_groups> CustomerGroups { get; set; }        
        public DbSet<orders> Orders { get; set; }
        public DbSet<order_items> OrderItems { get; set; }
        public DbSet<order_shipments> OrderShipments { get; set; }
        public DbSet<products> Products { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<customers>()
                .Property(c => c.customer_id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<customer_groups>()
                .Property(c => c.customer_group_id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<orders>()
                .Property(c => c.order_id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<order_items>()
                .Property(c => c.order_item_id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<order_shipments>()
                .Property(c => c.order_shipment_id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<customers>().ToTable("customers");
            modelBuilder.Entity<customer_groups>().ToTable("customer_groups");
            modelBuilder.Entity<orders>().ToTable("orders");
            modelBuilder.Entity<order_items>().ToTable("order_items");
            modelBuilder.Entity<order_shipments>().ToTable("order_shipments");
            modelBuilder.Entity<products>().ToTable("3dc_products");
        }
    }
}