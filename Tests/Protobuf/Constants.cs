using System.Text.RegularExpressions;
using MvtMesherCore.Mapbox;

namespace Tests.Protobuf;

public static class Constants
{
    public const string TestOutputFolder = "TestOutput";
    public const string TestInputFolder = "Res";

    public const string AtlanticPbfFile = "6-25-24_atlantic.pbf";
    public const string AtlanticExpectedFirstDecodedUnscaledPoint = "7336,2809";
    public const string AtlanticExpectedLayers = "park,place,water,water_name";
    public const string AtlanticJsonFile = "6-25-24_atlantic.json";
    public const string AtlanticPolygonBytesFile = "6-25-24_f75124043-3_gc.bytes";
    public const string EnoshimaPbfFile = "14-14540-6473_enoshima.pbf";
    public const string EnoshimaExpectedLayers = "boundary,building,landcover,landuse,mountain_peak,place," +
                                                 "poi,transportation,transportation_name,water,water_name";
    public const string EnoshimaJsonFile = "14-14540-6473_enoshima.json";
    public const string EnoshimaPolylineBytesFile = "14-14540-6473_f1916566182-2_gc.bytes";
    public const string EnoshimaPolygonBytesFile = "14-14540-6473_f1915597462-3_gc.bytes";

    public static readonly Regex LabelRegex = new Regex("name|label", RegexOptions.IgnoreCase);

    public static readonly VectorTile.ReadSettings ReadSettings = new VectorTile.ReadSettings()
    {
        ScaleToLayerExtents = false,
        ValidationLevel = PbfValidation.All & ~PbfValidation.LayerVersion // OpenFreeMap features lack version number
    };
}
