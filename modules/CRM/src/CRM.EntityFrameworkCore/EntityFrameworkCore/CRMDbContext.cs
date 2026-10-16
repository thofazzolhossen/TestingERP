using CRM.CustomerTypes;
using CRM.Leads;
using CRM.LeadSources;
using CRM.LeadStages;
using CRM.Messenger;
using CRM.SalesPipelines;
using CRM.Services;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace CRM.EntityFrameworkCore;

[ConnectionStringName("Default")]
public class CRMDbContext : AbpDbContext<CRMDbContext>
{
    public DbSet<LeadSource> LeadSources { get; set; }
    public DbSet<CustomerType> CustomerTypes { get; set; }
    public DbSet<SalesPipeline> SalesPipelines { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<LeadStage> LeadStages { get; set; }
    public DbSet<Lead> Leads { get; set; }

    public DbSet<MessengerConversation> MessengerConversations { get; set; }
    public DbSet<MessengerMessage> MessengerMessages { get; set; }
    public DbSet<MessengerPageConnection> MessengerPageConnections { get; set; }

    public CRMDbContext(DbContextOptions<CRMDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigureCRM();
    }
}