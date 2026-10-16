using CRM.Web.Menus;
using System.Threading.Tasks;
using TestingERP.Localization;
using TestingERP.MultiTenancy;
using TestingERP.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Identity.Web.Navigation;
using Volo.Abp.SettingManagement.Web.Navigation;
using Volo.Abp.TenantManagement.Web.Navigation;
using Volo.Abp.UI.Navigation;

namespace TestingERP.Web.Menus;

public class TestingERPMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private static Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<TestingERPResource>();

        // Home
        context.Menu.AddItem(
            new ApplicationMenuItem(
                TestingERPMenus.Home,
                l["Menu:Home"],
                "~/",
                icon: "fa fa-home",
                order: 1
            )
        );

        // =========================
        // CRM
        // =========================

        var crmMenu = new ApplicationMenuItem(
            "CRM",
            "CRM",
            icon: "fa fa-users",
            order: 2
        );

        crmMenu.AddItem(
            new ApplicationMenuItem(
                "CRM.LeadSources",
                "Lead Sources",
                url: "/LeadSources",
                icon: "fa fa-list"
            )
        );

        crmMenu.AddItem(
            new ApplicationMenuItem(
                "CRM.CustomerTypes",
                "Customer Types",
                url: "/CustomerTypes",
                icon: "fa fa-id-card"
            )
        );
        crmMenu.AddItem(
            new ApplicationMenuItem(
                "CRM.SalesPipelines",
                "Sales Pipelines",
                url: "/SalesPipelines",
                icon: "fa fa-filter"
            )
        );
        crmMenu.AddItem(
            new ApplicationMenuItem(
                "CRM.Services",
                "Services",
                url: "/Services",
                icon: "fa fa-concierge-bell"
            )
        );
        crmMenu.AddItem(
            new ApplicationMenuItem(
                "CRM.LeadStages",
                "Lead Stages",
                url: "/LeadStages",
                icon: "fa fa-layer-group",
                order: 4
            )
        );

        crmMenu.AddItem(new ApplicationMenuItem(
            "CRM.Leads",
            "Leads",
            url: "/Leads",
            icon: "fa fa-user-tie"
        ));

        crmMenu.AddItem(new ApplicationMenuItem(
            "CRM.Settings",
            "Settings",
            url: "/Messenger/Settings",
            icon: "fa fa-plug"
        ));


        context.Menu.AddItem(crmMenu);


        // =========================
        // Administration
        // =========================

        var administration = context.Menu.GetAdministration();

        administration.Order = 6;

        // Administration -> Identity
        administration.SetSubItemOrder(
            IdentityMenuNames.GroupName,
            1
        );

        if (MultiTenancyConsts.IsEnabled)
        {
            administration.SetSubItemOrder(
                TenantManagementMenuNames.GroupName,
                1
            );
        }
        else
        {
            administration.TryRemoveMenuItem(
                TenantManagementMenuNames.GroupName
            );
        }

        administration.SetSubItemOrder(
            SettingManagementMenuNames.GroupName,
            3
        );

        return Task.CompletedTask;
    }
}