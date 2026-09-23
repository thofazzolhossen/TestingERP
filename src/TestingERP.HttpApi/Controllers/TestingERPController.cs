using TestingERP.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace TestingERP.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class TestingERPController : AbpControllerBase
{
    protected TestingERPController()
    {
        LocalizationResource = typeof(TestingERPResource);
    }
}
