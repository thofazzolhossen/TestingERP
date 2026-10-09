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
    private readonly IRepository<IdentityUser, Guid> _userRepository; // Change here

    public LeadAppService(
        IRepository<Lead, Guid> repository,
        IRepository<CustomerType, Guid> customerTypeRepository,
        IRepository<LeadSource, Guid> leadSourceRepository,
        IRepository<LeadStage, Guid> leadStageRepository,
        IRepository<Service, Guid> serviceRepository,
        IRepository<IdentityUser, Guid> userRepository) : base(repository) // Change here
    {
        _customerTypeRepository = customerTypeRepository;
        _leadSourceRepository = leadSourceRepository;
        _leadStageRepository = leadStageRepository;
        _serviceRepository = serviceRepository;
        _userRepository = userRepository;
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

}