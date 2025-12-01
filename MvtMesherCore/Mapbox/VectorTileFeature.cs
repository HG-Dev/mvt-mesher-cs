using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using MvtMesherCore.Mapbox.Geometry;

namespace MvtMesherCore.Mapbox;

/// <summary>
/// Individual feature found on a VectorTile.Layer, encoded in a
/// ReadOnlyMemory byte slice of its parent layer's ReadOnlyMemory.
/// Corresponds to https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L31-L47
/// </summary>
/// <remarks>
/// Feature geometry often extends beyond the bounds of its parent tile,
/// so clipping may be necessary when rendering or meshing.
/// Additionally, features may be duplicated between tiles to ensure
/// continuity and completeness.
/// </remarks>
[DebuggerDisplay("Feature {Id}")]
public class VectorTileFeature
{
    public const string NAME_PROPERTY_KEY = "name";

    public static class PbfTags
    {
        public const uint Id = 1 << 3 | (byte)WireType.Varint;
        public const uint Tags = 2 << 3 | (byte)WireType.Len;
        public const uint Type = 3 << 3 | (byte)WireType.Varint;
        public const uint Geometry = 4 << 3 | (byte)WireType.Len;
        
        internal static readonly Dictionary<string, PbfTag> Dictionary = new()
        {
            { "Id", (Id) },
            { "Tags", (Tags) },
            { "Type", (Type) },
            { "Geometry", (Geometry) },
            //{ "Raster", Raster }
        };
        
        internal static readonly HashSet<int> ValidFieldNumbers = [..Dictionary.Values.Select(tag => tag.FieldNumber)];
    }
    
    readonly VectorTileLayer _parent;
    public VectorTileLayer ParentLayer => _parent;
    protected VectorTile.ReadSettings Settings => _parent.ParentTile.Settings;
    
    readonly ReadOnlyMemory<byte> _featureData;
    
    /// <summary>Index of this feature in parent layer's feature collection</summary>
    public readonly int Index;
    /// <summary>ID of this feature. Optional; defaults to 0.</summary>
    /// <seealso href="https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L32">Schema on GitHub</seealso>
    public readonly ulong Id;
    
    /// <summary>
    /// Name of this feature, derived from 'name' property if present.
    /// </summary>
    public string Name
    {
        get
        {
            if (_properties.TryGetValue(NAME_PROPERTY_KEY, out var val))
            {
                if (val.Kind is ValueKind.String)
                {
                    return val.StringValue;
                }
                else if (Settings.ValidationLevel.HasFlag(PbfValidation.FeaturePropertyPairs))
                {
                    throw new PbfValidationFailure(PbfValidation.FeaturePropertyPairs, $"{ToString()} has 'name' property with unexpected kind {val.Kind}");
                }
                else
                {
                    return val.ToShortString();
                }
            }
            else
            {
                return $"F({Id})";
            }
        }
    }

    private BaseGeometry _geometry;
    /// <summary>
    /// Geometry commands using internal tile coordinates: n / layer extent
    /// </summary>
    /// <remarks>
    /// Saved as a packed series of uint32 (varints) in protobuf message.
    /// </remarks>
    public BaseGeometry Geometry
    {
        get
        {
            if (!_geometry.Parsed)
            {
                try
                {
                    _geometry = _geometry.Parse(
                        ParentLayer.ParentTile.Settings.ScaleToLayerExtents ? (1f / ParentLayer.Extent) : 1f);
                }
                catch (PbfReadFailure geometryIssue)
                {
                    throw new PbfValidationFailure(PbfValidation.Geometry, $"{_geometry.DeclaredType} read failure on {this}", geometryIssue);
                }
                catch (ArgumentOutOfRangeException alloationIssue)
                {
                    throw new PbfValidationFailure(PbfValidation.Geometry, $"{_geometry.DeclaredType} read failure on {this}", alloationIssue);
                }
            }

            return _geometry;
        }
    }

    /// <summary>
    /// Type of geometry encoded in this feature
    /// </summary>
    public GeometryType GeometryType => _geometry.DeclaredType;
    
    readonly IndexedReadOnlyDictionary<string, PropertyValue> _properties;
    /// <summary>
    /// Key value pairs created by dereferencing indices on lists in the parent layer
    /// </summary>
    /// <remarks>
    /// Indices are saved as a packed series of uint32 (varints) in protobuf message.
    /// </remarks>
    public IReadOnlyDictionary<string, PropertyValue> Properties => _properties;

    internal VectorTileFeature(ReadOnlyMemory<byte> featureData, VectorTileLayer parent, int index)
    {
        _featureData = featureData;
        _parent = parent;
        Index = index;
        
        // Validate all tags if required
        if (Settings.ValidationLevel.HasFlag(PbfValidation.Tags) &&
            PbfSpan.FindInvalidTags(featureData.Span, PbfTags.ValidFieldNumbers) is {Count: > 0} invalid)
        {
            throw PbfValidationFailure.FromTags(invalid);
        }

        Id = 0;

        var geometryMemory = ReadOnlyMemory<byte>.Empty;
        var geometryType = GeometryType.Unknown;
        
        // Traverse feature data to obtain initial values
        int offset = 0;
        List<int> kvTags = new List<int>();
        while (offset < featureData.Length)
        {
            var tag = PbfSpan.ReadTag(featureData.Span, ref offset);
            switch (tag)
            {
                case PbfTags.Id:
                    Id = PbfSpan.ReadVarint(featureData.Span, ref offset).ToUInt64();
                    continue;
                case PbfTags.Type:
                    geometryType = (GeometryType)PbfSpan.ReadVarint(featureData.Span, ref offset).ToUInt64();
                    continue;
                case PbfTags.Tags:
                    var tagSpan = PbfMemory.ReadLengthDelimited(featureData, ref offset).Span;
                    kvTags = PbfSpan.ReadVarintPackedField(tagSpan).ConvertAll(Varint.Int32Conversion);
                    continue;
                case PbfTags.Geometry:
                    geometryMemory = PbfMemory.ReadLengthDelimited(featureData, ref offset);
                    continue;
            }
            PbfSpan.SkipField(featureData.Span, ref offset, tag.WireType);
        }

        if (kvTags.Count % 2 is not 0)
        {
            if (Settings.ValidationLevel.HasFlag(PbfValidation.FeaturePropertyPairs))
            {
                throw new PbfValidationFailure(PbfValidation.FeaturePropertyPairs, 
                    $"Feature {Id} on {_parent.Name} layer of {_parent.ParentTile.TileId} has odd number of KV tags");
            }

            kvTags.RemoveAt(kvTags.Count - 1);
        }

        _geometry = new UnparsedGeometry(geometryMemory, geometryType);
        _properties = new IndexedReadOnlyDictionary<string, PropertyValue>(
            _parent.PropertyNames, _parent.PropertyValues, kvTags);
    }

    public override string ToString()
    {
        return $"Feature({Id}:{GeometryType}) of {_parent}";
    }

    static Dictionary<string, PropertyValue> PopulateProperties(List<int> indices, IReadOnlyList<string> keys)
    {
        var propertyDictionary = new Dictionary<string, PropertyValue>();
        // Enumerate all tag integers
        for (int i = 1; i < keys.Count; i++)
        {
            var vIdx = indices[i];
            var kIdx = indices[i-1];
        }
        
        return propertyDictionary;
    }
}