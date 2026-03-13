using System.Text.Json;
using ClinicManagementSystem.Models;

namespace ClinicManagementSystem.Data;

public class JsonDataStore : IClinicDataStore
{
    private readonly string filePath;
    private readonly JsonSerializerOptions serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonDataStore(string filePath)
    {
        this.filePath = filePath;
    }

    public AppData Load()
    {
        if (!File.Exists(filePath))
        {
            var seeded = SeedDataFactory.Create();
            Save(seeded);
            return seeded;
        }

        var json = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return SeedDataFactory.Create();
        }

        var snapshot = JsonSerializer.Deserialize<AppDataSnapshot>(json, serializerOptions);
        return snapshot is null ? SeedDataFactory.Create() : AppData.FromSnapshot(snapshot);
    }

    public void Save(AppData data)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(data.ToSnapshot(), serializerOptions);
        File.WriteAllText(filePath, json);
    }
}
