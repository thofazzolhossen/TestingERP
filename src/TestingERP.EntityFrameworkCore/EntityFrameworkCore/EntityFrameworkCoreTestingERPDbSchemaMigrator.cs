using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestingERP.Data;
using Volo.Abp.DependencyInjection;

namespace TestingERP.EntityFrameworkCore;

public class EntityFrameworkCoreTestingERPDbSchemaMigrator
    : ITestingERPDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreTestingERPDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the TestingERPDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<TestingERPDbContext>()
            .Database
            .MigrateAsync();
    }
}
