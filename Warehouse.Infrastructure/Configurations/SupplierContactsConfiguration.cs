using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Configurations;

public sealed class SupplierContactsConfiguration
    : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        #region Primary Contact

        builder.OwnsOne(
            supplier => supplier.PrimaryContact,
            contact =>
            {
                ConfigureContact(
                    contact,
                    "PrimaryContact");
            });

        builder.Navigation(supplier => supplier.PrimaryContact)
            .IsRequired();

        #endregion

        #region Additional Contact 1

        builder.OwnsOne(
            supplier => supplier.AdditionalContact1,
            contact =>
            {
                ConfigureContact(
                    contact,
                    "AdditionalContact1");
            });

        builder.Navigation(supplier => supplier.AdditionalContact1)
            .IsRequired(false);

        #endregion

        #region Additional Contact 2

        builder.OwnsOne(
            supplier => supplier.AdditionalContact2,
            contact =>
            {
                ConfigureContact(
                    contact,
                    "AdditionalContact2");
            });

        builder.Navigation(supplier => supplier.AdditionalContact2)
            .IsRequired(false);

        #endregion

        #region Additional Contact 3

        builder.OwnsOne(
            supplier => supplier.AdditionalContact3,
            contact =>
            {
                ConfigureContact(
                    contact,
                    "AdditionalContact3");
            });

        builder.Navigation(supplier => supplier.AdditionalContact3)
            .IsRequired(false);

        #endregion
    }

    private static void ConfigureContact(
        OwnedNavigationBuilder<Supplier, SupplierContact> contact,
        string prefix)
    {
        contact.Property(item => item.Person)
            .HasColumnName($"{prefix}_Person")
            .HasMaxLength(150)
            .IsRequired();

        contact.Property(item => item.PhoneNumber)
            .HasColumnName($"{prefix}_PhoneNumber")
            .HasMaxLength(30)
            .IsRequired();

        contact.Property(item => item.JobTitle)
            .HasColumnName($"{prefix}_JobTitle")
            .HasMaxLength(100)
            .IsRequired();
    }
}
