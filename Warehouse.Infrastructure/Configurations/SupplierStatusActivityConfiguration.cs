using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Configurations;

public sealed class SupplierStatusActivityConfiguration
    : IEntityTypeConfiguration<SupplierStatusActivity>
{
    public void Configure(EntityTypeBuilder<SupplierStatusActivity> builder)
    {
        builder.Property(activity => activity.ActorAccountId)
            .HasMaxLength(450)
            .IsRequired();

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(activity => activity.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(activity => new { activity.SupplierId, activity.Id });
    }
}
