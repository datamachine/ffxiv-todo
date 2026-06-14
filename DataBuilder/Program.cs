using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DataBuilder.Formatters;
using DataBuilder.Models;
using DataBuilder.Data;
using DataBuilder.Scrapers;

namespace DataBuilder;

class Program
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    static async Task<int> Main(string[] args)
    {
        var fromStage = "scratch";
        var outputPath = "FfxivTodo/Data/content.json";
        var skipIdResolution = false;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--from" && i + 1 < args.Length)
                fromStage = args[++i];
            if (args[i] == "--output" && i + 1 < args.Length)
                outputPath = args[++i];
            if (args[i] == "--skip-id-resolution")
                skipIdResolution = true;
        }

        var cacheDir = "Cache";
        Directory.CreateDirectory(cacheDir);

        var catFile = Path.Combine(cacheDir, "category_items.json");
        var detailFile = Path.Combine(cacheDir, "detail_items.json");
        var resolvedFile = Path.Combine(cacheDir, "resolved_items.json");

        List<CategoryItem> categoryItems;
        List<DetailItem> detailItems;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "FfxivTodo-DataBuilder/1.0");

        // Stage 1: Category scrape
        if (fromStage == "scratch")
        {
            Console.WriteLine("Stage 1: Scraping category pages...");
            var catScraper = new WikiCategoryScraper(http);
            categoryItems = await catScraper.ScrapeAllAsync();
            await File.WriteAllTextAsync(catFile, JsonSerializer.Serialize(
                new CategoryItemsFile { Items = categoryItems }, JsonOpts));
            Console.WriteLine($"  Found {categoryItems.Count} items.");
        }
        else
        {
            Console.WriteLine($"Stage 1: Loading from {catFile}...");
            var json = await File.ReadAllTextAsync(catFile);
            categoryItems = JsonSerializer.Deserialize<CategoryItemsFile>(json)?.Items
                           ?? new List<CategoryItem>();
        }

        // Stage 2: CSV enrichment
        if (fromStage is "scratch" or "categories")
        {
            Console.WriteLine("Stage 2: Enriching from CSV data...");
            var csvProvider = new CsvDataProvider(http, Path.Combine(cacheDir, "csv"));
            await csvProvider.InitializeAsync();
            var enricher = new CsvEnricher(csvProvider, cacheDir);
            detailItems = enricher.Enrich(categoryItems);
            await File.WriteAllTextAsync(detailFile, JsonSerializer.Serialize(
                new DetailItemsFile { Items = detailItems }, JsonOpts));
            Console.WriteLine($"  Produced {detailItems.Count} detail items.");
        }
        else
        {
            Console.WriteLine($"Stage 2: Loading from {detailFile}...");
            var json = await File.ReadAllTextAsync(detailFile);
            detailItems = JsonSerializer.Deserialize<DetailItemsFile>(json)?.Items
                         ?? new List<DetailItem>();
        }

        var detailFileWritten = fromStage is "scratch" or "categories";

        // Stage 2.5: Resolve unlock quest chains
        if (fromStage is "scratch" or "categories")
        {
            Console.WriteLine("Stage 2.5: Resolving unlock quest chains...");
            var overridePath = Path.Combine("..", "DataBuilder", "Data", "quest_chain_overrides.json");
            if (!File.Exists(overridePath))
            {
                overridePath = Path.Combine("DataBuilder", "Data", "quest_chain_overrides.json");
                if (!File.Exists(overridePath))
                    Console.Error.WriteLine("  WARN: quest_chain_overrides.json not found");
            }

            var csvProvider = new CsvDataProvider(http, Path.Combine(cacheDir, "csv"));
            await csvProvider.InitializeAsync();

            var resolver = new UnlockQuestResolver(overridePath);

            await resolver.ResolveWithWikiAsync(detailItems, csvProvider, http);

            var newQuestItems = resolver.ResolveWithChainCreation(detailItems, csvProvider);
            Console.WriteLine($"  Created {newQuestItems.Count} quest chain entries.");

            if (detailFileWritten)
            {
                await File.WriteAllTextAsync(detailFile, JsonSerializer.Serialize(
                    new DetailItemsFile { Items = detailItems }, JsonOpts));
            }
        }

        // Stage 3: Wiki detail scrape for location data on all items, plus level for unmatched items
        if (fromStage is "scratch" or "categories" or "details" or "wiki-detail")
        {
            var catItems = detailItems
                .Select(i => new CategoryItem
                {
                    Name = i.Name,
                    Category = i.Category,
                    Expansion = i.Expansion
                })
                .ToList();

            Console.WriteLine($"Stage 3: Scraping wiki details for {catItems.Count} items...");
            var detailScraper = new WikiDetailScraper(http);
            var scrapedItems = await detailScraper.ScrapeDetailsAsync(catItems);
            Console.WriteLine($"  Scraped {scrapedItems.Count} items.");

            var scrapedByName = scrapedItems.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var item in detailItems)
            {
                if (!scrapedByName.TryGetValue(item.Name, out var scraped))
                    continue;

                // Merge level if missing
                if ((item.Level == null || item.Level == 0) && scraped.Level > 0)
                    item.Level = scraped.Level;

                // Merge location data — wiki data is more reliable than CSV PlaceName IDs
                if (scraped.LocationTerritoryName != null)
                    item.LocationTerritoryName = scraped.LocationTerritoryName;
                if (scraped.LocationMapX != null)
                    item.LocationMapX = scraped.LocationMapX;
                if (scraped.LocationMapY != null)
                    item.LocationMapY = scraped.LocationMapY;

                // Clear the wrong PlaceName-based territory ID; runtime resolves from name
                item.LocationTerritoryId = null;
            }

            if (detailFileWritten)
            {
                await File.WriteAllTextAsync(resolvedFile, JsonSerializer.Serialize(
                    new DetailItemsFile { Items = detailItems }, JsonOpts));
            }
        }
        else
        {
            Console.WriteLine($"Stage 3: Loading from {resolvedFile}...");
            var json = await File.ReadAllTextAsync(resolvedFile);
            detailItems = JsonSerializer.Deserialize<DetailItemsFile>(json)?.Items
                         ?? new List<DetailItem>();
        }

        // Stage 4: Format and output
        Console.WriteLine("Stage 4: Formatting content.json...");
        var formatted = ContentJsonFormatter.Format(detailItems);
        var outputJson = JsonSerializer.Serialize(formatted, JsonOpts);
        await File.WriteAllTextAsync(outputPath, outputJson);

        Console.WriteLine($"Done! {formatted.Items.Count} items written to {outputPath}");
        return 0;
    }
}
