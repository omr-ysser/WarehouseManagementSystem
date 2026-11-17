using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Identity;

namespace Warehouse.Infrastructure.Configurations;

public sealed class RiderAccountConfiguration
    : IEntityTypeConfiguration<Rider>
{
    public void Configure(EntityTypeBuilder<Rider> builder)
    {
        builder.Property(rider => rider.AccountId)
            .HasMaxLength(450);

        // One account can belong to only one rider.
        // Multiple legacy riders may still have AccountId = null.
        builder.HasIndex(rider => rider.AccountId)
            .IsUnique()
            .HasFilter("[AccountId] IS NOT NULL");

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Rider>(rider => rider.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
