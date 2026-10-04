using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace CRM.LeadStages;

public class LeadStageConfiguration : IEntityTypeConfiguration<LeadStage>
{
    public void Configure(EntityTypeBuilder<LeadStage> builder)
    {
        builder.ToTable("LeadStages");
        builder.ConfigureByConvention();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.Description)
            .HasMaxLength(512);

        builder.HasIndex(x => new { x.TenantId, x.Name });
    }
}