using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TestingERP.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class TestingERPDbContextFactory : IDesignTimeDbContextFactory<TestingERPDbContext>
{
    public TestingERPDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        
        TestingERPEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<TestingERPDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));
        
        return new TestingERPDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../TestingERP.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
