using System.Threading.Tasks;
using CRM.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace CRM.Web.Pages.Dashboard;

[Authorize]
public class IndexModel : AbpPageModel
{
    private readonly ICrmDashboardAppService _dashboard;
    public IndexModel(ICrmDashboardAppService dashboard) => _dashboard = dashboard;

    public void OnGet() { }

    public async Task<JsonResult> OnGetDataAsync() => new JsonResult(await _dashboard.GetAsync());
}