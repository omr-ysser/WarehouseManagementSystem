using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Infrastructure.Identity;

namespace Warehouse.Infrastructure.Persistence.Configurations;

public class ApplicationRoleConfiguration
    : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("AspNetRoles", table =>
        {
            table.HasCheckConstraint(
                "CK_AspNetRoles_Name_MinLength",
                "LEN(LTRIM(RTRIM([Name]))) >= 2");
        });

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.NormalizedName)
            .IsRequired()
            .HasMaxLength(50);
    }
}
