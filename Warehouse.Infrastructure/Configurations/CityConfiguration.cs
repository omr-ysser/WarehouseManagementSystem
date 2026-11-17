using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Configurations;

public sealed class CityConfiguration
    : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities");

        builder.HasKey(city => city.Id);

        builder.Property(city => city.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(city => new
        {
            city.CountryId,
            city.Name
        })
        .IsUnique();

        builder.HasMany(city => city.Riders)
            .WithOne(rider => rider.City)
            .HasForeignKey(rider => rider.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
