using System;
using System.Threading.Tasks;
using CRM.LeadSources;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace CRM.Web.Pages.LeadSources;

public class IndexModel : AbpPageModel
{
    private readonly ILeadSourceAppService _leadSourceAppService;

    public IndexModel(
        ILeadSourceAppService leadSourceAppService)
    {
        _leadSourceAppService = leadSourceAppService;
    }

    public void OnGet()
    {
    }


    // =========================================================
    // LIST
    // =========================================================

    public async Task<JsonResult> OnGetListAsync(
        int draw = 1,
        int start = 0,
        int length = 10,
        string? sorting = null)
    {
        var result = await _leadSourceAppService.GetListAsync(
            new PagedAndSortedResultRequestDto
            {
                SkipCount = start,
                MaxResultCount = length,
                Sorting = string.IsNullOrWhiteSpace(sorting)
                    ? "Name"
                    : sorting
            });

        return new JsonResult(new
        {
            draw = draw,
            recordsTotal = result.TotalCount,
            recordsFiltered = result.TotalCount,
            data = result.Items
        });
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task<JsonResult> OnPostCreateAsync(
        CreateUpdateLeadSourceDto input)
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(new
            {
                success = false,
                message = "Invalid data."
            });
        }

        await _leadSourceAppService.CreateAsync(input);

        return new JsonResult(new
        {
            success = true
        });
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task<JsonResult> OnPostDeleteAsync(
        Guid id)
    {
        await _leadSourceAppService.DeleteAsync(id);

        return new JsonResult(new
        {
            success = true
        });
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<JsonResult> OnPostUpdateAsync(
        Guid id,
        CreateUpdateLeadSourceDto input)
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(new
            {
                success = false,
                message = "Invalid data."
            });
        }

        await _leadSourceAppService.UpdateAsync(id, input);

        return new JsonResult(new
        {
            success = true,
            message = "Lead Source updated successfully."
        });
    }


}