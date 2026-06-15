using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace DataBuilder.Data;

public sealed class CsvDataProvider
{
    private const string CsvBaseUrl = "https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/en";
    private static readonly TimeSpan CacheMaxAge = TimeSpan.FromDays(30);

    private readonly HttpClient _http;
    private readonly string _csvCacheDir;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly Dictionary<string, QuestCsvRow> _questByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, QuestCsvRow> _questById = new();
    private readonly Dictionary<string, AchievementCsvRow> _achievementByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, AchievementCsvRow> _achievementById = new();
    private readonly Dictionary<int, string> _npcNames = new();
    private readonly Dictionary<int, string> _territoryNames = new();
    private readonly Dictionary<int, string> _placeNames = new();
    private bool _initialized;

    public CsvDataProvider(HttpClient http, string cacheDir)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _csvCacheDir = cacheDir ?? throw new ArgumentNullException(nameof(cacheDir));
    }

    public async Task InitializeAsync(bool forceRefresh = false)
    {
        await _lock.WaitAsync();
        try
        {
            if (forceRefresh)
            {
                _initialized = false;
                _questByName.Clear();
                _questById.Clear();
                _npcNames.Clear();
                _territoryNames.Clear();
                _placeNames.Clear();
            }

            if (_initialized) return;

            Directory.CreateDirectory(_csvCacheDir);

            var requiredCsvFiles = new[] { "Quest.csv", "Achievement.csv", "ENpcResident.csv", "TerritoryType.csv", "PlaceName.csv" };
            var questPath = Path.Combine(_csvCacheDir, "Quest.csv");
            var isCached = File.Exists(questPath);
            var cacheAge = isCached
                ? DateTime.UtcNow - File.GetLastWriteTimeUtc(questPath)
                : TimeSpan.MaxValue;

            if (!isCached || cacheAge > CacheMaxAge || forceRefresh)
            {
                foreach (var file in requiredCsvFiles)
                    await DownloadCsvAsync(file);
            }
            else if (requiredCsvFiles.Any(f => !File.Exists(Path.Combine(_csvCacheDir, f))))
            {
                foreach (var file in requiredCsvFiles.Where(f => !File.Exists(Path.Combine(_csvCacheDir, f))))
                    await DownloadCsvAsync(file);
            }

            LoadQuests();
            LoadAchievements();
            LoadNpcs();
            LoadTerritories();
            LoadPlaceNames();

            _initialized = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public QuestCsvRow? LookupQuest(string name)
    {
        var normalized = NormalizeName(name);
        if (_questByName.TryGetValue(normalized, out var row))
            return row;

        var noParen = RemoveParentheticals(normalized);
        if (noParen != normalized && _questByName.TryGetValue(noParen, out row))
            return row;

        return null;
    }

    public QuestCsvRow? LookupQuestById(int questId)
    {
        return _questById.TryGetValue(questId, out var row) ? row : null;
    }

    public string? ResolveNpcName(int npcId)
    {
        return _npcNames.TryGetValue(npcId, out var name) ? name : null;
    }

    public string? ResolveTerritoryName(int territoryId)
    {
        return _territoryNames.TryGetValue(territoryId, out var name) ? name : null;
    }

    public string? ResolvePlaceName(int placeNameId)
    {
        return _placeNames.TryGetValue(placeNameId, out var name) ? name : null;
    }

    public string? ResolveQuestName(int questId)
    {
        return _questById.TryGetValue(questId, out var row) ? row.Name : null;
    }

    internal static string NormalizeName(string name)
    {
        return string.Concat(name
            .Replace('\u2018', '\'')
            .Replace('\u2019', '\'')
            .Replace('\u201c', '"')
            .Replace('\u201d', '"')
            .Where(c => c < 0xE000 || c > 0xF8FF))
            .Trim()
            .Trim('"');
    }

    internal static string RemoveParentheticals(string name)
    {
        var idx = name.IndexOf('(');
        if (idx <= 0) return name;
        return name[..idx].Trim();
    }

    private async Task DownloadCsvAsync(string fileName)
    {
        var url = $"{CsvBaseUrl}/{fileName}";
        var filePath = Path.Combine(_csvCacheDir, fileName);

        Console.WriteLine($"  Downloading {fileName}...");
        try
        {
            var bytes = await _http.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(filePath, bytes);
            File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow);
        }
        catch
        {
            try { File.Delete(filePath); } catch { }
            Console.Error.WriteLine($"  Failed to download {fileName}");
            throw;
        }
    }

    private void LoadQuests()
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var reader = new StreamReader(Path.Combine(_csvCacheDir, "Quest.csv"));
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();
        var records = csv.GetRecords<QuestCsvRow>();

        foreach (var row in records)
        {
            if (row.Id == 0) continue;
            row.Name = NormalizeName(row.Name);
            if (!string.IsNullOrEmpty(row.Name))
            {
                _questByName[row.Name] = row;
                _questById[row.Id] = row;
            }
        }

        Console.WriteLine($"  Loaded {_questByName.Count} quests from CSV.");
    }

    private void LoadAchievements()
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var reader = new StreamReader(Path.Combine(_csvCacheDir, "Achievement.csv"));
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();
        var records = csv.GetRecords<AchievementCsvRow>();

        foreach (var row in records)
        {
            if (row.Id == 0) continue;
            var key = NormalizeName(row.Name);
            if (!string.IsNullOrEmpty(key))
            {
                _achievementByName[key] = row;
                _achievementById[row.Id] = row;
            }
        }

        Console.WriteLine($"  Loaded {_achievementByName.Count} achievements from CSV.");
    }

    public AchievementCsvRow? LookupAchievement(string name)
    {
        var normalized = NormalizeName(name);
        if (_achievementByName.TryGetValue(normalized, out var row))
            return row;

        var noParen = RemoveParentheticals(normalized);
        if (noParen != normalized && _achievementByName.TryGetValue(noParen, out row))
            return row;

        return null;
    }

    public AchievementCsvRow? LookupAchievementById(int achievementId)
    {
        return _achievementById.TryGetValue(achievementId, out var row) ? row : null;
    }

    private void LoadNpcs()
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var reader = new StreamReader(Path.Combine(_csvCacheDir, "ENpcResident.csv"));
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();
        var records = csv.GetRecords<EnpcResidentCsvRow>();

        foreach (var row in records)
        {
            if (row.Id == 0 || row.Id < 1000000) continue;
            if (!string.IsNullOrEmpty(row.Singular))
                _npcNames[row.Id] = row.Singular;
        }
    }

    private void LoadTerritories()
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var reader = new StreamReader(Path.Combine(_csvCacheDir, "TerritoryType.csv"));
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();
        var records = csv.GetRecords<TerritoryTypeCsvRow>();

        foreach (var row in records)
        {
            if (row.Id == 0) continue;
            if (!string.IsNullOrEmpty(row.Name))
                _territoryNames[row.Id] = row.Name;
        }
    }

    private void LoadPlaceNames()
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var reader = new StreamReader(Path.Combine(_csvCacheDir, "PlaceName.csv"));
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();
        var records = csv.GetRecords<PlaceNameCsvRow>();

        foreach (var row in records)
        {
            if (row.Id == 0) continue;
            if (!string.IsNullOrEmpty(row.Name))
                _placeNames[row.Id] = row.Name;
        }
    }
}
