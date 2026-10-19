using CRM.CustomerTypes;
using CRM.Leads;
using CRM.LeadSources;
using CRM.LeadStages;
using CRM.Messenger;
using CRM.SalesPipelines;
using CRM.Services;
using CRM.Sheets;
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

            b.Property(x => x.ExternalId).HasMaxLength(64);
            b.HasIndex(x => new { x.TenantId, x.ExternalId });

            // Foreign Key Relations
            b.HasOne<CustomerType>().WithMany().HasForeignKey(x => x.CustomerTypeId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<LeadSource>().WithMany().HasForeignKey(x => x.LeadSourceId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<LeadStage>().WithMany().HasForeignKey(x => x.LeadStageId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Service>().WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SheetConnection>(b =>
        {
            b.ToTable("CrmSheetConnections");
            b.ConfigureByConvention();
            b.Property(x => x.ConnectionKey).IsRequired().HasMaxLength(64);
            b.Property(x => x.Secret).IsRequired().HasMaxLength(512);
            b.HasIndex(x => x.ConnectionKey).IsUnique();
        });

        builder.ApplyConfiguration(new SalesPipelineConfiguration());
        builder.ApplyConfiguration(new LeadStageConfiguration());

        // Messenger

        builder.Entity<MessengerConversation>(b =>
        {
            b.ToTable("CrmMessengerConversations");
            b.ConfigureByConvention();
            b.Property(x => x.PageScopedUserId).IsRequired().HasMaxLength(128);
            b.Property(x => x.CustomerName).IsRequired().HasMaxLength(256);
            b.Property(x => x.ProfilePictureUrl).HasMaxLength(1024);
            b.Property(x => x.LastMessageText).HasMaxLength(1000);
            b.HasIndex(x => new { x.TenantId, x.PageScopedUserId }).IsUnique();
            b.Property(x => x.PageId).HasMaxLength(64);
        });

        builder.Entity<MessengerMessage>(b =>
        {
            b.ToTable("CrmMessengerMessages");
            b.ConfigureByConvention();
            b.Property(x => x.FacebookMessageId).HasMaxLength(256);
            b.Property(x => x.Text).IsRequired().HasMaxLength(2000);
            b.HasIndex(x => new { x.ConversationId, x.SentTime });
            b.HasIndex(x => x.FacebookMessageId);

            b.Property(x => x.AttachmentType).HasMaxLength(32);
            b.Property(x => x.AttachmentName).HasMaxLength(256);
            b.Property(x => x.AttachmentMimeType).HasMaxLength(128);
            b.Property(x => x.AttachmentStoredName).HasMaxLength(128);

            b.HasOne<MessengerConversation>().WithMany()
                .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<MessengerPageConnection>(b =>
        {
            b.ToTable("CrmMessengerPageConnections");
            b.ConfigureByConvention();
            b.Property(x => x.PageId).IsRequired().HasMaxLength(64);
            b.Property(x => x.PageName).IsRequired().HasMaxLength(256);
            b.Property(x => x.AccessToken).IsRequired().HasMaxLength(1024);
            b.HasIndex(x => x.PageId).IsUnique();   // one page belongs to exactly one tenant
        });

    }
}