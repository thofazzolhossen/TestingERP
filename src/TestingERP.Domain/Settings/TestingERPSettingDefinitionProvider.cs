using Volo.Abp.Settings;

namespace TestingERP.Settings;

public class TestingERPSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(TestingERPSettings.MySetting1));
    }
}
