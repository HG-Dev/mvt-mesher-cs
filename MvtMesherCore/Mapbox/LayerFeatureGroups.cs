using System.Collections.Generic;
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
    public VectorTileFeature this[int index] => Items[index];

    protected override ulong GetKey(VectorTileFeature item) => item.Id;

    public bool TryFindFeatureWithGeometryType(ulong id, GeometryType geometryType, out VectorTileFeature? feature)
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