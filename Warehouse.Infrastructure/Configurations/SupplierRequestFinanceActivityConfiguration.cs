using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Configurations;

public sealed class SupplierRequestFinanceActivityConfiguration
    : IEntityTypeConfiguration<SupplierRequestFinanceActivity>
{
    public void Configure(EntityTypeBuilder<SupplierRequestFinanceActivity> builder)
    {
        builder.Property(activity => activity.ChangedByAccountId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(activity => activity.PreviousCollectedAmount).HasPrecision(18, 2);
        builder.Property(activity => activity.PreviousPayments).HasPrecision(18, 2);
        builder.Property(activity => activity.CollectedAmount).HasPrecision(18, 2);
        builder.Property(activity => activity.Payments).HasPrecision(18, 2);

        builder.HasOne<SupplierRequest>()
            .WithMany()
            .HasForeignKey(activity => activity.SupplierRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(activity => new
        {
            activity.SupplierRequestId,
            activity.Id
        });
    }
}
