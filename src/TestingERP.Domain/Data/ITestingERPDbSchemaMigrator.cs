using System.Threading.Tasks;

namespace TestingERP.Data;

public interface ITestingERPDbSchemaMigrator
{
    Task MigrateAsync();
}
