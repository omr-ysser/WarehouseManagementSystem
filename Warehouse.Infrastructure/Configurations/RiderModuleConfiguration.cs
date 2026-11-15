using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Configurations;

public sealed class RiderConfiguration : IEntityTypeConfiguration<Rider>
{
    public void Configure(EntityTypeBuilder<Rider> builder)
    {
        builder.ToTable("Riders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Nationality).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();

        builder.Property(x => x.NormalizedEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(x => x.NormalizedEmail).IsUnique();

        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.PoloTeam).HasMaxLength(150);
        builder.Property(x => x.ImageUrl).HasMaxLength(2048);

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public sealed class TournamentConfiguration
    : IEntityTypeConfiguration<Tournament>
{
    public void Configure(EntityTypeBuilder<Tournament> builder)
    {
        builder.ToTable("Tournaments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(100).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100);

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public sealed class RiderTournamentConfiguration
    : IEntityTypeConfiguration<RiderTournament>
{
    public void Configure(EntityTypeBuilder<RiderTournament> builder)
    {
        builder.ToTable("RiderTournaments");

        // Prevent assigning the same rider to the tournament twice.
        builder.HasKey(x => new { x.RiderId, x.TournamentId });

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.Rider)
            .WithMany(x => x.Tournaments)
            .HasForeignKey(x => x.RiderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Tournament)
            .WithMany(x => x.Riders)
            .HasForeignKey(x => x.TournamentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RiderTournamentHorseConfiguration
    : IEntityTypeConfiguration<RiderTournamentHorse>
{
    public void Configure(EntityTypeBuilder<RiderTournamentHorse> builder)
    {
        builder.ToTable("RiderTournamentHorses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();

        builder.HasOne(x => x.Participation)
            .WithMany(x => x.Horses)
            .HasForeignKey(x => new { x.RiderId, x.TournamentId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
