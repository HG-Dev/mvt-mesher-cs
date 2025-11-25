using System;

namespace MvtMesherCore.Mapbox;

[Flags]
public enum PbfValidation
{
    None = 0,
    Tags = 1,
    LayerNames = 2,
    LayerDuplication = 4,
    FeatureVersion = 8,
    FeaturePropertyPairs = 16,
    Geometry = 32,
    All = Tags | LayerNames | LayerDuplication | FeatureVersion | FeaturePropertyPairs | Geometry
}