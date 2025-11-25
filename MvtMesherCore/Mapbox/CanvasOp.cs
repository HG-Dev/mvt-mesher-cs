namespace MvtMesherCore.Mapbox;

public enum CanvasOp : byte
{
    None,
    MoveTo = 1,
    LineTo = 2,
    ClosePath = 7
}