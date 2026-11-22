using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Configurations;

public sealed class SupplierDocumentConfiguration
    : IEntityTypeConfiguration<SupplierDocument>
{
    public void Configure(
        EntityTypeBuilder<SupplierDocument> builder)
    {
        #region Table And Primary Key

        builder.ToTable("SupplierDocuments");

        builder.HasKey(document => document.Id);

        #endregion

        #region Properties

        builder.Property(document => document.SupplierId)
            .IsRequired();

        builder.Property(document => document.Type)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(document => document.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(document => document.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(document => document.SizeBytes)
            .IsRequired();

        builder.Property(document => document.Content)
            .HasColumnType("varbinary(max)")
            .IsRequired();
        builder.Property(document => document.SupplierId)
    .IsRequired(false);

        builder.Property(document => document.UploadedByAccountId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(document => document.UploadedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        // Multiple unassigned documents are allowed.
        // Once attached, each supplier has one document of each type.
        builder.HasIndex(document => new
        {
            document.SupplierId,
            document.Type
        })
        .IsUnique()
        .HasFilter("[SupplierId] IS NOT NULL");

        #endregion

        #region Indexes

        // Prevent duplicate document types for the same supplier.
        builder.HasIndex(document => new
        {
            document.SupplierId,
            document.Type
        })
        .IsUnique();

        #endregion
    }
}
