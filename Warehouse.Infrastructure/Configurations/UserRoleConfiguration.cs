using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warehouse.Infrastructure.Identity;

namespace Warehouse.Infrastructure.Persistence.Configurations;

public class UserRoleConfiguration
    : IEntityTypeConfiguration<IdentityUserRole<string>>
{
    public void Configure(
        EntityTypeBuilder<IdentityUserRole<string>> builder)
    {
        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasOne<ApplicationRole>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
