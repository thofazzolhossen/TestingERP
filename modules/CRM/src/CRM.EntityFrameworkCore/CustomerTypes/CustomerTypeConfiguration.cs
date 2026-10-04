using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace CRM.CustomerTypes;

public class CustomerTypeConfiguration : IEntityTypeConfiguration<CustomerType>
{
    public void Configure(EntityTypeBuilder<CustomerType> builder)
    {
        builder.ToTable("CustomerTypes");
        builder.ConfigureByConvention(); // Automatically configures IMultiTenant properties

        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Description).HasMaxLength(512);

        // Index for multi-tenant lookups
        builder.HasIndex(x => new { x.TenantId, x.Name });
    }
}