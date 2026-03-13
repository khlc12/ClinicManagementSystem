namespace ClinicManagementSystem.Services;

public static class IdGenerator
{
    public static string Next(string prefix, int count) => $"{prefix}-{count + 1:000}";

    public static string Next(string prefix, IEnumerable<string> existingIds)
    {
        var nextNumber = existingIds
            .Where(id => id.StartsWith($"{prefix}-", StringComparison.OrdinalIgnoreCase))
            .Select(id => id[(prefix.Length + 1)..])
            .Select(value => int.TryParse(value, out var number) ? number : 0)
            .DefaultIfEmpty()
            .Max() + 1;

        return $"{prefix}-{nextNumber:000}";
    }
}
