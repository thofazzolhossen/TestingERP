using System;
using System.Threading.Tasks;
using CRM.Services;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace CRM.Web.Pages.Services;

public class IndexModel : AbpPageModel
{
    private readonly IServiceAppService _serviceAppService;

    public IndexModel(IServiceAppService serviceAppService)
    {
        _serviceAppService = serviceAppService;
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
        var result = await _serviceAppService.GetListAsync(
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

    public async Task<JsonResult> OnPostCreateAsync(CreateUpdateServiceDto input)
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(new { success = false, message = "Invalid data." });
        }

        await _serviceAppService.CreateAsync(input);
        return new JsonResult(new { success = true, message = "Service created successfully." });
    }

    public async Task<JsonResult> OnPostUpdateAsync(Guid id, CreateUpdateServiceDto input)
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(new { success = false, message = "Invalid data." });
        }

        await _serviceAppService.UpdateAsync(id, input);
        return new JsonResult(new { success = true, message = "Service updated successfully." });
    }

    public async Task<JsonResult> OnPostDeleteAsync(Guid id)
    {
        await _serviceAppService.DeleteAsync(id);
        return new JsonResult(new { success = true, message = "Service deleted successfully." });
    }
}