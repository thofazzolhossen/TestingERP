using System;
using System.Threading.Tasks;
using CRM.CustomerTypes;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace CRM.Web.Pages.CustomerTypes;

public class IndexModel : AbpPageModel
{
    private readonly ICustomerTypeAppService _customerTypeAppService;

    public IndexModel(ICustomerTypeAppService customerTypeAppService)
    {
        _customerTypeAppService = customerTypeAppService;
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
        var result = await _customerTypeAppService.GetListAsync(
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

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<JsonResult> OnPostCreateAsync(CreateUpdateCustomerTypeDto input)
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(new
            {
                success = false,
                message = "Invalid data."
            });
        }

        await _customerTypeAppService.CreateAsync(input);

        return new JsonResult(new
        {
            success = true
        });
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<JsonResult> OnPostUpdateAsync(Guid id, CreateUpdateCustomerTypeDto input)
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(new
            {
                success = false,
                message = "Invalid data."
            });
        }

        await _customerTypeAppService.UpdateAsync(id, input);

        return new JsonResult(new
        {
            success = true,
            message = "Customer Type updated successfully."
        });
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<JsonResult> OnPostDeleteAsync(Guid id)
    {
        await _customerTypeAppService.DeleteAsync(id);

        return new JsonResult(new
        {
            success = true
        });
    }
}