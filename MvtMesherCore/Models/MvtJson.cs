using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using MvtMesherCore.Collections;
using MvtMesherCore.Mapbox;
using MvtMesherCore.Mapbox.Geometry;
using Newtonsoft.Json;
using Polygon = MvtMesherCore.Collections.Polygon;

namespace MvtMesherCore.Models;

/// <summary>
/// An MVT represented in JSON format for easy inspection and testing.
/// </summary>
public class MvtJson
{
    [JsonProperty("layers")]
    public Dictionary<string, MvtJsonLayer> Layers = new();
    [JsonProperty("tileid")]
    public string TileId = "0/0/0";
}

public class MvtJsonLayer
{
    [JsonProperty("version")]
    public ulong Version;
    [JsonProperty("extent")]
    public ulong Extent;
    [JsonProperty("propertyNames")]
    public List<string> PropertyNames = new();
    [JsonProperty("propertyValues")]
    public List<string> PropertyValues = new();
    [JsonProperty("features")]
    public List<MvtJsonFeature> Features = new();
}

public class MvtJsonFeature : IEquatable<MvtJsonFeature>
{
    [JsonProperty("id")]
    public ulong Id;
    [JsonProperty("type")]
    public byte GeometryType;
    [JsonIgnore]
    public string ParentLayerName = "";
    [JsonIgnore]
    public int FeatureKey => HashCode.Combine(Id, GeometryType, ParentLayerName);
    public override int GetHashCode()
    {
        var hash = FeatureKey;
        unchecked
        {
            foreach (var pt in GeometryPoints)
            hash = hash * 31 + pt.GetHashCode();
        }
        return hash;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append($"Feature(Hash={GetHashCode()}");
        if (!string.IsNullOrEmpty(ParentLayerName))
        {
            sb.Append($", ParentLayer={ParentLayerName}");
        }
        sb.Append($", Id={Id}, Type={GeometryType}, Points={GeometryPoints.Count} total, Properties={Properties.Count} total)");
        return sb.ToString();
    }

    public bool Equals(MvtJsonFeature other)
    {
        return other != null && GetHashCode() == other.GetHashCode();
    }

    [JsonProperty("pts")]
    public List<MvtUnscaledJsonPoint> GeometryPoints = new();

    [JsonProperty("props")]
    public Dictionary<string, string> Properties = new();

    public static MvtJsonFeature FromVectorTileFeature(VectorTileFeature feature)
    {
        var mvtJsonFeature = new MvtJsonFeature
        {
            Id = feature.Id,
            GeometryType = (byte)feature.GeometryType,
            ParentLayerName = feature.ParentLayer.Name
        };

        mvtJsonFeature.GeometryPoints = feature.Geometry.EnumerateAllPoints()
            .Select(MvtUnscaledJsonPoint.FromVector2).ToList();

        if (feature.Id == 1915597462)
        {
            var geo = ((PolygonGeometry)feature.Geometry);
            Console.Out.WriteLine($"Feature {feature} has {mvtJsonFeature.GeometryPoints.Count} points, {geo.Polygons.Count} polys "+
                                    $"and {geo.Polygons.Sum(p => p.AllRings.Count)} rings");
            Console.Out.WriteLine("FT points: " + string.Join(", ", feature.Geometry.EnumerateAllPoints().Select(p => $"({p.X}, {p.Y})")));
            Console.Out.WriteLine("JSON points: " + string.Join(", ", mvtJsonFeature.GeometryPoints.Select(p => p.ToString())));
        }

        foreach (var (key, value) in feature.Properties)
        {
            mvtJsonFeature.Properties[key] = value.ToShortString();
        }

        return mvtJsonFeature;
    }
}

public struct MvtUnscaledJsonPoint
{
    [JsonProperty("x")]
    public long X;
    [JsonProperty("y")]
    public long Y;

    public static implicit operator MvtUnscaledJsonPoint((long x, long y) tuple)
    {
        return new MvtUnscaledJsonPoint { X = tuple.x, Y = tuple.y };
    }

    public static MvtUnscaledJsonPoint FromVector2(Vector2 vec2)
    {
        return new MvtUnscaledJsonPoint
        {
            X = (long)MathF.Round(vec2.X),
            Y = (long)MathF.Round(vec2.Y)
        };
    }

    public static List<MvtUnscaledJsonPoint> ListFromVector2(IEnumerable<Vector2> vec2List)
    {
        return vec2List.Select(FromVector2).ToList();
    }

    // public static List<List<MvtUnscaledJsonPoint>> ListFromPolygonRings(ReadOnlyPolygons polygons)
    // {
    //     var output = new List<List<MvtUnscaledJsonPoint>>();
    //     foreach (var poly in polygons)
    //     {
    //         foreach (var ring in poly.AllRings)
    //         {
    //             output.Add(ListFromVector2(ring));
    //         }
    //     }
    //     return output;
    // }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}