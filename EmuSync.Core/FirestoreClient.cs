using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EmuSync.Core;

/// <summary>
/// Minimal Cloud Firestore client over the REST API, authenticated with the
/// Firebase ID token of the signed-in user (so the security rules apply).
///
/// Like <see cref="FirebaseAuthClient"/> it only needs HttpClient, which keeps
/// the whole storage layer portable to Android.
///
/// Documents are exchanged as plain dictionaries: the REST "typed value" wire
/// format is handled by <see cref="ToValue"/> / <see cref="FromValue"/>.
/// </summary>
public class FirestoreClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private readonly FirebaseOptions _options;
    private readonly FirebaseAuthClient _auth;

    public FirestoreClient(FirebaseOptions options, FirebaseAuthClient auth)
    {
        _options = options;
        _auth = auth;
    }

    /// <summary>Reads a document. Returns null when it does not exist yet.</summary>
    public async Task<Dictionary<string, object?>?> GetDocumentAsync(string path, CancellationToken ct = default)
    {
        using var request = await NewRequestAsync(HttpMethod.Get, Url(path), ct);
        using var response = await Http.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        string body = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccess(response, body);

        using var doc = JsonDocument.Parse(body);
        return ReadFields(doc.RootElement);
    }

    /// <summary>
    /// Creates or updates a document. Only the top-level fields passed in are
    /// touched (an update mask is always sent), so two devices writing different
    /// fields of the same document never clobber each other.
    /// </summary>
    public async Task PatchDocumentAsync(string path, Dictionary<string, object?> fields, CancellationToken ct = default)
    {
        var query = new StringBuilder();
        foreach (var key in fields.Keys)
            query.Append(query.Length == 0 ? '?' : '&').Append("updateMask.fieldPaths=").Append(Uri.EscapeDataString(key));

        string payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["fields"] = fields.ToDictionary(kv => kv.Key, kv => ToValue(kv.Value))
        });

        using var request = await NewRequestAsync(new HttpMethod("PATCH"), Url(path) + query, ct);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await Http.SendAsync(request, ct);
        EnsureSuccess(response, await response.Content.ReadAsStringAsync(ct));
    }

    public async Task DeleteDocumentAsync(string path, CancellationToken ct = default)
    {
        using var request = await NewRequestAsync(HttpMethod.Delete, Url(path), ct);
        using var response = await Http.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return;
        EnsureSuccess(response, await response.Content.ReadAsStringAsync(ct));
    }

    /// <summary>Lists the documents of a collection, following pagination.</summary>
    public async Task<List<(string Id, Dictionary<string, object?> Fields)>> ListDocumentsAsync(
        string collectionPath, CancellationToken ct = default)
    {
        var result = new List<(string, Dictionary<string, object?>)>();
        string? pageToken = null;

        do
        {
            string url = Url(collectionPath) + "?pageSize=300";
            if (pageToken != null) url += "&pageToken=" + Uri.EscapeDataString(pageToken);

            using var request = await NewRequestAsync(HttpMethod.Get, url, ct);
            using var response = await Http.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.NotFound) break;

            string body = await response.Content.ReadAsStringAsync(ct);
            EnsureSuccess(response, body);

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("documents", out var docs))
            {
                foreach (var d in docs.EnumerateArray())
                {
                    string name = d.GetProperty("name").GetString() ?? "";
                    string id = name[(name.LastIndexOf('/') + 1)..];
                    result.Add((id, ReadFields(d)));
                }
            }

            pageToken = doc.RootElement.TryGetProperty("nextPageToken", out var tok) ? tok.GetString() : null;
        } while (pageToken != null);

        return result;
    }

    /// <summary>
    /// Runs a structured query against one collection. Needed wherever plain
    /// listing is not enough — "the 100 most recent runs", "everything older than
    /// a year" — because <see cref="ListDocumentsAsync"/> can only walk documents
    /// in name order.
    /// </summary>
    /// <param name="parentPath">Document holding the collection, e.g. <c>users/{uid}</c>.</param>
    /// <param name="collectionId">Collection to query, e.g. <c>activity</c>.</param>
    /// <param name="where">Optional filter: field, operator (LESS_THAN, EQUAL, ...) and value.</param>
    public async Task<List<(string Id, Dictionary<string, object?> Fields)>> RunQueryAsync(
        string parentPath,
        string collectionId,
        string? orderByField = null,
        bool descending = true,
        int? limit = null,
        (string Field, string Op, object? Value)? where = null,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, object?>
        {
            ["from"] = new List<object?> { new Dictionary<string, object?> { ["collectionId"] = collectionId } }
        };

        if (where.HasValue)
        {
            query["where"] = new Dictionary<string, object?>
            {
                ["fieldFilter"] = new Dictionary<string, object?>
                {
                    ["field"] = new Dictionary<string, object?> { ["fieldPath"] = where.Value.Field },
                    ["op"] = where.Value.Op,
                    ["value"] = ToValue(where.Value.Value)
                }
            };
        }

        if (orderByField != null)
        {
            query["orderBy"] = new List<object?>
            {
                new Dictionary<string, object?>
                {
                    ["field"] = new Dictionary<string, object?> { ["fieldPath"] = orderByField },
                    ["direction"] = descending ? "DESCENDING" : "ASCENDING"
                }
            };
        }

        if (limit.HasValue) query["limit"] = limit.Value;

        string payload = JsonSerializer.Serialize(new Dictionary<string, object?> { ["structuredQuery"] = query });

        using var request = await NewRequestAsync(HttpMethod.Post,
            $"{_options.FirestoreBaseUrl}/{parentPath.TrimStart('/')}:runQuery", ct);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await Http.SendAsync(request, ct);
        string body = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccess(response, body);

        var result = new List<(string, Dictionary<string, object?>)>();
        using var doc = JsonDocument.Parse(body);

        foreach (var entry in doc.RootElement.EnumerateArray())
        {
            // Entries without a 'document' are just read-time markers.
            if (!entry.TryGetProperty("document", out var document)) continue;
            string name = document.GetProperty("name").GetString() ?? "";
            result.Add((name[(name.LastIndexOf('/') + 1)..], ReadFields(document)));
        }

        return result;
    }

    // ---------------------------------------------------------------- plumbing

    private string Url(string path) => $"{_options.FirestoreBaseUrl}/{path.TrimStart('/')}";

    private async Task<HttpRequestMessage> NewRequestAsync(HttpMethod method, string url, CancellationToken ct)
    {
        _options.EnsureConfigured();
        string token = await _auth.GetIdTokenAsync(ct);
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static void EnsureSuccess(HttpResponseMessage response, string body)
    {
        if (response.IsSuccessStatusCode) return;

        string message = body;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var msg))
                message = msg.GetString() ?? body;
        }
        catch { /* keep the raw body */ }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new FirebaseAuthException("PERMISSION_DENIED",
                "Firestore refused the request: " + message +
                "\n(Check the security rules and that you are signed in.)");

        throw new IOException($"Firestore ({(int)response.StatusCode}): {message}");
    }

    private static Dictionary<string, object?> ReadFields(JsonElement document)
    {
        var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (!document.TryGetProperty("fields", out var el)) return fields;
        foreach (var property in el.EnumerateObject())
            fields[property.Name] = FromValue(property.Value);
        return fields;
    }

    // ------------------------------------------------- typed value conversion

    /// <summary>CLR value → Firestore REST "Value".</summary>
    private static object ToValue(object? value)
    {
        switch (value)
        {
            case null:
                return new Dictionary<string, object?> { ["nullValue"] = null };
            case string s:
                return new Dictionary<string, object?> { ["stringValue"] = s };
            case bool b:
                return new Dictionary<string, object?> { ["booleanValue"] = b };
            case int or long or short or byte:
                // Firestore carries 64-bit integers as strings.
                return new Dictionary<string, object?> { ["integerValue"] = Convert.ToInt64(value).ToString(CultureInfo.InvariantCulture) };
            case double or float or decimal:
                return new Dictionary<string, object?> { ["doubleValue"] = Convert.ToDouble(value, CultureInfo.InvariantCulture) };
            case DateTime dt:
                return new Dictionary<string, object?>
                {
                    ["timestampValue"] = dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)
                };
            case IDictionary<string, object?> map:
                return new Dictionary<string, object?>
                {
                    ["mapValue"] = new Dictionary<string, object?>
                    {
                        ["fields"] = map.ToDictionary(kv => kv.Key, kv => ToValue(kv.Value))
                    }
                };
            case System.Collections.IEnumerable list:
                var values = new List<object>();
                foreach (var item in list) values.Add(ToValue(item));
                return new Dictionary<string, object?>
                {
                    ["arrayValue"] = new Dictionary<string, object?> { ["values"] = values }
                };
            default:
                return new Dictionary<string, object?> { ["stringValue"] = value.ToString() ?? "" };
        }
    }

    /// <summary>Firestore REST "Value" → CLR value.</summary>
    private static object? FromValue(JsonElement value)
    {
        foreach (var property in value.EnumerateObject())
        {
            switch (property.Name)
            {
                case "nullValue":
                    return null;
                case "stringValue":
                    return property.Value.GetString();
                case "booleanValue":
                    return property.Value.GetBoolean();
                case "integerValue":
                    return property.Value.ValueKind == JsonValueKind.String
                        ? long.Parse(property.Value.GetString()!, CultureInfo.InvariantCulture)
                        : property.Value.GetInt64();
                case "doubleValue":
                    return property.Value.GetDouble();
                case "timestampValue":
                    return DateTime.Parse(property.Value.GetString()!, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
                case "mapValue":
                    var map = new Dictionary<string, object?>(StringComparer.Ordinal);
                    if (property.Value.TryGetProperty("fields", out var fields))
                        foreach (var f in fields.EnumerateObject())
                            map[f.Name] = FromValue(f.Value);
                    return map;
                case "arrayValue":
                    var list = new List<object?>();
                    if (property.Value.TryGetProperty("values", out var items))
                        foreach (var item in items.EnumerateArray())
                            list.Add(FromValue(item));
                    return list;
            }
        }
        return null;
    }

    // ----------------------------------------------------- reading helpers

    public static string GetString(Dictionary<string, object?> fields, string key, string fallback = "") =>
        fields.TryGetValue(key, out var v) && v is string s ? s : fallback;

    public static bool GetBool(Dictionary<string, object?> fields, string key, bool fallback = false) =>
        fields.TryGetValue(key, out var v) && v is bool b ? b : fallback;

    public static int GetInt(Dictionary<string, object?> fields, string key, int fallback = 0) =>
        fields.TryGetValue(key, out var v) && v is long l ? (int)l : fallback;

    public static DateTime? GetDateTime(Dictionary<string, object?> fields, string key) =>
        fields.TryGetValue(key, out var v) && v is DateTime d ? d : null;

    public static List<object?> GetList(Dictionary<string, object?> fields, string key) =>
        fields.TryGetValue(key, out var v) && v is List<object?> l ? l : new List<object?>();

    public static Dictionary<string, object?> GetMap(Dictionary<string, object?> fields, string key) =>
        fields.TryGetValue(key, out var v) && v is Dictionary<string, object?> m ? m : new Dictionary<string, object?>();
}
