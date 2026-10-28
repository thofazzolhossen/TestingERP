using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Leads;
using CRM.LeadStages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Identity;

namespace CRM.Web.Pages.FollowUps;

public class IndexModel : AbpPageModel
{
    private readonly ILeadAppService _leadAppService;
    private readonly ILeadStageAppService _leadStageAppService;
    private readonly IIdentityUserAppService _identityUserAppService;

    public List<SelectListItem> LeadStages { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();

    public IndexModel(
        ILeadAppService leadAppService,
        ILeadStageAppService leadStageAppService,
        IIdentityUserAppService identityUserAppService)
    {
        _leadAppService = leadAppService;
        _leadStageAppService = leadStageAppService;
        _identityUserAppService = identityUserAppService;
    }

    public async Task OnGetAsync()
    {
        var stageList = await _leadStageAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        LeadStages = stageList.Items.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();

        var userList = await _identityUserAppService.GetListAsync(new GetIdentityUsersInput { MaxResultCount = 1000 });
        Users = userList.Items.Select(x => new SelectListItem($"{x.UserName} ({x.Email})", x.Id.ToString())).ToList();
    }

    public async Task<JsonResult> OnGetListAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var items = await _leadAppService.GetFollowUpListAsync(new GetFollowUpListInput
        {
            FromDate = fromDate,
            ToDate = toDate,
            MaxResultCount = 1000
        });

        return new JsonResult(new
        {
            recordsTotal = items.Count,
            data = items
        });
    }

    public async Task<JsonResult> OnPostRescheduleAsync(RescheduleFollowUpDto input)
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(new { success = false, message = "Invalid input data." });
        }

        await _leadAppService.RescheduleFollowUpAsync(input);
        return new JsonResult(new { success = true, message = "Follow-up updated successfully!" });
    }
}