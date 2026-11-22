using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Configurations;

public sealed class SupplierTrucksConfiguration
    : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.OwnsMany(
            supplier => supplier.Trucks,
            trucks =>
            {
                #region Table

                trucks.ToTable("SupplierTrucks");

                #endregion

                #region Ownership

                trucks.WithOwner()
                    .HasForeignKey("SupplierId");

                trucks.Property<int>("SupplierId");

                #endregion

                #region Primary Key

                trucks.Property<int>("Id")
                    .ValueGeneratedOnAdd();

                trucks.HasKey("SupplierId", "Id");

                #endregion

                #region Truck Value

                trucks.Property(truck => truck.Value)
                    .HasMaxLength(20)
                    .IsRequired();

                #endregion

                #region Indexes

                trucks.HasIndex(
                        "SupplierId",
                        nameof(SupplierTruck.Value))
                    .IsUnique();

                #endregion
            });
    }
}