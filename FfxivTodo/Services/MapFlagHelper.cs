using System;
using System.Collections.Generic;
using System.Numerics;
using FfxivTodo.Models;
using Lumina.Excel.Sheets;

namespace FfxivTodo.Services;

public sealed class MapFlagHelper
{
    public ContentItem GetFlagTarget(ContentItem item, IReadOnlyList<ContentItem> questChain, Func<uint, ItemStatus> getQuestStatus)
    {
        foreach (var quest in questChain)
        {
            if (getQuestStatus(quest.Id) == ItemStatus.NotStarted && HasMapCoords(quest))
                return quest;
        }

        foreach (var quest in questChain)
        {
            if (getQuestStatus(quest.Id) != ItemStatus.Completed && HasMapCoords(quest))
                return quest;
        }

        return item;
    }

    public void PlaceFlag(ContentItem item)
    {
        Plugin.Log.Debug($"[MapFlag] PlaceFlag: {item.Name}");

        if (!item.LocationMapX.HasValue || !item.LocationMapY.HasValue)
        {
            Plugin.Log.Debug($"[MapFlag] No map coords for {item.Name}");
            return;
        }

        Plugin.Log.Debug($"[MapFlag] Coords: X={item.LocationMapX}, Y={item.LocationMapY}, Name={item.LocationTerritoryName}");

        var territoryId = ResolveTerritoryId(item);
        if (territoryId == null)
        {
            Plugin.Log.Debug($"[MapFlag] Territory not resolved for {item.Name} (name={item.LocationTerritoryName})");
            return;
        }

        Plugin.Log.Debug($"[MapFlag] Resolved territory: {territoryId}");

        var map = GetMapForTerritory(territoryId.Value);
        if (map == null)
        {
            Plugin.Log.Debug($"[MapFlag] No map for territory {territoryId}");
            return;
        }

        Plugin.Log.Debug($"[MapFlag] Map: row={map.Value.RowId}, sizeFactor={map.Value.SizeFactor}, offsetX={map.Value.OffsetX}, offsetY={map.Value.OffsetY}");

        var worldPos = MapToWorld(
            map.Value,
            item.LocationMapX.Value,
            item.LocationMapY.Value
        );

        Plugin.Log.Debug($"[MapFlag] World pos: ({worldPos.X}, {worldPos.Y}, {worldPos.Z})");

        var result = Plugin.GameGui.OpenMapWithMapLink(
            territoryId.Value,
            map.Value.RowId,
            worldPos
        );

        Plugin.Log.Debug($"[MapFlag] OpenMapWithMapLink result: {result}");
    }

    private static uint? ResolveTerritoryId(ContentItem item)
    {
        if (item.LocationTerritoryId.HasValue && item.LocationTerritoryId.Value != 0)
            return item.LocationTerritoryId;

        if (string.IsNullOrEmpty(item.LocationTerritoryName))
            return null;

        return FindTerritoryByName(item.LocationTerritoryName);
    }

    private static uint? FindTerritoryByName(string name)
    {
        var territorySheet = Plugin.DataManager.GameData.GetExcelSheet<TerritoryType>();
        if (territorySheet == null)
        {
            Plugin.Log.Debug($"[MapFlag] TerritoryType sheet is null");
            return null;
        }

        uint? exact = null;
        uint? contains = null;

        foreach (var territory in territorySheet)
        {
            var placeName = territory.PlaceName.ValueNullable;
            if (placeName == null) continue;

            var placeNameStr = placeName.Value.Name.ExtractText();

            if (placeNameStr.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                Plugin.Log.Debug($"[MapFlag] Exact match: name='{name}' == placeName='{placeNameStr}' -> territory={territory.RowId}");
                exact ??= territory.RowId;
            }
            else if (placeNameStr.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                Plugin.Log.Debug($"[MapFlag] Contains match: name='{name}' in placeName='{placeNameStr}' -> territory={territory.RowId}");
                contains ??= territory.RowId;
            }
        }

        var result = exact ?? contains;
        Plugin.Log.Debug($"[MapFlag] FindTerritoryByName('{name}') -> {result}");
        return result;
    }

    private static Map? GetMapForTerritory(uint territoryId)
    {
        var mapSheet = Plugin.DataManager.GameData.GetExcelSheet<Map>();
        if (mapSheet == null)
            return null;

        foreach (var map in mapSheet)
        {
            if (map.TerritoryType.RowId == territoryId)
                return map;
        }

        return null;
    }

    private static Vector3 MapToWorld(Map map, float mapX, float mapY)
    {
        var c = map.SizeFactor / 100f;

        var worldX = (mapX - 1f) * 2048f / 41f - 1024f / c - map.OffsetX;
        var worldZ = (mapY - 1f) * 2048f / 41f - 1024f / c - map.OffsetY;

        return new Vector3(worldX, 0f, worldZ);
    }

    private static bool HasMapCoords(ContentItem item) =>
        (item.LocationTerritoryId.HasValue && item.LocationTerritoryId.Value != 0) ||
        (item.LocationMapX.HasValue && !string.IsNullOrEmpty(item.LocationTerritoryName));
}
