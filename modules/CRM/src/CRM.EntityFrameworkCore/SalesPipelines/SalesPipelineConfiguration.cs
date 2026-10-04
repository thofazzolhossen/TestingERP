using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace CRM.SalesPipelines;

public class SalesPipelineConfiguration : IEntityTypeConfiguration<SalesPipeline>
{
    public void Configure(EntityTypeBuilder<SalesPipeline> builder)
    {
        builder.ToTable("CRMSalesPipelines");
        builder.ConfigureByConvention();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.Description)
            .HasMaxLength(512);

        builder.HasIndex(x => new { x.TenantId, x.Name });
    }
}