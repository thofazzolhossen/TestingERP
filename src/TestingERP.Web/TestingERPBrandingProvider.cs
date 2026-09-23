using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.Localization;
using TestingERP.Localization;

namespace TestingERP.Web;

[Dependency(ReplaceServices = true)]
public class TestingERPBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<TestingERPResource> _localizer;

    public TestingERPBrandingProvider(IStringLocalizer<TestingERPResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
