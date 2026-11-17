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

public sealed class CustomerPasswordEmailConfiguration
    : IEntityTypeConfiguration<CustomerPasswordEmail>
{
    public void Configure(EntityTypeBuilder<CustomerPasswordEmail> builder)
    {
        builder.ToTable("CustomerPasswordEmails");

        builder.HasKey(email => email.Id);

        builder.Property(email => email.AccountId)
            .HasMaxLength(450)
            .IsRequired();

        builder.HasOne<Rider>()
            .WithMany()
            .HasForeignKey(email => email.RiderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(email => email.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(email => new
        {
            email.ProcessedAtUtc,
            email.NextAttemptAtUtc
        });
    }
}
