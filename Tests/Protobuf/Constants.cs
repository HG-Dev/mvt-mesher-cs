using MvtMesherCore.Mapbox;

namespace Tests.Protobuf;

public static class Constants
{
    public const string AtlanticPbfPath = "Res/6-25-24_atlantic.pbf";
    public const string AtlanticExpectedFirstDecodedUnscaledPoint = "7336,2809";
    public const string AtlanticExpectedLayers = "park,place,water,water_name";
    public const string AtlanticJsonPath = "Res/6-25-24_atlantic.json";
    public const string AtlanticPolygonBytesPath = "Res/6-25-24_f75124043-3_gc.bytes";
    public const string EnoshimaPbfPath = "Res/14-14540-6473_enoshima.pbf";
    public const string EnoshimaExpectedLayers = "boundary,building,landcover,landuse,mountain_peak,place," +
                                                 "poi,transportation,transportation_name,water,water_name";
    public const string EnoshimaJsonPath = "Res/14-14540-6473_enoshima.json";
    public const string EnoshimaPolylineBytesPath = "Res/14-14540-6473_f1916566182-2_gc.bytes";
    public const string EnoshimaPolygonBytesPath = "Res/14-14540-6473_f1915597462-3_gc.bytes";

    public static readonly VectorTile.ReadSettings ReadSettings = new VectorTile.ReadSettings()
    {
        ScaleToLayerExtents = false,
        ValidationLevel = PbfValidation.All & ~PbfValidation.LayerVersion // OpenFreeMap features lack version number
    };
}
