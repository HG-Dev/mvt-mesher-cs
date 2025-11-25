namespace MvtMesherCore.Mapbox.Geometry;

public enum CanvasCommand : byte 
{ 
    Error = 0,
    MoveTo = 1,
    LineTo = 2, 
    ClosePath = 7 
}