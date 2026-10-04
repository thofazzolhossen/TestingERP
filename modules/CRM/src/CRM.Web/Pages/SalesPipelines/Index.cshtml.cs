using System;
using System.Threading.Tasks;
using CRM.SalesPipelines;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace CRM.Web.Pages.SalesPipelines;

public class IndexModel : AbpPageModel
{
    private readonly ISalesPipelineAppService _salesPipelineAppService;

    public IndexModel(ISalesPipelineAppService salesPipelineAppService)
    {
        _salesPipelineAppService = salesPipelineAppService;
    }

    public void OnGet()
    {
    }

    public async Task<JsonResult> OnGetListAsync(
        int draw = 1,
        int start = 0,
        int length = 10,
        string? sorting = null)
    {
        var result = await _salesPipelineAppService.GetListAsync(
            new PagedAndSortedResultRequestDto
            {
                SkipCount = start,
                MaxResultCount = length,
                Sorting = string.IsNullOrWhiteSpace(sorting) ? "Name" : sorting
            });

        return new JsonResult(new
        {
            draw = draw,
            recordsTotal = result.TotalCount,
            recordsFiltered = result.TotalCount,
            data = result.Items
        });
    }

    public async Task<JsonResult> OnPostCreateAsync(CreateUpdateSalesPipelineDto input)
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(new { success = false, message = "Invalid data." });
        }

        await _salesPipelineAppService.CreateAsync(input);
        return new JsonResult(new { success = true });
    }

    public async Task<JsonResult> OnPostUpdateAsync(Guid id, CreateUpdateSalesPipelineDto input)
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(new { success = false, message = "Invalid data." });
        }

        await _salesPipelineAppService.UpdateAsync(id, input);
        return new JsonResult(new { success = true, message = "Sales Pipeline updated successfully." });
    }

    public async Task<JsonResult> OnPostDeleteAsync(Guid id)
    {
        await _salesPipelineAppService.DeleteAsync(id);
        return new JsonResult(new { success = true });
    }
}