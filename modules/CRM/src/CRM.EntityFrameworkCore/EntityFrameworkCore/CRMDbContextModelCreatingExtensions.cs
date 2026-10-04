using CRM.CustomerTypes;
using CRM.Leads;
using CRM.LeadSources;
using CRM.LeadStages;
using CRM.SalesPipelines;
using CRM.Services;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace CRM.EntityFrameworkCore;

public static class CRMDbContextModelCreatingExtensions
{
    public static void ConfigureCRM(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<LeadSource>(b =>
        {
            b.ToTable("CrmLeadSources");
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Description).HasMaxLength(512);
        });

        builder.Entity<CustomerType>(b =>
        {
            b.ToTable("CrmCustomerTypes");
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Description).HasMaxLength(512);

            b.HasIndex(x => new { x.TenantId, x.Name });
        });

        builder.Entity<Lead>(b =>
        {
            b.ToTable("CrmLeads");
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.CompanyName).HasMaxLength(128);
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.Phone).HasMaxLength(32);
            b.Property(x => x.EstimatedBudget).HasColumnType("decimal(18,2)");
            b.Property(x => x.Remarks).HasMaxLength(1000);

            // Foreign Key Relations
            b.HasOne<CustomerType>().WithMany().HasForeignKey(x => x.CustomerTypeId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<LeadSource>().WithMany().HasForeignKey(x => x.LeadSourceId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<LeadStage>().WithMany().HasForeignKey(x => x.LeadStageId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Service>().WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.ApplyConfiguration(new SalesPipelineConfiguration());
        builder.ApplyConfiguration(new LeadStageConfiguration());
    }
}