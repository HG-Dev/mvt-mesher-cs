using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MvtMesherCore.Mapbox;

/// <summary>
/// Individual layer found on a vector tile.
/// Defined as a child class of VectorTile, it depends on memory coming from VectorTile.
/// Corresponds to https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L50-L73
/// </summary>
[DebuggerDisplay("Layer {Name}")]
public class VectorTileLayer : IReadOnlyDictionary<ulong, VectorTileFeature>
{
    const uint DefaultExtent = 4096;
    internal static class PbfTags
    {
        internal const uint Name = (1 << 3 | (byte)WireType.Len);
        ///<seealso href="https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#60">Schema on GitHub</seealso>
        internal const uint Features = (2 << 3 | (byte)WireType.Len);
        ///<seealso href="https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#63">Schema on GitHub</seealso>
        internal const uint Keys = (3 << 3 | (byte)WireType.Len);
        ///<seealso href="https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#65">Schema on GitHub</seealso>
        internal const uint Values = (4 << 3 | (byte)WireType.Len);
        internal const uint Extent = (5 << 3 | (byte)WireType.Fixed32);
        internal const uint Version = (15 << 3 | (byte)WireType.Fixed32);
    
        internal static readonly Dictionary<string, PbfTag> Dictionary = new()
        {
            { "Name", (Name) },
            { "Features",  (Features) },
            { "Keys", (Keys) },
            { "PropertyValues", (Values) },
            { "Extent", (Extent) },
            { "Version", (Version) }
        };
        
        internal static readonly HashSet<int> ValidFieldNumbers = [..Dictionary.Values.Select(tag => tag.FieldNumber)];
    }

    readonly VectorTile _parent;
    public VectorTile ParentTile => _parent;
    
    readonly ReadOnlyMemory<byte> _layerData;
    /// <summary>Unique name of this layer</summary>
    /// <seealso href="https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L57">Schema on GitHub</seealso>
    public readonly string Name;
    /// <summary>True if a name was not found for this layer in the PBF file</summary>
    public readonly bool IsUnnamedInPbf;
    /// <summary>Index of this layer amongst other layers in the vector tile's collection.</summary>
    public readonly int Index;
    /// <seealso href="https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L55">Schema on GitHub</seealso>
    public readonly int Version;

    uint? _extent;

    /// <summary>
    /// Describes the width and height of the tile in integer coordinates.
    /// The geometries within the Vector Tile may extend past the bounds of the tile's area as defined by the extent.
    /// Defaults to 4096.
    /// </summary>
    /// <remarks>
    /// Geometries that extend past the tile's area as defined by extent are often used as a buffer for rendering features that overlap multiple adjacent tiles.
    /// If a tile has an extent of 4096, coordinate units within the tile refer to 1/4096th of its square dimensions.
    /// A point at (-1,10) or (4097,10) would be outside the extent of such a tile.
    /// </remarks>
    /// <seealso href="https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L70">Schema on GitHub</seealso> 
    public uint Extent => _extent ??= GetExtent(_layerData.Span, DefaultExtent);

    List<string>? _keys;
    /// <summary>
    /// Property names referenced by features of this layer.
    /// </summary>
    /// <remarks>
    /// All strings will likely be required if a feature's dictionary interface is used even once.
    /// TODO: Figure out how to prevent early evaluation upon the creation of feature's IndexedReadOnlyDictionary
    /// </remarks>
    public IReadOnlyList<string> PropertyNames =>
        _keys ??= PbfSpan.ReadAllStringsWithTag(_layerData.Span, PbfTags.Keys);


    List<PropertyValue>? _values;

    /// <summary>
    /// Property values referenced by features of this layer.
    /// </summary>
    /// TODO: Read from bytes lazily so that only required values are obtained
    public IReadOnlyList<PropertyValue> PropertyValues =>
        _values ??= PbfMemoryUtility.EnumeratePropertyValuesWithTag(_layerData, PbfTags.Values).ToList();
    
    readonly TileFeatureCollection _features;
    /// <summary>
    /// Feature objects found on this layer.
    /// </summary>
    public IReadOnlyDictionary<ulong, VectorTileFeature> Features => _features;
    
    /// <summary>
    /// Construct a VectorTileLayer from a slice of vector tile data.
    /// </summary>
    /// <param name="layerData">Vector tile data slice</param>
    /// <param name="parent">Tile on which this layer is found</param>
    /// <param name="layerIndex">Index of this layer as discovered in tile data</param>
    /// <returns></returns>
    /// <exception cref="PbfValidationFailure">Version field is not <see cref="VectorTile.ProtobufSchemaVersion"/></exception>
    /// <exception cref="PbfValidationFailure">'validation' requires all tags to be of known field numbers, but one or more invalid numbers was found</exception>
    public static VectorTileLayer FromBytes(ReadOnlyMemory<byte> layerData, VectorTile parent, int layerIndex)
    {
        // Validate layer version number (schema requires this)
        // https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L50-L73
        int version = PbfSpan.TryFindFirstTag(layerData.Span, PbfTags.Version, out int offset) 
            ? (int)PbfSpan.ReadFixed32(layerData.Span, ref offset) 
            : VectorTile.ValidationLevel.HasFlag(PbfValidation.FeatureVersion) 
                ? -1 
                : VectorTile.ProtobufSchemaVersion;

        switch (version)
        {
            case -1: 
                throw new PbfValidationFailure(PbfValidation.FeatureVersion, $"Feature on layer #{layerIndex} of {parent.TileId} is missing feature version tag");
            case not VectorTile.ProtobufSchemaVersion:
                throw new PbfValidationFailure(PbfValidation.FeatureVersion, $"Feature on layer #{layerIndex} of {parent.TileId} has invalid feature version tag {version}; expected {VectorTile.ProtobufSchemaVersion}");
        }
        
        // Validate all tags if required
        if (VectorTile.ValidationLevel.HasFlag(PbfValidation.Tags) &&
            PbfSpan.FindInvalidTags(layerData.Span, PbfTags.ValidFieldNumbers) is {Count: > 0} invalid)
        {
            throw PbfValidationFailure.FromTags(invalid);
        }

        return new VectorTileLayer(layerData, parent, layerIndex, version);
    }
    
    VectorTileLayer(ReadOnlyMemory<byte> layerData, VectorTile parent, int index, int version)
    {
        _layerData = layerData;
        _parent = parent;
        Index = index;
        Version = version;
        if (!TryGetName(layerData.Span, out Name))
        {
            Name = $"Layer {index}";
            IsUnnamedInPbf = true;
        }

        _features = new (EnumerateFeatures(layerData, this));
    }

    public override string ToString()
    {
        return $"Layer: {Name} in {_parent}";
    }

    static bool TryGetName(ReadOnlySpan<byte> layerSpan, out string name)
    {
        if (PbfSpan.TryFindFirstTag(layerSpan, PbfTags.Name, out int offset))
        {
            name = PbfSpan.ReadString(layerSpan, ref offset);
            return true;
        }

        name = "";
        return false;
    }

    static uint GetExtent(ReadOnlySpan<byte> layerSpan, uint fallback = VectorTile.ProtobufSchemaVersion)
    {
        return PbfSpan.TryFindFirstTag(layerSpan, PbfTags.Extent, out int offset) 
            ? PbfSpan.ReadFixed32(layerSpan, ref offset) 
            : fallback;
    }
    
    /// <summary>
    /// Adds fresh VectorTileFeature objects to a VectorTileLayer's internal list,
    /// ensuring that the VectorTileFeatures have a reference back to the VectorTileLayer for tag dereferencing.
    /// </summary>
    static IEnumerable<VectorTileFeature> EnumerateFeatures(ReadOnlyMemory<byte> layerData, VectorTileLayer parent)
    {
        var featuresFound = 0;
        foreach (var featureMemory in PbfMemory.FindAndEnumerateFieldsWithTag(layerData, PbfTags.Features))
        {
            yield return new VectorTileFeature(featureMemory, parent, featuresFound++);
        }
    }

#region IReadOnlyDictionary<ulong, VectorTileFeature> implementation
    public IEnumerator<KeyValuePair<ulong, VectorTileFeature>> GetEnumerator()
    {
        return Features.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Features).GetEnumerator();
    }

    public int Count => Features.Count;

    public bool ContainsKey(ulong key)
    {
        return Features.ContainsKey(key);
    }

    public bool TryGetValue(ulong key, out VectorTileFeature value)
    {
        return Features.TryGetValue(key, out value);
    }

    public VectorTileFeature this[ulong key] => Features[key];

    public IEnumerable<ulong> Keys => Features.Keys;

    public IEnumerable<VectorTileFeature> Values => Features.Values;
#endregion
}