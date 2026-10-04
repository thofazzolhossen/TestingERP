using CRM.CustomerTypes;
using CRM.Leads;
using CRM.LeadSources;
using CRM.LeadStages;
using CRM.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Identity;

namespace CRM.Web.Pages.Leads;

public class IndexModel : AbpPageModel
{
    private readonly ILeadAppService _leadAppService;
    private readonly ICustomerTypeAppService _customerTypeAppService;
    private readonly ILeadSourceAppService _leadSourceAppService;
    private readonly ILeadStageAppService _leadStageAppService;
    private readonly IServiceAppService _serviceAppService;
    private readonly IIdentityUserAppService _identityUserAppService;

    public List<SelectListItem> CustomerTypes { get; set; } = new();
    public List<SelectListItem> LeadSources { get; set; } = new();
    public List<SelectListItem> LeadStages { get; set; } = new();
    public List<SelectListItem> Services { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();

    public IndexModel(
        ILeadAppService leadAppService,
        ICustomerTypeAppService customerTypeAppService,
        ILeadSourceAppService leadSourceAppService,
        ILeadStageAppService leadStageAppService,
        IServiceAppService serviceAppService,
        IIdentityUserAppService identityUserAppService)
    {
        _leadAppService = leadAppService;
        _customerTypeAppService = customerTypeAppService;
        _leadSourceAppService = leadSourceAppService;
        _leadStageAppService = leadStageAppService;
        _serviceAppService = serviceAppService;
        _identityUserAppService = identityUserAppService;
    }

    public async Task OnGetAsync()
    {
        var ctList = await _customerTypeAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        CustomerTypes = ctList.Items.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();

        var lsList = await _leadSourceAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        LeadSources = lsList.Items.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();

        var stageList = await _leadStageAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        LeadStages = stageList.Items.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();

        var serviceList = await _serviceAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        Services = serviceList.Items.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();

        // Get Users from ABP Identity Module
        var userList = await _identityUserAppService.GetListAsync(new GetIdentityUsersInput { MaxResultCount = 1000 });
        Users = userList.Items.Select(x => new SelectListItem($"{x.UserName} ({x.Email})", x.Id.ToString())).ToList();
    }

    public async Task<JsonResult> OnGetListAsync(int draw = 1, int start = 0, int length = 10, string? sorting = null)
    {
        var result = await _leadAppService.GetListAsync(new PagedAndSortedResultRequestDto
        {
            SkipCount = start,
            MaxResultCount = length,
            Sorting = sorting
        });

        return new JsonResult(new
        {
            draw = draw,
            recordsTotal = result.TotalCount,
            recordsFiltered = result.TotalCount,
            data = result.Items
        });
    }

    public async Task<JsonResult> OnPostCreateAsync(CreateUpdateLeadDto input)
    {
        if (!ModelState.IsValid) return new JsonResult(new { success = false, message = "Invalid Form Input" });
        await _leadAppService.CreateAsync(input);
        return new JsonResult(new { success = true, message = "Lead created successfully!" });
    }

    public async Task<JsonResult> OnPostUpdateAsync(Guid id, CreateUpdateLeadDto input)
    {
        if (!ModelState.IsValid) return new JsonResult(new { success = false, message = "Invalid Form Input" });
        await _leadAppService.UpdateAsync(id, input);
        return new JsonResult(new { success = true, message = "Lead updated successfully!" });
    }

    public async Task<JsonResult> OnPostDeleteAsync(Guid id)
    {
        await _leadAppService.DeleteAsync(id);
        return new JsonResult(new { success = true, message = "Lead deleted successfully!" });
    }

    public async Task<IActionResult> OnPostUploadCsvAsync(IFormFile csvFile)
    {
        if (csvFile == null || csvFile.Length == 0)
        {
            return new JsonResult(new { success = false, message = "Please select a valid CSV file." });
        }

        try
        {
            string csvContent;
            using (var reader = new StreamReader(csvFile.OpenReadStream()))
            {
                csvContent = await reader.ReadToEndAsync();
            }

            var importedCount = await _leadAppService.ImportCsvAsync(csvContent);

            return new JsonResult(new
            {
                success = true,
                message = $"Successfully imported {importedCount} leads!"
            });
        }
        catch (System.Exception ex)
        {
            var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return new JsonResult(new { success = false, message });
        }
    }
}