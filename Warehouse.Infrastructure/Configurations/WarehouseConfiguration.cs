using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseEntity = Warehouse.Domain.Entities.Warehouse;

namespace Warehouse.Infrastructure.Persistence.Configurations;

public class WarehouseConfiguration
    : IEntityTypeConfiguration<WarehouseEntity>
{
    public void Configure(EntityTypeBuilder<WarehouseEntity> builder)
    {
        builder.ToTable("Warehouses", table =>
        {
            table.HasCheckConstraint(
                "CK_Warehouses_Name_MinLength",
                "LEN(LTRIM(RTRIM([Name]))) >= 2");

            table.HasCheckConstraint(
                "CK_Warehouses_NormalizedName_MinLength",
                "LEN(LTRIM(RTRIM([NormalizedName]))) >= 2");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.NormalizedName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.NormalizedName)
            .IsUnique();
    }
}
