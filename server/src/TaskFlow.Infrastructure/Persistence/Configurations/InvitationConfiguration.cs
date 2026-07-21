using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Email).HasMaxLength(320).IsRequired();
        builder.Property(i => i.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.Token).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(InvitationStatus.Pending);
        builder.Property(i => i.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(i => i.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(i => i.OrganizationId);
        builder.HasIndex(i => i.Token).IsUnique();

        builder.HasOne(i => i.Organization)
            .WithMany()
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.InvitedBy)
            .WithMany()
            .HasForeignKey(i => i.InvitedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
