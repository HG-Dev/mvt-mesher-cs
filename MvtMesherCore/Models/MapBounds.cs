using Newtonsoft.Json;

namespace MvtMesherCore.Models;

public record MapBounds(decimal MinLatitude, decimal MinLongitude, decimal MaxLatitude, decimal MaxLongitude)
{
    [JsonConstructor]
    public MapBounds(decimal[] bounds) : this(bounds[0], bounds[1], bounds[2], bounds[3])
    { }
}