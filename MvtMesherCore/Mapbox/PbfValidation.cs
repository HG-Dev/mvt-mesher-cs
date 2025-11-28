using System;

namespace MvtMesherCore.Mapbox;

[Flags]
public enum PbfValidation
{
    None = 0,
    Tags = 1,
    LayerNames = 2,
    LayerDuplication = 4,
    LayerVersion = 8,
    FeaturePropertyPairs = 16,
    Geometry = 32,
    Standard = Tags | LayerNames | LayerDuplication,
    All = Tags | LayerNames | LayerDuplication | LayerVersion | FeaturePropertyPairs | Geometry
}