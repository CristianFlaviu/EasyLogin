namespace EasyLogin.Application.Common;

public static class AuditDiffBuilder
{
    public static IDictionary<string, string> ForField<T>(string field, T? before, T? after, IDictionary<string, string>? acc = null)
    {
        acc ??= new Dictionary<string, string>();
        var b = before?.ToString() ?? string.Empty;
        var a = after?.ToString() ?? string.Empty;
        if (!string.Equals(b, a, StringComparison.Ordinal))
            acc[field] = $"{b} -> {a}";
        return acc;
    }

    public static IDictionary<string, string> ForCollection(
        string field, IEnumerable<string>? before, IEnumerable<string>? after, IDictionary<string, string>? acc = null)
    {
        acc ??= new Dictionary<string, string>();
        var b = (before ?? []).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var a = (after ?? []).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        if (!b.SequenceEqual(a, StringComparer.Ordinal))
            acc[field] = $"[{string.Join(',', b)}] -> [{string.Join(',', a)}]";
        return acc;
    }
}
