using TestingERP.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace TestingERP.Permissions;

public class TestingERPPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(TestingERPPermissions.GroupName);

        //Define your own permissions here. Example:
        //myGroup.AddPermission(TestingERPPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<TestingERPResource>(name);
    }
}
