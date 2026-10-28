using System;
using System.Threading.Tasks;
using CRM.Leads;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace CRM.Web.Pages.Leads;

public class DetailsModel : AbpPageModel
{
    private readonly ILeadAppService _leadAppService;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public LeadDetailDto Lead { get; set; } = default!;

    public DetailsModel(ILeadAppService leadAppService)
    {
        _leadAppService = leadAppService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id == Guid.Empty)
        {
            return RedirectToPage("/Leads/Index");
        }

        Lead = await _leadAppService.GetDetailAsync(Id);
        return Page();
    }

    // Handles GET: /Leads/Details?handler=StageHistory&leadId={guid}
    public async Task<JsonResult> OnGetStageHistoryAsync(Guid? leadId)
    {
        var targetLeadId = (leadId.HasValue && leadId.Value != Guid.Empty)
            ? leadId.Value
            : Id;

        if (targetLeadId == Guid.Empty)
        {
            return new JsonResult(new { success = false, data = Array.Empty<LeadStageHistoryDto>() });
        }

        var items = await _leadAppService.GetStageHistoryAsync(targetLeadId);
        return new JsonResult(new { success = true, data = items });
    }
}