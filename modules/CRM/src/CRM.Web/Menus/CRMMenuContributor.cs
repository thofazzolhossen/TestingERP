using System.Threading.Tasks;
using Volo.Abp.UI.Navigation;

namespace CRM.Web.Menus;

public class CRMMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var crmMenu = new ApplicationMenuItem(
            CRMMenus.GroupName,
            "CRM",
            icon: "fa fa-users"
        );

        crmMenu.AddItem(new ApplicationMenuItem(
            CRMMenus.LeadSources,
            "Lead Sources",
            url: "/LeadSources",
            icon: "fa fa-list"
        ));

        crmMenu.AddItem(new ApplicationMenuItem(
            CRMMenus.CustomerTypes,
            "Customer Types",
            url: "/CustomerTypes",
            icon: "fa fa-id-card"
        ));
        crmMenu.AddItem(new ApplicationMenuItem(
            CRMMenus.SalesPipelines,
            "Sales Pipelines",
            url: "/SalesPipelines",
            icon: "fa fa-filter"
        ));
        crmMenu.AddItem(new ApplicationMenuItem(
            CRMMenus.Services,
            "Services",
            url: "/Services",
            icon: "fa fa-concierge-bell"
        ));
        crmMenu.AddItem(new ApplicationMenuItem(
            CRMMenus.LeadStages,
            "Lead Stages",
            url: "/LeadStages",
            icon: "fa fa-layer-group"
        ));

        crmMenu.AddItem(new ApplicationMenuItem(
            CRMMenus.Leads,
            "Leads",
            url: "/Leads",
            icon: "fa fa-user-tie"
        ));

        crmMenu.AddItem(new ApplicationMenuItem(
            CRMMenus.Settings,
            "Settings",
            url: "/Messenger/Settings",
            icon: "fa fa-user-tie"
        ));

        context.Menu.AddItem(crmMenu);

        return Task.CompletedTask;
    }
}