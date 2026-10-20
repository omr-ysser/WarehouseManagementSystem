using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint(
                "CK_Products_Name_MinLength",
                "LEN(LTRIM(RTRIM([Name]))) >= 2");

            table.HasCheckConstraint(
                "CK_Products_SKU_MinLength",
                "LEN(LTRIM(RTRIM([SKU]))) >= 2");

            table.HasCheckConstraint(
                "CK_Products_NormalizedSku_MinLength",
                "LEN(LTRIM(RTRIM([NormalizedSku]))) >= 2");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.SKU)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.NormalizedSku)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.NormalizedSku)
            .IsUnique();
    }
}
