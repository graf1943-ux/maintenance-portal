using Microsoft.EntityFrameworkCore;
using RoadbikeCare.Api.Models;

namespace RoadbikeCare.Api.Data;

public sealed class RoadbikeCareDbContext(DbContextOptions<RoadbikeCareDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Bike> Bikes => Set<Bike>();
    public DbSet<BikeComponent> BikeComponents => Set<BikeComponent>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<StaffMember> Staff => Set<StaffMember>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<EventCheck> Events => Set<EventCheck>();
    public DbSet<WorkHistory> History => Set<WorkHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().HasKey(x => x.Id);
        modelBuilder.Entity<Bike>().HasKey(x => x.Id);
        modelBuilder.Entity<BikeComponent>().HasKey(x => x.Id);
        modelBuilder.Entity<Shop>().HasKey(x => x.Id);
        modelBuilder.Entity<StaffMember>().HasKey(x => x.Id);
        modelBuilder.Entity<Plan>().HasKey(x => x.Id);
        modelBuilder.Entity<Contract>().HasKey(x => x.Id);
        modelBuilder.Entity<WorkItem>().HasKey(x => x.Id);
        modelBuilder.Entity<EventCheck>().HasKey(x => x.Id);
        modelBuilder.Entity<WorkHistory>().HasKey(x => x.Id);

        modelBuilder.Entity<Bike>().Property(x => x.Serial).HasMaxLength(100);
        modelBuilder.Entity<Bike>().HasIndex(x => x.Serial).IsUnique().HasFilter("[Serial] <> N''");
        modelBuilder.Entity<BikeComponent>().Property(x => x.Category).HasMaxLength(100);
        modelBuilder.Entity<BikeComponent>().Property(x => x.Manufacturer).HasMaxLength(100);
        modelBuilder.Entity<BikeComponent>().Property(x => x.Model).HasMaxLength(100);
        modelBuilder.Entity<Plan>().Property(x => x.Name).HasMaxLength(100);
        modelBuilder.Entity<Plan>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Customer>().Property(x => x.EntraTenantId).HasMaxLength(64);
        modelBuilder.Entity<Customer>().Property(x => x.EntraObjectId).HasMaxLength(128);
        modelBuilder.Entity<Contract>()
            .HasIndex(x => x.BikeId)
            .IsUnique()
            .HasFilter("[IsActive] = 1");
        modelBuilder.Entity<Customer>()
            .HasIndex(x => new { x.EntraTenantId, x.EntraObjectId })
            .IsUnique()
            .HasFilter("[EntraTenantId] IS NOT NULL AND [EntraObjectId] IS NOT NULL");

        modelBuilder.Entity<Bike>().HasOne(x => x.CustomerNavigation).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Bike>().HasOne(x => x.ActiveContractNavigation).WithMany().HasForeignKey(x => x.ActiveContractId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BikeComponent>().HasOne(x => x.BikeNavigation).WithMany().HasForeignKey(x => x.BikeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StaffMember>().HasOne(x => x.ShopNavigation).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Contract>().HasOne(x => x.BikeNavigation).WithMany().HasForeignKey(x => x.BikeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Contract>().HasOne(x => x.PlanNavigation).WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Contract>().HasOne(x => x.ShopNavigation).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Contract>().HasOne(x => x.ReceptionistNavigation).WithMany().HasForeignKey(x => x.ReceptionistId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkItem>().HasOne(x => x.BikeNavigation).WithMany().HasForeignKey(x => x.BikeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkItem>().HasOne(x => x.ShopNavigation).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkItem>().HasOne(x => x.ReceptionistNavigation).WithMany().HasForeignKey(x => x.ReceptionistId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EventCheck>().HasOne(x => x.BikeNavigation).WithMany().HasForeignKey(x => x.BikeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EventCheck>().HasOne<Shop>().WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EventCheck>().HasOne<StaffMember>().WithMany().HasForeignKey(x => x.ReceptionistId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkHistory>().HasOne(x => x.BikeNavigation).WithMany().HasForeignKey(x => x.BikeId).OnDelete(DeleteBehavior.Restrict);
    }
}
