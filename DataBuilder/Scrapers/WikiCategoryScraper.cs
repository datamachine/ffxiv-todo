using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using DataBuilder.Models;
using HtmlAgilityPack;

namespace DataBuilder.Scrapers;

public sealed class WikiCategoryScraper
{
    private readonly HttpClient _http;

    private static readonly Dictionary<string, string> ExpansionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a realm reborn"] = "ARR",
        ["heavensward"] = "HW",
        ["stormblood"] = "SB",
        ["shadowbringers"] = "ShB",
        ["endwalker"] = "EW",
        ["dawntrail"] = "DT",
    };

    private static readonly Dictionary<string, string> JobExpansionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // ARR base jobs
        ["Paladin"] = "ARR", ["Warrior"] = "ARR", ["Dark Knight"] = "ARR",
        ["Monk"] = "ARR", ["Dragoon"] = "ARR", ["Ninja"] = "ARR",
        ["Bard"] = "ARR", ["Machinist"] = "ARR", ["Dancer"] = "ARR",
        ["White Mage"] = "ARR", ["Scholar"] = "ARR", ["Astrologian"] = "ARR",
        ["Black Mage"] = "ARR", ["Summoner"] = "ARR", ["Red Mage"] = "ARR",
        // Shadowbringers jobs (start at 70-80)
        ["Gunbreaker"] = "ShB", ["Reaper"] = "ShB", ["Sage"] = "ShB",
        // Endwalker/Dawntrail
        ["Viper"] = "EW", ["Pictomancer"] = "EW",
        // Blue Mage
        ["Blue Mage"] = "ARR",
    };

    public WikiCategoryScraper(HttpClient http)
    {
        _http = http;
    }

    private static string GetHeadingId(HtmlNode heading)
    {
        var id = heading.GetAttributeValue("id", "");
        if (!string.IsNullOrEmpty(id))
            return id;
        var span = heading.SelectSingleNode(".//span[@id]");
        return span?.GetAttributeValue("id", "") ?? "";
    }

    private static HtmlNode GetWalkStart(HtmlNode heading)
    {
        // Modern MediaWiki wraps h2/h3/h4 in <div class="mw-heading mw-headingN">
        var parent = heading.ParentNode;
        if (parent?.Name == "div")
        {
            var cls = parent.GetAttributeValue("class", "");
            if (cls.Contains("mw-heading"))
                return parent;
        }
        return heading;
    }

    private static HtmlNode? FindNextTable(HtmlNode heading)
    {
        var current = GetWalkStart(heading);
        while ((current = current.NextSibling) != null)
        {
            if (current.Name == "table") return current;
            if (current.Name is "h2" or "h3" or "h4") break;
        }
        return null;
    }

    public static string? ParseExpansionFromHeading(string heading)
    {
        return ExpansionMap.TryGetValue(heading, out var value) ? value : null;
    }

    private static string GetJobNameFromHeading(string headingId)
    {
        // heading IDs are like "Paladin_Quests", "Warrior_Quests", etc.
        if (!headingId.EndsWith("_Quests", StringComparison.OrdinalIgnoreCase))
            return headingId;
        return headingId[..^7].Replace("_", " ");
    }

    public List<CategoryItem> ParseJobQuestTable(HtmlNode contentNode)
    {
        var items = new List<CategoryItem>();

        var h4Tags = contentNode.SelectNodes(".//h4");
        if (h4Tags == null) return items;

        foreach (var h4 in h4Tags)
        {
            var h4Id = GetHeadingId(h4);
            if (string.IsNullOrEmpty(h4Id)) continue;

            var jobName = GetJobNameFromHeading(h4Id);
            var expShort = JobExpansionMap.GetValueOrDefault(jobName, "ARR");

            var table = FindNextTable(h4);
            if (table == null) continue;

            var rows = table.SelectNodes(".//tr");
            if (rows == null) continue;

            foreach (var row in rows.Skip(1))
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null || cells.Count < 1) continue;

                var link = cells[0].SelectSingleNode(".//a");
                if (link == null) continue;

                var name = HttpUtility.HtmlDecode(link.InnerText.Trim());
                items.Add(new CategoryItem
                {
                    Name = name,
                    Category = "JobQuest",
                    Expansion = expShort
                });
            }
        }

        return items;
    }

    public List<CategoryItem> ParseRaidsPage(HtmlNode contentNode)
    {
        var items = new List<CategoryItem>();
        var currentSection = string.Empty;
        var currentExpansion = string.Empty;

        var headings = contentNode.SelectNodes(".//h2|.//h3|.//h4");
        if (headings == null) return items;

        foreach (var heading in headings)
        {
            var sectionId = GetHeadingId(heading);
            if (string.IsNullOrEmpty(sectionId)) continue;

            var stripped = Regex.Replace(sectionId, @"_\d+$", "");
            if (stripped != sectionId)
                sectionId = stripped;

            if (sectionId == "Normal_Raids") { currentSection = "RaidSeries"; continue; }
            if (sectionId == "Alliance_Raids") { currentSection = "AllianceRaid"; continue; }
            if (sectionId == "Field_Operations") { currentSection = "FieldOperation"; continue; }
            if (sectionId.StartsWith("Savage_Raids") || sectionId.StartsWith("Ultimate_Raids"))
            { currentSection = string.Empty; continue; }
            if (sectionId == "Chaotic_Alliance_Raids")
            { currentSection = string.Empty; continue; }

            var cleaned = sectionId.Replace("_", " ");
            var exp = ParseExpansionFromHeading(cleaned);
            if (exp != null)
            {
                currentExpansion = exp;
                AddItemsFromNextTable(heading, currentSection, currentExpansion, items);
            }
            // Skip meta sections (not expansion names, not section markers)
            else if (string.IsNullOrEmpty(currentSection)
                     || sectionId.Contains("Participation")
                     || sectionId.Contains("Rewards")
                     || sectionId.Contains("List_of")
                     || sectionId.Contains("Additional_"))
            {
                continue;
            }
        }

        return items;
    }

    private static void AddItemsFromNextTable(
        HtmlNode heading, string section, string expansion, List<CategoryItem> items)
    {
        var table = FindNextTable(heading);
        if (table == null) return;

        var rows = table.SelectNodes(".//tr");
        if (rows == null) return;

        foreach (var row in rows.Skip(1))
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 1) continue;

            var firstCol = cells[0];
            var link = firstCol.SelectSingleNode(".//a");
            var dutyName = link != null
                ? HttpUtility.HtmlDecode(link.InnerText.Trim())
                : HttpUtility.HtmlDecode(firstCol.InnerText.Trim());

            if (string.IsNullOrWhiteSpace(dutyName)) continue;

            items.Add(new CategoryItem
            {
                Name = dutyName,
                Category = section,
                Expansion = expansion
            });
        }
    }

    private static readonly HashSet<string> NonQuestLinkTexts = new(StringComparer.OrdinalIgnoreCase)
    {
        "Notable Rewards", "Rewards",
        "MSQ", "Main Scenario", "Main Scenario Quest",
        "Shadowbringers", "Shadowbringers", "Endwalker", "Dawntrail", "Stormblood", "Heavensward",
        "Disciple of the Hand", "Disciple of the Land",
        "Reputation", "Vendor", "Collectables",
        "Back to top", "Table of Contents",
    };

    private static readonly HashSet<string> NonQuestLinkHrefs = new(StringComparer.OrdinalIgnoreCase)
    {
        "/wiki/Achievements", "/wiki/MSQ", "/wiki/Main_Scenario",
        "/wiki/Patch_", "/wiki/Crafting", "/wiki/Items", "/wiki/Enemies",
        "/wiki/Dye", "/wiki/Collectables", "/wiki/Gathering",
        "/wiki/Shadowbringers", "/wiki/Endwalker", "/wiki/Dawntrail", "/wiki/Stormblood", "/wiki/Heavensward",
        "/wiki/Disciple_of_the_Hand", "/wiki/Disciple_of_the_Land",
    };

    private static bool IsJunkLink(HtmlNode link)
    {
        var text = link.InnerText.Trim();
        if (NonQuestLinkTexts.Contains(text))
            return true;

        var href = link.GetAttributeValue("href", "");
        if (href.Contains("#") || href.Contains("Daily_Quests", StringComparison.OrdinalIgnoreCase))
            return true;
        foreach (var prefix in NonQuestLinkHrefs)
        {
            if (href.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public List<CategoryItem> ParseAlliedSocietyPage(HtmlNode contentNode)
    {
        var items = new List<CategoryItem>();
        var currentExpansion = string.Empty;

        var headings = contentNode.SelectNodes(".//h2|.//h3");
        if (headings == null) return items;

        foreach (var heading in headings)
        {
            var sectionId = GetHeadingId(heading);
            if (string.IsNullOrEmpty(sectionId)) continue;

            var cleaned = Regex.Replace(sectionId.Replace("_", " "), @" (Allied Societies|Daily Quests)$", "");
            var exp = ParseExpansionFromHeading(cleaned);
            if (exp != null)
            {
                currentExpansion = exp;
                continue;
            }

            var para = GetWalkStart(heading).NextSibling;
            while (para != null && para.Name != "p" && para.Name != "h2" && para.Name != "h3" && para.Name != "h4")
                para = para.NextSibling;

            if (para == null || para.Name != "p") continue;

            // Try links inside <b> first (quest names are typically bold)
            var found = false;
            var boldLinks = para.SelectNodes(".//b/a[contains(@href,'/wiki/')]");
            if (boldLinks != null)
            {
                foreach (var link in boldLinks)
                {
                    if (IsJunkLink(link)) continue;
                    var questName = HttpUtility.HtmlDecode(link.InnerText.Trim());
                    if (string.IsNullOrWhiteSpace(questName)) continue;
                    items.Add(new CategoryItem
                    {
                        Name = questName,
                        Category = "BeastTribe",
                        Expansion = currentExpansion
                    });
                    found = true;
                    break;
                }
            }

            // Fallback: first non-junk wiki link in the paragraph
            if (!found)
            {
                var links = para.SelectNodes(".//a[contains(@href,'/wiki/')]");
                if (links != null)
                {
                    foreach (var link in links)
                    {
                        if (IsJunkLink(link)) continue;
                        var questName = HttpUtility.HtmlDecode(link.InnerText.Trim());
                        if (string.IsNullOrWhiteSpace(questName)) continue;
                        items.Add(new CategoryItem
                        {
                            Name = questName,
                            Category = "BeastTribe",
                            Expansion = currentExpansion
                        });
                        break;
                    }
                }
            }
        }

        return items;
    }

    public List<CategoryItem> ParseFeatureQuestsPage(HtmlNode contentNode)
    {
        var items = new List<CategoryItem>();
        var currentCategory = "BlueUnlock";

        var headings = contentNode.SelectNodes(".//h2|.//h3");
        if (headings == null) return items;

        foreach (var heading in headings)
        {
            var sectionId = GetHeadingId(heading);
            if (string.IsNullOrEmpty(sectionId)) continue;

            // Skip TOC heading
            if (sectionId == "mw-toc-heading") continue;

            // Sections that should be skipped entirely (no content type for us)
            if (sectionId.Contains("Class") || sectionId.Contains("Job") || sectionId.Contains("Role"))
            { currentCategory = string.Empty; continue; }

            // Chronicles of a New Era and its sub-sections
            if (sectionId.Contains("Chronicles") || sectionId.Contains("Trials")
                || sectionId.Contains("Normal_Raids") || sectionId.Contains("Alliance_Raids")
                || sectionId.Contains("High-end"))
            { currentCategory = string.Empty; continue; }

            // Records of Unusual Endeavors (Custom Delivery + Variant/Criterion)
            if (sectionId.Contains("Records_of_Unusual"))
            { currentCategory = "BlueUnlock"; continue; }

            // Side Story sections (Hildibrand, Relic Weapons, Eureka, Bozja, Occult Crescent)
            // Hildibrand IS a BlueUnlock feature quest, don't skip the h2
            if (sectionId.Contains("Side_Story_Questlines"))
            { currentCategory = "BlueUnlock"; continue; }

            // Other non-feature sections
            if (sectionId.Contains("Aether_Current") || sectionId.Contains("Levequests")
                || sectionId.Contains("Grand_Company") || sectionId.Contains("The_Hunt")
                || sectionId.Contains("Allied_Society") || sectionId.Contains("Locations")
                || sectionId.Contains("Glamour") || sectionId.Contains("Achievement")
                || sectionId.Contains("Seasonal") || sectionId.Contains("Special")
                || sectionId.Contains("Guildhests")
                || sectionId.Contains("Stone,"))
            { currentCategory = string.Empty; continue; }

            // Skip sub-sections of non-feature sections (e.g., GC squad headings)
            if (sectionId.StartsWith("The_", StringComparison.OrdinalIgnoreCase)
                && (sectionId.Contains("Maelstrom") || sectionId.Contains("Flames")
                    || sectionId.Contains("Twin_Adder") || sectionId.Contains("Order_of")))
            { currentCategory = string.Empty; continue; }

            // Other Instances — container section, don't parse its own table
            if (sectionId == "Other_Instances")
            { currentCategory = "BlueUnlock"; continue; }

            currentCategory = "BlueUnlock";

            // Override for specific sections under Other Instances
            if (sectionId == "Dungeons")
                currentCategory = "Dungeon";
            else if (sectionId == "PvP")
                currentCategory = "PvP";

            var table = FindNextTable(heading);
            if (table == null || string.IsNullOrEmpty(currentCategory)) continue;

            var rows = table.SelectNodes(".//tr");
            if (rows == null) continue;

            foreach (var row in rows.Skip(1))
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null || cells.Count < 1) continue;

                var link = cells[0].SelectSingleNode(".//a");
                if (link == null) continue;

                var name = HttpUtility.HtmlDecode(link.InnerText.Trim());
                items.Add(new CategoryItem
                {
                    Name = name,
                    Category = currentCategory,
                    Expansion = "ARR" // placeholder, refined in detail stage
                });
            }
        }

        return items;
    }

    public List<CategoryItem> ParseSideQuestsPage(HtmlNode contentNode)
    {
        var items = new List<CategoryItem>();
        var currentExpansion = string.Empty;

        var headings = contentNode.SelectNodes(".//h2|.//h3");
        if (headings == null) return items;

        foreach (var heading in headings)
        {
            var sectionId = GetHeadingId(heading);
            if (string.IsNullOrEmpty(sectionId)) continue;

            var cleaned = Regex.Replace(sectionId.Replace("_", " "), @" (Quests?)$", "");
            var exp = ParseExpansionFromHeading(cleaned);
            if (exp != null)
            {
                currentExpansion = exp;
                continue;
            }

            // Side quests are listed as links in paragraphs, not tables
            var para = GetWalkStart(heading).NextSibling;
            while (para != null && para.Name != "p" && para.Name != "h2" && para.Name != "h3" && para.Name != "h4")
                para = para.NextSibling;

            if (para == null || para.Name != "p") continue;

            var links = para.SelectNodes(".//a[contains(@href,'/wiki/')]");
            if (links == null) continue;

            foreach (var link in links)
            {
                var questName = HttpUtility.HtmlDecode(link.InnerText.Trim());
                if (string.IsNullOrWhiteSpace(questName)) continue;
                items.Add(new CategoryItem
                {
                    Name = questName,
                    Category = "SideQuest",
                    Expansion = currentExpansion
                });
            }
        }

        return items;
    }

    public List<CategoryItem> ParseDeepDungeonsPage(HtmlNode contentNode)
    {
        var items = new List<CategoryItem>();

        var ul = contentNode.SelectSingleNode(".//h2/span[@id='List_of_deep_dungeons']/../../following-sibling::ul[1]");
        if (ul == null)
        {
            // Fallback: find any ul with deep dungeon links
            ul = contentNode.SelectSingleNode(".//ul[li/a[contains(@href,'/wiki/Palace_of_the_Dead')]]");
        }

        if (ul == null) return items;

        var links = ul.SelectNodes(".//li/a[contains(@href,'/wiki/')]");
        if (links == null) return items;

        foreach (var link in links)
        {
            var name = HttpUtility.HtmlDecode(link.InnerText.Trim());
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (name.Contains("the") && name.Length < 5) continue; // skip "the", "a", etc.

            items.Add(new CategoryItem
            {
                Name = name,
                Category = "DeepDungeon",
                Expansion = "ARR"
            });
        }

        return items;
    }

    private static readonly HashSet<string> KnownRoleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Tank", "Physical DPS", "Melee DPS", "Physical Ranged DPS", "Magical Ranged DPS", "Healer", "Master"
    };

    private static readonly string[] ExpansionNamesShort = ["ARR", "HW", "SB", "ShB", "EW", "DT"];

    private static readonly Dictionary<string, string> RoleExpansionFullNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ShB"] = "Shadowbringers",
        ["EW"] = "Endwalker",
        ["DT"] = "Dawntrail",
    };

    public List<CategoryItem> ParseRoleQuestsPage(HtmlNode contentNode)
    {
        var items = new List<CategoryItem>();
        var currentExpansion = string.Empty;

        var headings = contentNode.SelectNodes(".//h2|.//h3");
        if (headings == null) return items;

        foreach (var heading in headings)
        {
            var sectionId = GetHeadingId(heading);
            if (string.IsNullOrEmpty(sectionId)) continue;

            // Try as expansion heading first (h2)
            var cleaned = Regex.Replace(Regex.Replace(sectionId, @"_\d+$", "").Replace("_", " "), @"^\d+\s+", "");
            var exp = ParseExpansionFromHeading(cleaned);
            if (exp != null)
            {
                currentExpansion = exp;
                continue;
            }

            // Strip MediaWiki duplicate heading suffix (_2, _3, etc.)
            var cleanSectionId = Regex.Replace(sectionId, @"_\d+$", "");

            // Determine role name from h3 heading
            var role = cleanSectionId.Equals("Master_Role_Quests", StringComparison.OrdinalIgnoreCase)
                ? "Master" : cleanSectionId.Replace("_", " ");

            // Skip non-role headings (Contents, Trivia, References, etc.)
            if (!KnownRoleNames.Contains(role)) continue;

            // Produce one content item per role chain
            var expansionFull = RoleExpansionFullNames.GetValueOrDefault(currentExpansion, currentExpansion);
            items.Add(new CategoryItem
            {
                Name = $"{expansionFull} {role} Role Quests",
                Category = "RoleQuest",
                Expansion = currentExpansion
            });
        }

        return items;
    }

    public List<CategoryItem> ParseVariantDungeonsPage(HtmlNode contentNode)
    {
        var items = new List<CategoryItem>();
        var inVariantSection = false;
        var inCriterionSection = false;

        var headings = contentNode.SelectNodes(".//h2|.//h3");
        if (headings == null) return items;

        foreach (var heading in headings)
        {
            var sectionId = GetHeadingId(heading);
            if (string.IsNullOrEmpty(sectionId)) continue;

            if (sectionId == "Variant_Dungeons")
            {
                inVariantSection = true;
                inCriterionSection = false;
                continue;
            }
            if (sectionId == "Criterion_Dungeons")
            {
                inVariantSection = false;
                inCriterionSection = true;
                continue;
            }
            if (sectionId.Contains("Advanced") || sectionId.Contains("Savage"))
            {
                inVariantSection = false;
                inCriterionSection = false;
                continue;
            }

            if (!inVariantSection && !inCriterionSection) continue;

            if (sectionId == "Available_Variant_Dungeons" || sectionId == "Available_Criterion_Dungeons")
            {
                var table = FindNextTable(heading);
                if (table == null) continue;

                var rows = table.SelectNodes(".//tr");
                if (rows == null) continue;

                foreach (var row in rows.Skip(1))
                {
                    var cells = row.SelectNodes(".//td");
                    if (cells == null || cells.Count < 1) continue;
                    var link = cells[0].SelectSingleNode(".//a");
                    if (link == null) continue;
                    var name = HttpUtility.HtmlDecode(link.InnerText.Trim());
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var exp = name.Contains("Merchant") ? "DT" : "EW";

                    items.Add(new CategoryItem
                    {
                        Name = name,
                        Category = "VariantDungeon",
                        Expansion = exp
                    });
                }
            }
        }

        return items;
    }

    public List<CategoryItem> ParseIslandSanctuaryPage(HtmlNode contentNode)
    {
        var items = new List<CategoryItem>();

        var headings = contentNode.SelectNodes(".//h2|.//h3");
        if (headings != null)
        {
            foreach (var heading in headings)
            {
                var sectionId = GetHeadingId(heading);
                if (sectionId != "Questline") continue;

                var table = FindNextTable(heading);
                if (table == null) continue;

                var rows = table.SelectNodes(".//tr");
                if (rows == null) continue;

                foreach (var row in rows.Skip(1))
                {
                    var cells = row.SelectNodes(".//td");
                    if (cells == null || cells.Count < 1) continue;
                    var link = cells[0].SelectSingleNode(".//a");
                    if (link == null) continue;
                    var name = HttpUtility.HtmlDecode(link.InnerText.Trim());
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        items.Add(new CategoryItem
                        {
                            Name = name,
                            Category = "BlueUnlock",
                            Expansion = "EW"
                        });
                    }
                }
            }
        }

        items.Add(new CategoryItem
        {
            Name = "Island Sanctuary",
            Category = "IslandSanctuary",
            Expansion = "EW"
        });

        return items;
    }

    public List<CategoryItem> ParseFauxHollowsPage(HtmlNode contentNode)
    {
        return new List<CategoryItem>
        {
            new()
            {
                Name = "Faux Hollows",
                Category = "FauxHollows",
                Expansion = "ShB"
            }
        };
    }

    public List<CategoryItem> ParseMaskedCarnivalePage(HtmlNode contentNode)
    {
        return new List<CategoryItem>
        {
            new()
            {
                Name = "The Masked Carnivale",
                Category = "MaskedCarnivale",
                Expansion = "SB"
            }
        };
    }

    public List<CategoryItem> ParseIshgardianRestorationPage(HtmlNode contentNode)
    {
        return new List<CategoryItem>
        {
            new()
            {
                Name = "Ishgardian Restoration",
                Category = "IshgardianRestoration",
                Expansion = "ShB"
            }
        };
    }

    public List<CategoryItem> ParseGoldSaucerPage(HtmlNode contentNode)
    {
        return new List<CategoryItem>
        {
            new() { Name = "The Gold Saucer", Category = "GoldSaucer", Expansion = "ARR" }
        };
    }

    public List<CategoryItem> ParseTreasureHuntPage(HtmlNode contentNode)
    {
        return new List<CategoryItem>
        {
            new() { Name = "Treasure Hunts", Category = "TreasureHunt", Expansion = "ARR" }
        };
    }

    public List<CategoryItem> ParseChocoboPage(HtmlNode contentNode)
    {
        return new List<CategoryItem>
        {
            new() { Name = "Companion Chocobo", Category = "Chocobo", Expansion = "ARR" }
        };
    }

    private static List<CategoryItem> GetStaticRelicWeapons()
    {
        return new List<CategoryItem>
        {
            new() { Name = "Zodiac Weapons",      Category = "RelicWeapon", Expansion = "ARR" },
            new() { Name = "Anima Weapons",       Category = "RelicWeapon", Expansion = "HW"  },
            new() { Name = "Eureka Weapons",      Category = "RelicWeapon", Expansion = "SB"  },
            new() { Name = "Resistance Weapons",  Category = "RelicWeapon", Expansion = "ShB" },
            new() { Name = "Skysteel Tools",      Category = "RelicWeapon", Expansion = "ShB" },
            new() { Name = "Manderville Weapons", Category = "RelicWeapon", Expansion = "EW"  },
            new() { Name = "Phantom Weapons",     Category = "RelicWeapon", Expansion = "DT"  },
        };
    }

    public async Task<List<CategoryItem>> ScrapeCustomDeliveriesAsync()
    {
        var items = new List<CategoryItem>();

        var customPage = await FetchPageAsync("/wiki/Custom_Deliveries");
        var urls = GetCustomDeliverySubPageUrls(customPage);

        foreach (var urlItem in urls)
        {
            var subPage = await FetchPageAsync(urlItem.Name);
            var subItems = ParseCustomDeliverySubPage(subPage);
            items.AddRange(subItems);
        }

        return items;
    }

    private async Task<HtmlNode> FetchPageAsync(string path)
    {
        var url = $"{WikiBase}{path}";
        var html = await _http.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc.DocumentNode;
    }

    public List<CategoryItem> GetCustomDeliverySubPageUrls(HtmlNode contentNode)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var items = new List<CategoryItem>();
        var links = contentNode.SelectNodes(
            ".//a[contains(@href,'/wiki/Custom_Deliveries_(')]");
        if (links == null) return items;

        foreach (var link in links)
        {
            var href = link.GetAttributeValue("href", "");
            if (string.IsNullOrWhiteSpace(href)) continue;
            if (seen.Add(href))
                items.Add(new CategoryItem { Name = href });
        }
        return items;
    }

    public List<CategoryItem> ParseCustomDeliverySubPage(HtmlNode contentNode)
    {
        var items = new List<CategoryItem>();
        var table = contentNode.SelectSingleNode(
            ".//table[contains(@class,'quest')]");
        if (table == null) return items;

        var rows = table.SelectNodes(".//tr");
        if (rows == null) return items;

        foreach (var row in rows.Skip(1))
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 1) continue;

            var link = cells[0].SelectSingleNode(".//a[contains(@href,'/wiki/')]");
            if (link == null) continue;

            var name = HttpUtility.HtmlDecode(link.InnerText.Trim());
            if (string.IsNullOrWhiteSpace(name)) continue;

            items.Add(new CategoryItem
            {
                Name = name,
                Category = "CustomDelivery",
                Expansion = "ARR" // placeholder, refined from level in detail stage
            });
        }
        return items;
    }

    private static readonly string WikiBase = "https://ffxiv.consolegameswiki.com";

    public async Task<List<CategoryItem>> ScrapeAllAsync()
    {
        var allItems = new List<CategoryItem>();

        // Job Quests
        var jobItems = await FetchAndParseAsync("/wiki/Job_Quests", doc =>
            ParseJobQuestTable(doc.DocumentNode));
        allItems.AddRange(jobItems);

        // Raids (Normal + Alliance)
        var raidItems = await FetchAndParseAsync("/wiki/Raids", doc =>
            ParseRaidsPage(doc.DocumentNode));
        allItems.AddRange(raidItems);

        // Beast Tribes
        var beastItems = await FetchAndParseAsync("/wiki/Allied_Society_Quests", doc =>
            ParseAlliedSocietyPage(doc.DocumentNode));
        allItems.AddRange(beastItems);

        // Feature Quests
        var blueItems = await FetchAndParseAsync("/wiki/Feature_Quests", doc =>
            ParseFeatureQuestsPage(doc.DocumentNode));
        allItems.AddRange(blueItems);

        // Side Quests
        var sideItems = await FetchAndParseAsync("/wiki/Side_Quests", doc =>
            ParseSideQuestsPage(doc.DocumentNode));
        allItems.AddRange(sideItems);

        // Deep Dungeons
        var deepItems = await FetchAndParseAsync("/wiki/Deep_Dungeons", doc =>
            ParseDeepDungeonsPage(doc.DocumentNode));
        allItems.AddRange(deepItems);

        // Custom Deliveries
        try
        {
            var customItems = await ScrapeCustomDeliveriesAsync();
            allItems.AddRange(customItems);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"WARN: Failed to scrape custom deliveries: {ex.Message}");
        }

        // Role Quests
        try
        {
            var roleItems = await FetchAndParseAsync("/wiki/Role_Quests", doc =>
                ParseRoleQuestsPage(doc.DocumentNode));
            allItems.AddRange(roleItems);
        }
        catch (Exception ex) { Console.Error.WriteLine($"WARN: Failed to scrape Role Quests: {ex.Message}"); }

        // Variant & Criterion Dungeons
        try
        {
            var variantItems = await FetchAndParseAsync("/wiki/Variant_and_Criterion_Dungeons", doc =>
                ParseVariantDungeonsPage(doc.DocumentNode));
            allItems.AddRange(variantItems);
        }
        catch (Exception ex) { Console.Error.WriteLine($"WARN: Failed to scrape Variant Dungeons: {ex.Message}"); }

        // Island Sanctuary
        try
        {
            var islandItems = await FetchAndParseAsync("/wiki/Island_Sanctuary", doc =>
                ParseIslandSanctuaryPage(doc.DocumentNode));
            allItems.AddRange(islandItems);
        }
        catch (Exception ex) { Console.Error.WriteLine($"WARN: Failed to scrape Island Sanctuary: {ex.Message}"); }

        // Faux Hollows
        try
        {
            var fauxItems = await FetchAndParseAsync("/wiki/Faux_Hollows", doc =>
                ParseFauxHollowsPage(doc.DocumentNode));
            allItems.AddRange(fauxItems);
        }
        catch (Exception ex) { Console.Error.WriteLine($"WARN: Failed to scrape Faux Hollows: {ex.Message}"); }

        // The Masked Carnivale
        try
        {
            var carnItems = await FetchAndParseAsync("/wiki/The_Masked_Carnivale", doc =>
                ParseMaskedCarnivalePage(doc.DocumentNode));
            allItems.AddRange(carnItems);
        }
        catch (Exception ex) { Console.Error.WriteLine($"WARN: Failed to scrape Masked Carnivale: {ex.Message}"); }

        // Ishgardian Restoration
        try
        {
            var restoItems = await FetchAndParseAsync("/wiki/Ishgardian_Restoration", doc =>
                ParseIshgardianRestorationPage(doc.DocumentNode));
            allItems.AddRange(restoItems);
        }
        catch (Exception ex) { Console.Error.WriteLine($"WARN: Failed to scrape Ishgardian Restoration: {ex.Message}"); }

        // Relic Weapons (static)
        allItems.AddRange(GetStaticRelicWeapons());

        // Gold Saucer
        allItems.AddRange(new List<CategoryItem>
        {
            new() { Name = "The Gold Saucer", Category = "GoldSaucer", Expansion = "ARR" }
        });

        // Treasure Hunts
        allItems.AddRange(new List<CategoryItem>
        {
            new() { Name = "Treasure Hunts", Category = "TreasureHunt", Expansion = "ARR" }
        });

        // Companion Chocobo
        allItems.AddRange(new List<CategoryItem>
        {
            new() { Name = "Companion Chocobo", Category = "Chocobo", Expansion = "ARR" }
        });

        return allItems.DistinctBy(i => i.Name).ToList();
    }

    private async Task<List<CategoryItem>> FetchAndParseAsync(
        string path, Func<HtmlDocument, List<CategoryItem>> parser)
    {
        var url = $"{WikiBase}{path}";
        string html;
        try
        {
            html = await _http.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"WARN: Failed to fetch {path}: {ex.Message}");
            return new List<CategoryItem>();
        }
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return parser(doc);
    }
}
