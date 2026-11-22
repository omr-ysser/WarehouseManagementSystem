using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Identity;

namespace Warehouse.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Warehouse.Domain.Entities.Warehouse> Warehouses
        => Set<Warehouse.Domain.Entities.Warehouse>();

    public DbSet<WarehouseStock> WarehouseStocks
        => Set<WarehouseStock>();

    public DbSet<StockTransaction> StockTransactions
        => Set<StockTransaction>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions
        => Set<RolePermission>();
    public DbSet<Rider> Riders => Set<Rider>();

    public DbSet<Tournament> Tournaments => Set<Tournament>();

    public DbSet<RiderTournament> RiderTournaments
        => Set<RiderTournament>();

    public DbSet<RiderTournamentHorse> RiderTournamentHorses
        => Set<RiderTournamentHorse>();
    public DbSet<CustomerPasswordEmail> CustomerPasswordEmails
    => Set<CustomerPasswordEmail>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<SupplierDocument> SupplierDocuments
        => Set<SupplierDocument>();

    public DbSet<City> Cities => Set<City>();
    public DbSet<SupplierRequest> SupplierRequests => Set<SupplierRequest>();

    public DbSet<SupplierRequestFinanceActivity> SupplierRequestFinanceActivities =>
        Set<SupplierRequestFinanceActivity>();
    public DbSet<SupplierStatusActivity> SupplierStatusActivities =>
    Set<SupplierStatusActivity>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
