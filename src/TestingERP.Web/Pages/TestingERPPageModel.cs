using TestingERP.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace TestingERP.Web.Pages;

public abstract class TestingERPPageModel : AbpPageModel
{
    protected TestingERPPageModel()
    {
        LocalizationResourceType = typeof(TestingERPResource);
    }
}
