using CRM.CustomerTypes;
using CRM.LeadSources;
using CRM.LeadStages;
using CRM.Services;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace CRM.Leads;

public class LeadAppService : CrudAppService<
    Lead,
    LeadDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateLeadDto>, ILeadAppService
{
    private readonly IRepository<CustomerType, Guid> _customerTypeRepository;
    private readonly IRepository<LeadSource, Guid> _leadSourceRepository;
    private readonly IRepository<LeadStage, Guid> _leadStageRepository;
    private readonly IRepository<Service, Guid> _serviceRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IRepository<LeadStageHistory, Guid> _stageHistoryRepository;

    public LeadAppService(
        IRepository<Lead, Guid> repository,
        IRepository<CustomerType, Guid> customerTypeRepository,
        IRepository<LeadSource, Guid> leadSourceRepository,
        IRepository<LeadStage, Guid> leadStageRepository,
        IRepository<Service, Guid> serviceRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IRepository<LeadStageHistory, Guid> stageHistoryRepository) : base(repository) // Change here
    {
        _customerTypeRepository = customerTypeRepository;
        _leadSourceRepository = leadSourceRepository;
        _leadStageRepository = leadStageRepository;
        _serviceRepository = serviceRepository;
        _userRepository = userRepository;
        _stageHistoryRepository = stageHistoryRepository;
    }

    public override async Task<PagedResultDto<LeadDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        // 1. Fetch CRM Queryables (from CRMDbContext)
        var query = await Repository.GetQueryableAsync();
        var ctQuery = await _customerTypeRepository.GetQueryableAsync();
        var lsQuery = await _leadSourceRepository.GetQueryableAsync();
        var stQuery = await _leadStageRepository.GetQueryableAsync();
        var sQuery = await _serviceRepository.GetQueryableAsync();

        // 2. Join CRM entities in SQL
        var baseQuery = from lead in query
                        join ct in ctQuery on lead.CustomerTypeId equals ct.Id
                        join ls in lsQuery on lead.LeadSourceId equals ls.Id
                        join st in stQuery on lead.LeadStageId equals st.Id
                        join s in sQuery on lead.ServiceId equals s.Id
                        select new
                        {
                            lead,
                            CustomerTypeName = ct.Name,
                            LeadSourceName = ls.Name,
                            LeadStageName = st.Name,
                            ServiceName = s.Name
                        };

        // 3. Get total count and paged items from database
        var totalCount = await baseQuery.CountAsync();
        var items = await baseQuery
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToListAsync();

        // 4. Fetch assigned User IDs from the current page
        var userIds = items
            .Where(x => x.lead.AssignedUserId.HasValue)
            .Select(x => x.lead.AssignedUserId.Value)
            .Distinct()
            .ToList();

        // 5. Query IdentityUsers separately (from IIdentityDbContext)
        var userDictionary = new Dictionary<Guid, string>();
        if (userIds.Any())
        {
            var usersQuery = await _userRepository.GetQueryableAsync();
            userDictionary = await usersQuery
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName);
        }

        // 6. Map and attach AssignedUserName in-memory
        var dtos = items.Select(x =>
        {
            var dto = ObjectMapper.Map<Lead, LeadDto>(x.lead);
            dto.CustomerTypeName = x.CustomerTypeName;
            dto.LeadSourceName = x.LeadSourceName;
            dto.LeadStageName = x.LeadStageName;
            dto.ServiceName = x.ServiceName;

            dto.AssignedUserName = x.lead.AssignedUserId.HasValue && userDictionary.TryGetValue(x.lead.AssignedUserId.Value, out var userName)
                ? userName
                : "Unassigned";

            return dto;
        }).ToList();

        return new PagedResultDto<LeadDto>(totalCount, dtos);
    }



    public override async Task<LeadDto> UpdateAsync(
        Guid id,
        CreateUpdateLeadDto input)
    {
        // 1. Get the existing lead before updating.
        var lead = await Repository.GetAsync(id);

        var oldStageId = lead.LeadStageId;
        var newStageId = input.LeadStageId;

        // 2. Update the lead using the existing ABP logic.
        var result = await base.UpdateAsync(id, input);

        // 3. Save history only when the stage changes.
        if (oldStageId != newStageId)
        {
            var history = new LeadStageHistory(
                GuidGenerator.Create(),
                id,
                oldStageId,
                newStageId,
                CurrentTenant.Id,
                "Lead stage changed"
            );

            await _stageHistoryRepository.InsertAsync(
                history,
                autoSave: true
            );
        }

        return result;
    }



    // Import CSV File
    public virtual async Task<int> ImportCsvAsync(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent)) return 0;

        using var stringReader = new StringReader(csvContent);
        using var csv = new CsvReader(stringReader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header.Trim().ToLower()
        });

        var records = csv.GetRecords<LeadCsvRecordDto>().ToList();
        if (!records.Any()) return 0;

        // Fetch database items as a list first, then group by key to safely prevent duplicate key errors
        var customerTypesList = await (await _customerTypeRepository.GetQueryableAsync()).ToListAsync();
        var customerTypes = customerTypesList
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim().ToLower())
            .ToDictionary(g => g.Key, g => g.First().Id);

        var leadSourcesList = await (await _leadSourceRepository.GetQueryableAsync()).ToListAsync();
        var leadSources = leadSourcesList
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim().ToLower())
            .ToDictionary(g => g.Key, g => g.First().Id);

        var leadStagesList = await (await _leadStageRepository.GetQueryableAsync()).ToListAsync();
        var leadStages = leadStagesList
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim().ToLower())
            .ToDictionary(g => g.Key, g => g.First().Id);

        var servicesList = await (await _serviceRepository.GetQueryableAsync()).ToListAsync();
        var services = servicesList
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim().ToLower())
            .ToDictionary(g => g.Key, g => g.First().Id);

        var usersList = await (await _userRepository.GetQueryableAsync()).ToListAsync();
        var users = usersList
            .Where(x => !string.IsNullOrWhiteSpace(x.UserName))
            .GroupBy(x => x.UserName.Trim().ToLower())
            .ToDictionary(g => g.Key, g => g.First().Id);




        var leadsToInsert = new List<Lead>();
        var errors = new List<string>();
        var rowNumber = 1; // header

        foreach (var row in records)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(row.Name)) continue;

            var ctOk = customerTypes.TryGetValue(row.CustomerTypeName?.Trim().ToLower() ?? "", out var ctId);
            var lsOk = leadSources.TryGetValue(row.LeadSourceName?.Trim().ToLower() ?? "", out var lsId);
            var stOk = leadStages.TryGetValue(row.LeadStageName?.Trim().ToLower() ?? "", out var stId);
            var sOk = services.TryGetValue(row.ServiceName?.Trim().ToLower() ?? "", out var sId);

            var rowErrors = new List<string>();
            if (!ctOk) rowErrors.Add($"CustomerType '{row.CustomerTypeName}' not found");
            if (!lsOk) rowErrors.Add($"LeadSource '{row.LeadSourceName}' not found");
            if (!stOk) rowErrors.Add($"LeadStage '{row.LeadStageName}' not found");
            if (!sOk) rowErrors.Add($"Service '{row.ServiceName}' not found");
            if (row.Name.Trim().Length > 128) rowErrors.Add("Name > 128 chars");
            if ((row.CompanyName?.Length ?? 0) > 128) rowErrors.Add("CompanyName > 128 chars");
            if ((row.Email?.Length ?? 0) > 256) rowErrors.Add("Email > 256 chars");
            if ((row.Phone?.Length ?? 0) > 32) rowErrors.Add("Phone > 32 chars");
            if ((row.Remarks?.Length ?? 0) > 1000) rowErrors.Add("Remarks > 1000 chars");

            if (rowErrors.Any())
            {
                errors.Add($"Row {rowNumber}: {string.Join("; ", rowErrors)}");
                continue;
            }

            Guid? assignedUserId = null;
            if (!string.IsNullOrWhiteSpace(row.AssignedUserName) &&
                users.TryGetValue(row.AssignedUserName.Trim().ToLower(), out var uId))
            {
                assignedUserId = uId;
            }

            leadsToInsert.Add(new Lead(
                GuidGenerator.Create(), row.Name.Trim(), ctId, lsId, stId, sId,
                row.CompanyName ?? string.Empty, row.Email ?? string.Empty,
                row.Phone ?? string.Empty, row.EstimatedBudget ?? 0m,
                row.Remarks, assignedUserId));
        }

        if (errors.Any())
        {
            throw new UserFriendlyException(
                "CSV import failed. " + string.Join(" | ", errors.Take(10)) +
                (errors.Count > 10 ? $" ... and {errors.Count - 10} more" : ""));
        }

        if (leadsToInsert.Any())
        {
            // autoSave: true runs SaveChanges now, inside the page's try/catch
            await Repository.InsertManyAsync(leadsToInsert, autoSave: true);
        }

        return leadsToInsert.Count;
    }



    // O(1) Single-Query Projection with AsNoTracking()

    public async Task<LeadDetailDto> GetDetailAsync(Guid id)
    {
        // 1. Get CRM data only. Do not join IdentityUser here.
        var leads = (await Repository.GetQueryableAsync()).AsNoTracking();
        var customerTypes = (await _customerTypeRepository.GetQueryableAsync()).AsNoTracking();
        var leadSources = (await _leadSourceRepository.GetQueryableAsync()).AsNoTracking();
        var leadStages = (await _leadStageRepository.GetQueryableAsync()).AsNoTracking();
        var services = (await _serviceRepository.GetQueryableAsync()).AsNoTracking();

        var query =
            from l in leads
            where l.Id == id

            join ct0 in customerTypes on l.CustomerTypeId equals ct0.Id into ctGroup
            from ct in ctGroup.DefaultIfEmpty()

            join ls0 in leadSources on l.LeadSourceId equals ls0.Id into lsGroup
            from ls in lsGroup.DefaultIfEmpty()

            join st0 in leadStages on l.LeadStageId equals st0.Id into stGroup
            from st in stGroup.DefaultIfEmpty()

            join sv0 in services on l.ServiceId equals sv0.Id into svGroup
            from sv in svGroup.DefaultIfEmpty()

            select new LeadDetailDto
            {
                Id = l.Id,
                TenantId = l.TenantId,
                ExternalId = l.ExternalId,
                ConcurrencyStamp = l.ConcurrencyStamp,

                Name = l.Name,
                CompanyName = l.CompanyName,
                Email = l.Email,
                Phone = l.Phone,
                EstimatedBudget = l.EstimatedBudget,
                ExpectedCloseDate = l.ExpectedCloseDate,
                NextFollowUpDate = l.NextFollowUpDate,
                Remarks = l.Remarks,

                CustomerTypeId = l.CustomerTypeId,
                CustomerTypeName = ct != null ? ct.Name : "-",

                LeadSourceId = l.LeadSourceId,
                LeadSourceName = ls != null ? ls.Name : "-",

                LeadStageId = l.LeadStageId,
                LeadStageName = st != null ? st.Name : "-",

                ServiceId = l.ServiceId,
                ServiceName = sv != null ? sv.Name : "-",

                AssignedUserId = l.AssignedUserId,
                CreationTime = l.CreationTime,
                CreatorId = l.CreatorId,
                LastModificationTime = l.LastModificationTime,
                LastModifierId = l.LastModifierId,

                // Filled from Identity DB after executing the CRM query.
                AssignedUserName = "Unassigned",
                CreatorUserName = "System",
                LastModifierUserName = "-"
            };

        // 2. Execute the CRM query before accessing Identity.
        var result = await query.FirstOrDefaultAsync();

        if (result == null)
        {
            throw new EntityNotFoundException(typeof(Lead), id);
        }

        // 3. Collect Identity user IDs.
        var userIds = new[]
        {
        result.AssignedUserId,
        result.CreatorId,
        result.LastModifierId
    }
        .Where(userId => userId.HasValue)
        .Select(userId => userId!.Value)
        .Distinct()
        .ToList();

        // 4. Query Identity separately (different DbContext).
        if (userIds.Count > 0)
        {
            var usersQuery = (await _userRepository.GetQueryableAsync())
                .AsNoTracking();

            var userDictionary = await usersQuery
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.UserName
                })
                .ToDictionaryAsync(u => u.Id, u => u.UserName);

            // 5. Populate the user names in memory.
            if (result.AssignedUserId.HasValue &&
                userDictionary.TryGetValue(
                    result.AssignedUserId.Value, out var assignedUser))
            {
                result.AssignedUserName = assignedUser ?? "Unassigned";
            }

            if (result.CreatorId.HasValue &&
                userDictionary.TryGetValue(
                    result.CreatorId.Value, out var creatorUser))
            {
                result.CreatorUserName = creatorUser ?? "System";
            }

            if (result.LastModifierId.HasValue &&
                userDictionary.TryGetValue(
                    result.LastModifierId.Value, out var modifierUser))
            {
                result.LastModifierUserName = modifierUser ?? "-";
            }
        }

        return result;
    }


    // O(log N + K) Indexed Lookup with AsNoTracking()

    public async Task<List<LeadStageHistoryDto>> GetStageHistoryAsync(Guid leadId)
    {
        // 1. History and stage data: CRM DbContext only
        var historiesQuery = (await _stageHistoryRepository.GetQueryableAsync())
            .AsNoTracking();

        var stagesQuery = (await _leadStageRepository.GetQueryableAsync())
            .AsNoTracking();

        var crmItems = await (
            from h in historiesQuery
            where h.LeadId == leadId

            join oldStageItem in stagesQuery
                on h.OldStageId equals oldStageItem.Id into oldStageGroup
            from oldStage in oldStageGroup.DefaultIfEmpty()

            join newStageItem in stagesQuery
                on h.NewStageId equals newStageItem.Id into newStageGroup
            from newStage in newStageGroup.DefaultIfEmpty()

            orderby h.CreationTime descending

            select new
            {
                h.Id,
                h.LeadId,
                h.OldStageId,
                OldStageName = oldStage != null
                    ? oldStage.Name
                    : "Initial Creation",
                h.NewStageId,
                NewStageName = newStage != null
                    ? newStage.Name
                    : "Unknown Stage",
                h.CreationTime,
                h.CreatorId,
                h.Remarks
            }
        ).ToListAsync();

        // 2. Get Creator IDs
        var creatorIds = crmItems
            .Where(x => x.CreatorId.HasValue)
            .Select(x => x.CreatorId!.Value)
            .Distinct()
            .ToList();

        // 3. Identity DbContext query separately
        var userDictionary = new Dictionary<Guid, string>();

        if (creatorIds.Count > 0)
        {
            var usersQuery = (await _userRepository.GetQueryableAsync())
                .AsNoTracking();

            userDictionary = await usersQuery
                .Where(u => creatorIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.UserName
                })
                .ToDictionaryAsync(
                    u => u.Id,
                    u => u.UserName ?? "System"
                );
        }

        // 4. Build DTOs in memory
        return crmItems.Select(x => new LeadStageHistoryDto
        {
            Id = x.Id,
            LeadId = x.LeadId,
            OldStageId = x.OldStageId,
            OldStageName = x.OldStageName,
            NewStageId = x.NewStageId,
            NewStageName = x.NewStageName,
            CreationTime = x.CreationTime,
            CreatorId = x.CreatorId,
            ChangedByUserName =
                x.CreatorId.HasValue &&
                userDictionary.TryGetValue(x.CreatorId.Value, out var userName)
                    ? userName
                    : "System",
            Remarks = x.Remarks
        }).ToList();
    }


    public async Task<List<LeadDto>> GetFollowUpListAsync(GetFollowUpListInput input)
    {
        // 1. Fetch CRM Queryables ONLY (all from CRMDbContext)
        var leadQuery = (await Repository.GetQueryableAsync()).AsNoTracking();
        var ctQuery = (await _customerTypeRepository.GetQueryableAsync()).AsNoTracking();
        var lsQuery = (await _leadSourceRepository.GetQueryableAsync()).AsNoTracking();
        var stQuery = (await _leadStageRepository.GetQueryableAsync()).AsNoTracking();
        var sQuery = (await _serviceRepository.GetQueryableAsync()).AsNoTracking();

        // Filter only leads that have a NextFollowUpDate
        leadQuery = leadQuery.Where(x => x.NextFollowUpDate != null);

        if (input.FromDate.HasValue)
        {
            var from = input.FromDate.Value.Date;
            leadQuery = leadQuery.Where(x => x.NextFollowUpDate >= from);
        }

        if (input.ToDate.HasValue)
        {
            // Inclusive of the entire ToDate day (up to 23:59:59.9999999)
            var toExclusive = input.ToDate.Value.Date.AddDays(1);
            leadQuery = leadQuery.Where(x => x.NextFollowUpDate < toExclusive);
        }

        if (input.LeadStageId.HasValue)
        {
            leadQuery = leadQuery.Where(x => x.LeadStageId == input.LeadStageId.Value);
        }

        if (input.AssignedUserId.HasValue)
        {
            leadQuery = leadQuery.Where(x => x.AssignedUserId == input.AssignedUserId.Value);
        }

        // 2. Execute the CRM query first (Single DbContext: CRMDbContext)
        var crmItems = await (
            from lead in leadQuery
            join ct in ctQuery on lead.CustomerTypeId equals ct.Id into ctLeft
            from ct in ctLeft.DefaultIfEmpty()
            join ls in lsQuery on lead.LeadSourceId equals ls.Id into lsLeft
            from ls in lsLeft.DefaultIfEmpty()
            join st in stQuery on lead.LeadStageId equals st.Id into stLeft
            from st in stLeft.DefaultIfEmpty()
            join s in sQuery on lead.ServiceId equals s.Id into sLeft
            from s in sLeft.DefaultIfEmpty()
            orderby lead.NextFollowUpDate ascending
            select new LeadDto
            {
                Id = lead.Id,
                Name = lead.Name,
                CompanyName = lead.CompanyName,
                Email = lead.Email,
                Phone = lead.Phone,
                EstimatedBudget = lead.EstimatedBudget,
                ExpectedCloseDate = lead.ExpectedCloseDate,
                NextFollowUpDate = lead.NextFollowUpDate,
                Remarks = lead.Remarks,
                CustomerTypeId = lead.CustomerTypeId,
                CustomerTypeName = ct != null ? ct.Name : "-",
                LeadSourceId = lead.LeadSourceId,
                LeadSourceName = ls != null ? ls.Name : "-",
                LeadStageId = lead.LeadStageId,
                LeadStageName = st != null ? st.Name : "-",
                ServiceId = lead.ServiceId,
                ServiceName = s != null ? s.Name : "-",
                AssignedUserId = lead.AssignedUserId,
                AssignedUserName = "Unassigned", // Populated in Step 4
                CreationTime = lead.CreationTime
            })
            .Take(input.MaxResultCount > 0 ? input.MaxResultCount : 1000)
            .ToListAsync();

        // 3. Collect distinct AssignedUserIds from the CRM results
        var userIds = crmItems
            .Where(x => x.AssignedUserId.HasValue)
            .Select(x => x.AssignedUserId!.Value)
            .Distinct()
            .ToList();

        // 4. Query IdentityUsers separately (Single DbContext: IIdentityDbContext)
        if (userIds.Count > 0)
        {
            var uQuery = (await _userRepository.GetQueryableAsync()).AsNoTracking();
            var userDictionary = await uQuery
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToDictionaryAsync(u => u.Id, u => u.UserName);

            foreach (var item in crmItems)
            {
                if (item.AssignedUserId.HasValue &&
                    userDictionary.TryGetValue(item.AssignedUserId.Value, out var userName))
                {
                    item.AssignedUserName = userName ?? "Unassigned";
                }
            }
        }

        return crmItems
        .OrderByDescending(x => x.NextFollowUpDate)
        .ToList();
    }


    public async Task RescheduleFollowUpAsync(RescheduleFollowUpDto input)
    {
        var lead = await Repository.GetAsync(input.LeadId);
        var oldStageId = lead.LeadStageId;

        lead.NextFollowUpDate = input.NextFollowUpDate;
        lead.LeadStageId = input.LeadStageId;
        lead.AssignedUserId = input.AssignedUserId;
        lead.Remarks = input.Remarks;

        await Repository.UpdateAsync(lead, autoSave: true);

        // If stage changed during follow-up, record it in LeadStageHistory automatically
        if (oldStageId != input.LeadStageId)
        {
            await _stageHistoryRepository.InsertAsync(new LeadStageHistory(
                GuidGenerator.Create(),
                lead.Id,
                oldStageId,
                input.LeadStageId,
                CurrentTenant.Id,
                string.IsNullOrWhiteSpace(input.Remarks) ? "Updated via Follow-Up" : input.Remarks
            ), autoSave: true);
        }
    }

}