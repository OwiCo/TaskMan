using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.Api.Infrastructure.Persistence.Configurations;

public sealed class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.IssueType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(w => w.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(w => w.Number)
            .IsRequired();

        // Two work items under the same project must not share a number - backs the per-project
        // counter invariant even if the application logic that assigns numbers is later changed.
        builder.HasIndex(w => new { w.ProjectId, w.Number })
            .IsUnique();

        builder.Property(w => w.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(w => w.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing hierarchy: a work item with children cannot be deleted - see
        // DECISIONS.md #005. Restrict is Postgres' effective default, set explicitly here so the
        // intent is visible in the configuration rather than relied on implicitly.
        builder.HasOne<WorkItem>()
            .WithMany()
            .HasForeignKey(w => w.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // A work item must always have a reporter, so a User with reported work can't be deleted
        // out from under it. No user-deletion flow exists yet, but Restrict is the safe default.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(w => w.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        // AssigneeId is optional, so unlike ReporterId, deleting the assignee just unassigns the
        // ticket rather than blocking the delete.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(w => w.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
