using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using MvtMesherCore.Models;

namespace MvtMesherCore.Mapbox;

/// <summary>
/// A Mapbox Vector Tile, parsed from a profobuf-encoded byte array.
/// </summary>
[DebuggerDisplay("Tile {TileId}")]
public class VectorTile
{
    /// <summary>
    /// Expected version of the protobuf schema.
    /// https://github.com/mapbox/vector-tile-spec/tree/master/2.1
    /// </summary>
    /// <remarks>
    /// Currently, only version 2 is supported.
    /// </remarks>
    public const int ProtobufSchemaVersion = 2;

    /// <summary>
    /// PBF tags used in the VectorTile message.
    /// </summary>
    public static class PbfTags
    {
        /// <summary> Layers tag with Length Delimited wire type. </summary>
        public static readonly PbfTag Layers = new PbfTag(3, WireType.Len);
        internal static readonly Dictionary<string, PbfTag> Dictionary = new()
        {
            { "Layers", Layers }
        };
        internal static readonly HashSet<int> ValidFieldNumbers = [..Dictionary.Values.Select(tag => tag.FieldNumber)];
    }

    /// <summary>
    /// Settings for reading a VectorTile.
    /// </summary>
    public class ReadSettings
    {
        /// <summary>
        /// If true, scale geometries to the layer's extent when reading.
        /// </summary>
        public bool ScaleToLayerExtents = true;
        /// <summary>
        /// Level of PBF validation to perform when reading.
        /// </summary>
        public PbfValidation ValidationLevel = PbfValidation.Standard;
    }

    /// <summary>
    /// Tile ID of this vector tile.
    /// </summary>
    /// <remarks>
    /// Not included in the PBF data itself, but useful for context.
    /// </remarks>
    public readonly CanonicalTileId TileId;
    /// <summary>
    /// Settings used when reading this vector tile.
    /// </summary>
    public readonly ReadSettings Settings;
    
    /// <summary>
    /// Alternative dereferencing method for clarity
    /// </summary>
    public readonly ReadOnlyDictionary<string, VectorTileLayer> LayersByName;
    /// <summary>
    /// Names of all layers in this vector tile.
    /// </summary>
    public ICollection<string> LayerNames => LayersByName.Keys;
    /// <summary>
    /// All layers in this vector tile.
    /// </summary>
    public ICollection<VectorTileLayer> Layers => LayersByName.Values;

    VectorTile(CanonicalTileId tileId, Dictionary<string, VectorTileLayer> layers, ReadSettings settings)
    {
        TileId = tileId;
        Settings = settings;
        LayersByName = new ReadOnlyDictionary<string, VectorTileLayer>(layers);
    }

    /// <summary>
    /// Create a VectorTile from a byte array of protobuf data.
    /// </summary>
    /// <param name="rawData">Bytes of protobuf-encoded vector tile data.</param>
    /// <param name="tileId">ID of this tile</param>
    /// <param name="settings">Settings to use when reading</param>
    /// <returns>A VectorTile instance</returns>
    /// <exception cref="ArgumentException">Thrown when the input data is empty or gzip compressed.</exception>
    public static VectorTile FromByteArray(byte[] rawData, CanonicalTileId tileId, ReadSettings? settings = null)
    {
        settings ??= new ReadSettings();
        if (rawData is not { Length: > 0 }) throw new ArgumentException("Array must not be null or empty.", nameof(rawData));
        // Detect gzipped array: https://stackoverflow.com/questions/19364497/how-to-tell-if-a-byte-array-is-gzipped
        if (rawData[0] == 31 /*0x1F*/ && rawData[1] == 139) throw new ArgumentException("Array bytes are a gzip stream. It must be unzipped first.", nameof(rawData));

        if (settings.ValidationLevel.HasFlag(PbfValidation.Tags) &&
            PbfSpan.FindInvalidTags(rawData, PbfTags.ValidFieldNumbers) is { Count: > 0 } invalid)
        {
            throw PbfValidationFailure.FromTags(invalid);
        }

        var layers = new Dictionary<string, VectorTileLayer>();
        var tile = new VectorTile(tileId, layers, settings);
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
        
            if (parent.Settings.ValidationLevel.HasFlag(PbfValidation.LayerNames) && layer.IsUnnamedInPbf)
            {
                // Edge case to check: name field comes at very end of layer data
                throw new PbfValidationFailure(PbfValidation.LayerNames, $"{layer.Name} is missing a name.");
            }

            if (!layers.TryGetValue(layer.Name, out var existingLayer))
            {
                layers.Add(layer.Name, layer);
            }
            else if (parent.Settings.ValidationLevel.HasFlag(PbfValidation.LayerDuplication))
            {
                throw new PbfValidationFailure(PbfValidation.LayerDuplication,
                    $"Duplicate layer name '{layer.Name}' ({layer.Index}); " +
                    $"existing layer has index {existingLayer.Index}.");
            }
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"VectorTile({TileId.ToShortString()}, {LayersByName.Count} layers)";
    }
}