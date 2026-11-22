using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Configurations;

public sealed class SupplierConfiguration
    : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        #region Table And Primary Key

        builder.ToTable("Suppliers");

        builder.HasKey(supplier => supplier.Id);

        #endregion

        #region Basic Properties

        builder.Property(supplier => supplier.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(supplier => supplier.Email)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(supplier => supplier.IsActive)
            .IsRequired();

        builder.Property(supplier => supplier.CreatedByAccountId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(supplier => supplier.CreatedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();
        #region Note

        builder.Property(supplier => supplier.Note)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(supplier => supplier.NoteCreatedByAccountId)
            .HasMaxLength(450)
            .IsRequired(false);

        builder.Property(supplier => supplier.NoteCreatedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired(false);

        #endregion

        #endregion

        #region Indexes

        builder.HasIndex(supplier => supplier.Name);

        builder.HasIndex(supplier => new
        {
            supplier.IsActive,
            supplier.CreatedAtUtc
        });

        #endregion

        #region Documents Relationship

        builder.HasMany(supplier => supplier.Documents)
            .WithOne(document => document.Supplier)
            .HasForeignKey(document => document.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        #endregion
    }
}
