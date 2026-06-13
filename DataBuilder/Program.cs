using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DataBuilder.Formatters;
using DataBuilder.Models;
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
        var outputPath = "../FfxivTodo/Data/content.json";

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--from" && i + 1 < args.Length)
                fromStage = args[++i];
            if (args[i] == "--output" && i + 1 < args.Length)
                outputPath = args[++i];
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

        // Stage 2: Detail scrape
        if (fromStage is "scratch" or "categories")
        {
            Console.WriteLine("Stage 2: Scraping detail pages...");
            var detailScraper = new WikiDetailScraper(http);
            detailItems = await detailScraper.ScrapeDetailsAsync(categoryItems);
            await File.WriteAllTextAsync(detailFile, JsonSerializer.Serialize(
                new DetailItemsFile { Items = detailItems }, JsonOpts));
            Console.WriteLine($"  Scraped {detailItems.Count} detail pages.");
        }
        else
        {
            Console.WriteLine($"Stage 2: Loading from {detailFile}...");
            var json = await File.ReadAllTextAsync(detailFile);
            detailItems = JsonSerializer.Deserialize<DetailItemsFile>(json)?.Items
                         ?? new List<DetailItem>();
        }

        // Stage 3: ID resolution
        if (fromStage is "scratch" or "categories" or "details")
        {
            Console.WriteLine("Stage 3: Resolving IDs via EDB + XIVAPI...");
            var resolver = new XivApiResolver(http);
            foreach (var item in detailItems)
                await resolver.ResolveAsync(item);

            await File.WriteAllTextAsync(resolvedFile, JsonSerializer.Serialize(
                new DetailItemsFile { Items = detailItems }, JsonOpts));
            Console.WriteLine("  IDs resolved.");
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
