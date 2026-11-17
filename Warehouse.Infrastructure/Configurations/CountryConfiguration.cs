using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Configurations;

public sealed class CountryConfiguration
    : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries");

        builder.HasKey(country => country.Id);

        builder.Property(country => country.Code)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(country => country.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(country => country.Code)
            .IsUnique();

        builder.HasMany(country => country.Cities)
            .WithOne(city => city.Country)
            .HasForeignKey(city => city.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
