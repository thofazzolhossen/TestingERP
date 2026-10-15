using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Volo.Abp.DependencyInjection;

namespace CRM.Messenger;

public class MessengerFileStore : ITransientDependency
{
    private readonly string _root;

    public MessengerFileStore(IConfiguration config)
    {
        var configured = config["Messenger:FilesPath"];
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "messenger-files")
            : configured;
    }

    public async Task<string> SaveAsync(byte[] content, string fileName)
    {
        Directory.CreateDirectory(_root);

        var ext = Path.GetExtension(fileName ?? "");
        if (ext.Length > 10 || !ext.All(c => char.IsLetterOrDigit(c) || c == '.')) ext = "";

        var stored = Guid.NewGuid().ToString("N") + ext.ToLowerInvariant();
        await File.WriteAllBytesAsync(Path.Combine(_root, stored), content);
        return stored;
    }

    public async Task<byte[]?> ReadAsync(string storedName)
    {
        var path = Path.Combine(_root, Path.GetFileName(storedName));
        return File.Exists(path) ? await File.ReadAllBytesAsync(path) : null;
    }
}