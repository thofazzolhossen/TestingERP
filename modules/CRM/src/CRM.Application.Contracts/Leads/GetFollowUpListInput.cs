using System;
using Volo.Abp.Application.Dtos;

namespace CRM.Leads;

public class GetFollowUpListInput : PagedAndSortedResultRequestDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? LeadStageId { get; set; }
    public Guid? AssignedUserId { get; set; }
}