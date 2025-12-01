using System;

namespace MvtMesherCore.Mapbox;

/// <summary>
/// Validation options for Protocol Buffer-encoded MVT data.
/// </summary>
[Flags]
public enum PbfValidation
{
    /// <summary>
    /// No validation.
    /// </summary>
    None = 0,
    /// <summary>
    /// Validate tags.
    /// </summary>
    Tags = 1,
    /// <summary>
    /// Validate layer names.
    /// </summary>
    LayerNames = 2,
    /// <summary>
    /// Validate layer duplication.
    /// </summary>
    LayerDuplication = 4,
    /// <summary>
    /// Validate layer version.
    /// </summary>
    LayerVersion = 8,
    /// <summary>
    /// Validate feature-property pairs.
    /// </summary>
    FeaturePropertyPairs = 16,
    /// <summary>
    /// Validate geometry.
    /// </summary>
    Geometry = 32,
    /// <summary>
    /// Standard validation (tags, layer names, layer duplication).
    /// </summary>
    Standard = Tags | LayerNames | LayerDuplication,
    /// <summary>
    /// Full validation (all checks).
    /// </summary>
    All = Tags | LayerNames | LayerDuplication | LayerVersion | FeaturePropertyPairs | Geometry
}