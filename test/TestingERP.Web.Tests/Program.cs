using Microsoft.AspNetCore.Builder;
using TestingERP;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("TestingERP.Web.csproj"); 
await builder.RunAbpModuleAsync<TestingERPWebTestModule>(applicationName: "TestingERP.Web");

public partial class Program
{
}
