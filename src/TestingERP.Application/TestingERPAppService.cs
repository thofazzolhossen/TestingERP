using TestingERP.Localization;
using Volo.Abp.Application.Services;

namespace TestingERP;

/* Inherit your application services from this class.
 */
public abstract class TestingERPAppService : ApplicationService
{
    protected TestingERPAppService()
    {
        LocalizationResource = typeof(TestingERPResource);
    }
}
