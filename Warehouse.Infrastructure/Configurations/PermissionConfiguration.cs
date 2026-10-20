using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Infrastructure.Identity;

namespace Warehouse.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration
    : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", table =>
        {
            table.HasCheckConstraint(
                "CK_Permissions_Key_MinLength",
                "LEN(LTRIM(RTRIM([Key]))) >= 3");

            table.HasCheckConstraint(
                "CK_Permissions_NormalizedKey_MinLength",
                "LEN(LTRIM(RTRIM([NormalizedKey]))) >= 3");

            table.HasCheckConstraint(
                "CK_Permissions_DisplayName_MinLength",
                "LEN(LTRIM(RTRIM([DisplayName]))) >= 2");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.NormalizedKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.NormalizedKey)
            .IsUnique();
    }
}
