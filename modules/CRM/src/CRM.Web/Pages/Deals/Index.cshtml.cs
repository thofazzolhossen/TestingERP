using System;
using System.Threading.Tasks;
using CRM.Leads;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace CRM.Web.Pages.Deals;

public class IndexModel : AbpPageModel
{
    private readonly ILeadAppService _leadAppService;

    public IndexModel(ILeadAppService leadAppService)
    {
        _leadAppService = leadAppService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetListAsync(int start = 0, int length = 1000, string? sorting = null)
    {
        var result = await _leadAppService.GetDealListAsync(new PagedAndSortedResultRequestDto
        {
            SkipCount = start,
            MaxResultCount = length,
            Sorting = sorting
        });

        return new JsonResult(new
        {
            recordsTotal = result.TotalCount,
            recordsFiltered = result.TotalCount,
            data = result.Items
        });
    }

    public async Task<IActionResult> OnPostBackToLeadAsync(Guid id)
    {
        await _leadAppService.RevertToLeadAsync(id);
        return new JsonResult(new
        {
            success = true,
            message = "Deal moved back to Lead successfully!"
        });
    }
}