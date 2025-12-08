using Microsoft.EntityFrameworkCore;
using StationaryManagement1.Models; // <-- FIXED: Use correct namespace for models
using StationaryManagement1.Models.ViewModels;

namespace StationaryManagement1.Data
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options)
            : base(options)
        {
        }

        // Tables
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RoleThreshold> RoleThresholds { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<StationeryItem> StationeryItems { get; set; }
        public DbSet<StationeryRequest> StationeryRequests { get; set; }
        public DbSet<RequestItem> RequestItems { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // View
        public DbSet<FrequentItemView> FrequentItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SQL view
            modelBuilder.Entity<FrequentItemView>()
                .HasNoKey()
                .ToView("vw_FrequentItems");

            // Employee → Subordinates
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Superior)
                .WithMany(s => s.Subordinates)
                .HasForeignKey(e => e.SuperiorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee → Requests
            modelBuilder.Entity<Employee>()
                .HasMany(e => e.StationeryRequests)
                .WithOne(r => r.Employee)
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee → Notifications
            modelBuilder.Entity<Employee>()
                .HasMany(e => e.Notifications)
                .WithOne(n => n.Employee)
                .HasForeignKey(n => n.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Precision fields
            modelBuilder.Entity<RequestItem>().Property(x => x.UnitCost).HasPrecision(18, 2);
            modelBuilder.Entity<StationeryItem>().Property(x => x.UnitCost).HasPrecision(18, 2);
            modelBuilder.Entity<StationeryRequest>().Property(x => x.TotalCost).HasPrecision(18, 2);
            modelBuilder.Entity<RoleThreshold>().Property(x => x.MaxAmount).HasPrecision(18, 2);
            modelBuilder.Entity<FrequentItemView>().Property(x => x.TotalSpent).HasPrecision(18, 2);

            // Role identity
            modelBuilder.Entity<Role>().Property(r => r.RoleId).ValueGeneratedOnAdd();
            modelBuilder.Entity<Role>()
                .HasOne(r => r.ReportsTo)
                .WithMany(r => r.DirectReports)
                .HasForeignKey(r => r.ReportsToRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Role → RoleThreshold 1:1
            modelBuilder.Entity<RoleThreshold>()
                .HasOne(rt => rt.Role)
                .WithOne(r => r.RoleThreshold)
                .HasForeignKey<RoleThreshold>(rt => rt.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Role>()
                .Property(r => r.CanApprove)
                .HasDefaultValue(false);

            // Seeds
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin", Description = "System Administrator", CanApprove = true },
                new Role { RoleId = 2, RoleName = "Manager", Description = "Department Manager", CanApprove = true },
                new Role { RoleId = 3, RoleName = "Employee", Description = "Regular Employee", CanApprove = false }
            );

            modelBuilder.Entity<RoleThreshold>().HasData(
                new RoleThreshold { RoleId = 1, MaxAmount = 999999 },
                new RoleThreshold { RoleId = 2, MaxAmount = 5000 },
                new RoleThreshold { RoleId = 3, MaxAmount = 500 }
            );
        }
    }
}
