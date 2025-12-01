using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MvtMesherCore.Collections;
using MvtMesherCore.Mapbox.Geometry;

namespace MvtMesherCore.Mapbox;

/// <summary>
/// A collection of vector tile features within a layer, keyed by feature ID.
/// Features may lack IDs or have duplicate IDs.
/// Features lacking IDs will have an ID of 0 in this collection.
/// </summary>
/// <param name="features"></param>
public class LayerFeatureGroups(IEnumerable<VectorTileFeature> features) 
    : ReadOnlyKeyedBucketCollection<ulong, VectorTileFeature>(features), IReadOnlyList<VectorTileFeature>
{
    /// <inheritdoc/>
    public VectorTileFeature this[int index] => Items[index];

    /// <inheritdoc/>
    protected override ulong GetKey(VectorTileFeature item) => item.Id;

    /// <summary>
    /// Try to find a feature with the given ID and geometry type.
    /// </summary>
    /// <param name="id">Feature ID to search for.</param>
    /// <param name="geometryType">Geometry type to search for.</param>
    /// <param name="feature">Output feature if found; null otherwise.</param>
    public bool TryFindFeatureWithGeometryType(ulong id, GeometryType geometryType, [NotNullWhen(true)] out VectorTileFeature? feature)
    {
        feature = null;
        if (TryGetValue(id, out var candidates))
        {
            foreach (var candidate in candidates)
            {
                if (candidate.GeometryType == geometryType)
                {
                    feature = candidate;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Enumerates all individual features in this collection.
    /// </summary>
    public IEnumerable<VectorTileFeature> EnumerateIndividualFeatures()
    {
        foreach (var item in Items)
        {
            yield return item;
        }
    }

    IEnumerator<VectorTileFeature> IEnumerable<VectorTileFeature>.GetEnumerator()
    {
        return EnumerateIndividualFeatures().GetEnumerator();
    }
}