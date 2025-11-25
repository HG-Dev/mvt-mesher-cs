using System.Collections.Generic;
using System.Runtime.CompilerServices.Collections;

namespace MvtMesherCore.Mapbox;

public class TileFeatureCollection(IEnumerable<VectorTileFeature> features) 
    : ReadOnlyKeyedCollection<ulong, VectorTileFeature>(features)
{
    protected override ulong GetKey(VectorTileFeature item) => item.Id;
}