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
    public async Task<int> ImportCsvAsync(string csvContent)
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

        foreach (var row in records)
        {
            if (string.IsNullOrWhiteSpace(row.Name)) continue;

            // Resolve Foreign Key GUIDs (fallback to Guid.Empty or default if lookup not found)
            customerTypes.TryGetValue(row.CustomerTypeName?.Trim().ToLower() ?? "", out var ctId);
            leadSources.TryGetValue(row.LeadSourceName?.Trim().ToLower() ?? "", out var lsId);
            leadStages.TryGetValue(row.LeadStageName?.Trim().ToLower() ?? "", out var stId);
            services.TryGetValue(row.ServiceName?.Trim().ToLower() ?? "", out var sId);

            Guid? assignedUserId = null;
            if (!string.IsNullOrWhiteSpace(row.AssignedUserName) &&
                users.TryGetValue(row.AssignedUserName.Trim().ToLower(), out var uId))
            {
                assignedUserId = uId;
            }

            var lead = new Lead(
                GuidGenerator.Create(),
                row.Name.Trim(),
                ctId,
                lsId,
                stId,
                sId,
                row.CompanyName ?? string.Empty,
                row.Email ?? string.Empty,
                row.Phone ?? string.Empty,
                row.EstimatedBudget ?? 0m,
                row.Remarks,
                assignedUserId
            );

            leadsToInsert.Add(lead);
        }

        if (leadsToInsert.Any())
        {
            await Repository.InsertManyAsync(leadsToInsert);
        }

        return leadsToInsert.Count;
    }

}