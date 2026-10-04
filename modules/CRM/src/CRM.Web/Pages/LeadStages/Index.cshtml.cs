using System;
using System.Threading.Tasks;
using CRM.LeadStages;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace CRM.Web.Pages.LeadStages;

public class IndexModel : AbpPageModel
{
    private readonly ILeadStageAppService _leadStageAppService;

    public IndexModel(ILeadStageAppService leadStageAppService)
    {
        _leadStageAppService = leadStageAppService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetListAsync(int start, int length, string sorting)
    {
        var input = new PagedAndSortedResultRequestDto
        {
            SkipCount = start,
            MaxResultCount = length,
            Sorting = sorting
        };

        var result = await _leadStageAppService.GetListAsync(input);

        return new JsonResult(new
        {
            recordsTotal = result.TotalCount,
            recordsFiltered = result.TotalCount,
            data = result.Items
        });
    }

    public async Task<IActionResult> OnPostCreateAsync(CreateUpdateLeadStageDto input)
    {
        await _leadStageAppService.CreateAsync(input);
        return new JsonResult(new { success = true, message = "Lead Stage created successfully" });
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid id, CreateUpdateLeadStageDto input)
    {
        await _leadStageAppService.UpdateAsync(id, input);
        return new JsonResult(new { success = true, message = "Lead Stage updated successfully" });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _leadStageAppService.DeleteAsync(id);
        return new JsonResult(new { success = true, message = "Deleted successfully" });
    }
}