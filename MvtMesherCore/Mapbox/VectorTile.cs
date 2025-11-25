using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using MvtMesherCore.Models;

namespace MvtMesherCore.Mapbox;

[DebuggerDisplay("Tile {TileId}")]
public class VectorTile : ReadOnlyDictionary<string, VectorTileLayer>
{
    public const int ProtobufSchemaVersion = 2;
    static class PbfTags
    {
        internal static readonly PbfTag Layers = new PbfTag(3, WireType.Len);
        internal static readonly Dictionary<string, PbfTag> Dictionary = new()
        {
            { "Layers", Layers }
        };
        internal static readonly HashSet<int> ValidFieldNumbers = [..Dictionary.Values.Select(tag => tag.FieldNumber)];
    }

    public static PbfValidation ValidationLevel;
    public readonly CanonicalTileId TileId;
    public ICollection<string> LayerNames => Dictionary.Keys;

    /// <summary>
    /// A Mapbox Vector Tile layer. Contains features, Name, Version, Extent (ulong), PropertyValues, and Keys
    /// </summary>
    VectorTile(CanonicalTileId tileId, Dictionary<string, VectorTileLayer> layers) : base(layers)
    {
        TileId = tileId;
    }

    public static VectorTile FromByteArray(byte[] rawData, CanonicalTileId tileId)
    {
        if (rawData is not { Length: > 0 }) throw new ArgumentException("Array must not be null or empty.", nameof(rawData));
        // Detect gzipped array: https://stackoverflow.com/questions/19364497/how-to-tell-if-a-byte-array-is-gzipped
        if (rawData[0] == 31 /*0x1F*/ && rawData[1] == 139) throw new ArgumentException("Array bytes are a gzip stream. It must be unzipped first.", nameof(rawData));

        if (ValidationLevel.HasFlag(PbfValidation.Tags) &&
            PbfSpan.FindInvalidTags(rawData, PbfTags.ValidFieldNumbers) is { Count: > 0 } invalid)
        {
            throw PbfValidationFailure.FromTags(invalid);
        }

        var layers = new Dictionary<string, VectorTileLayer>();
        var tile = new VectorTile(tileId, layers);
        PopulateLayers(layers, rawData, tile);
        return tile;
    }
    
    static void PopulateLayers(Dictionary<string, VectorTileLayer> layers, ReadOnlyMemory<byte> tileData, VectorTile parent)
    {
        var layerIndex = 0;

        foreach (var layerMemory in PbfMemory.FindAndEnumerateFieldsWithTag(tileData, PbfTags.Layers))
        {
            // Create a potential new layer and get its Name through construction
            var layer = VectorTileLayer.FromBytes(layerMemory, parent, layerIndex++);
        
            if (ValidationLevel.HasFlag(PbfValidation.LayerNames) && layer.IsUnnamedInPbf)
            {
                // Edge case to check: name field comes at very end of layer data
                throw new PbfValidationFailure(PbfValidation.LayerNames, $"{layer.Name} is missing a name.");
            }

            if (!layers.TryGetValue(layer.Name, out var existingLayer))
            {
                layers.Add(layer.Name, layer);
            }
            else if (ValidationLevel.HasFlag(PbfValidation.LayerDuplication))
            {
                throw new PbfValidationFailure(PbfValidation.LayerDuplication,
                    $"Duplicate layer name '{layer.Name}' ({layer.Index}); " +
                    $"existing layer has index {existingLayer.Index}.");
            }
        }
    }
}