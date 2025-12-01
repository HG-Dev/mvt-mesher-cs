namespace MvtMesherCore.Mapbox.Geometry;

/// <summary>
/// Canvas drawing commands used in MVT geometries.
/// All coordinates are relative to the previous point;
/// the first point is relative to the origin (0,0).
/// </summary>
public enum CanvasCommand : byte 
{ 
    /// <summary>
    /// Error command (invalid).
    /// </summary>
    Error = 0,
    /// <summary>
    /// Move to command.
    /// </summary>
    MoveTo = 1,
    /// <summary>
    /// Line to command.
    /// </summary>
    LineTo = 2, 
    /// <summary>
    /// Close path command.
    /// </summary>
    ClosePath = 7 
}