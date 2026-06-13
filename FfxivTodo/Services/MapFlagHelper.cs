using FfxivTodo.Models;

namespace FfxivTodo.Services;

public sealed class MapFlagHelper
{
    public void PlaceFlag(ContentItem item)
    {
        if (!item.LocationTerritoryId.HasValue || item.LocationTerritoryId == 0)
            return;
        if (!item.LocationMapX.HasValue || !item.LocationMapY.HasValue)
            return;

        Plugin.GameGui.OpenMapWithMapLink(
            item.LocationTerritoryId.Value,
            item.LocationMapX.Value,
            item.LocationMapY.Value
        );
    }
}