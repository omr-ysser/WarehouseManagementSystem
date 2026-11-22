using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Configurations;

public sealed class SupplierRequestConfiguration
    : IEntityTypeConfiguration<SupplierRequest>
{
    public void Configure(EntityTypeBuilder<SupplierRequest> builder)
    {
        builder.Property(request => request.OrderId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(request => request.LoadWeight)
            .HasPrecision(18, 3);

        builder.Property(request => request.CollectedAmount)
            .HasPrecision(18, 2)
            .IsConcurrencyToken();

        builder.Property(request => request.Payments)
            .HasPrecision(18, 2)
            .IsConcurrencyToken();

        builder.HasOne(request => request.Supplier)
            .WithMany()
            .HasForeignKey(request => request.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(request => new
        {
            request.SupplierId,
            request.FromDate,
            request.Id
        });
    }
}