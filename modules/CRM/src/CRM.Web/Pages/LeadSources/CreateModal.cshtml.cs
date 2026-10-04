using System.Threading.Tasks;
using CRM.LeadSources;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace CRM.Web.Pages.LeadSources;

public class CreateModalModel : AbpPageModel
{
    [BindProperty]
    public CreateUpdateLeadSourceDto LeadSource { get; set; } = new();

    private readonly ILeadSourceAppService _leadSourceAppService;

    public CreateModalModel(ILeadSourceAppService leadSourceAppService)
    {
        _leadSourceAppService = leadSourceAppService;
    }

    public void OnGet()
    {
        // Empty - just show the form
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _leadSourceAppService.CreateAsync(LeadSource);
        return NoContent();   // Important for ModalManager
    }
}