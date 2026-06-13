using System.Numerics;
using FfxivTodo.Models;
using Lumina.Excel.Sheets;

namespace FfxivTodo.Services;

public sealed class MapFlagHelper
{
    public void PlaceFlag(ContentItem item)
    {
        if (!item.LocationTerritoryId.HasValue || item.LocationTerritoryId == 0)
            return;
        if (!item.LocationMapX.HasValue || !item.LocationMapY.HasValue)
            return;

        var territoryId = item.LocationTerritoryId.Value;
        var map = GetMapForTerritory(territoryId);
        if (map == null)
            return;

        var worldPos = MapToWorld(
            map.Value,
            item.LocationMapX.Value,
            item.LocationMapY.Value
        );

        Plugin.GameGui.OpenMapWithMapLink(
            territoryId,
            map.Value.RowId,
            worldPos
        );
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
        var scale = map.SizeFactor / 100f;

        var worldX = (mapX / 100f * scale + map.OffsetX) * 1000f;
        var worldY = 0f;
        var worldZ = (mapY / 100f * scale + map.OffsetY) * 1000f;

        return new Vector3(worldX, worldY, worldZ);
    }
}
