using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace TestingERP.Data;

/* This is used if database provider does't define
 * ITestingERPDbSchemaMigrator implementation.
 */
public class NullTestingERPDbSchemaMigrator : ITestingERPDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
