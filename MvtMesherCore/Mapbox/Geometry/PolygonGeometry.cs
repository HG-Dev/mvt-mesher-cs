using System;
using System.Collections.Generic;
using MvtMesherCore.Collections;

namespace MvtMesherCore.Mapbox.Geometry;

/// <summary>
/// Container for polyline data parsed from sequential geometry (canvas) commands.
/// Each polyline should contain at least two UV coordinates.
/// </summary>
/// <param name="plines">Polyline collection to be held by container.</param>
public class PolygonGeometry(ReadOnlyPolygons pgons) : ParsedGeometry(GeometryType.Polygon)
{
    public readonly ReadOnlyPolygons Polygons = pgons;
    public override int MajorElementCount => Polygons.Count;

    internal static PolygonGeometry CreateFromCommands(ReadOnlySpan<byte> field, float scale)
    {
        // Ensure evenly sized float array
        var floats = new float[field.Length >> 1 << 1];
        var readOnlyPoints = Populate(floats, field);
        ScaleAll(floats, readOnlyPoints.RawValues.Length, scale);
        return new PolygonGeometry(readOnlyPoints);
    }

    public override string ToString() => $"{nameof(PolylineGeometry)}({Polygons.Count} pgons)";

    static ReadOnlyPolygons Populate(float[] values, ReadOnlySpan<byte> field)
    {
        // Obtain the float values, polyline lengths, and polylines/rings per polygon
        int valueIdx = 0;
        int offset = 0;
        int currentRingCount = 0;
        List<int> pointsPerRing = new List<int>();
        List<int> ringsPerPolygon = new List<int>();
        while (offset < field.Length)
        {
            var cmdInteger = PbfSpan.ReadVarint(field, ref offset).ToUInt32();
            CanvasCommand commandId = (CanvasCommand)(cmdInteger & 0x07);
            var commandCount = (int)(cmdInteger >> 3);

            switch (commandId)
            {
                case CanvasCommand.MoveTo when commandCount is 1:
                    // Consume two points to get start of line
                    values[valueIdx++] = PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                    values[valueIdx++] = PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                    break;
                case CanvasCommand.LineTo when commandCount > 1:
                    // Consume `commandCount` * 2 points to get rest of line
                    pointsPerRing.Add(commandCount + 1); // Two added for MoveTo starting point
                    for (int i = 0; i < commandCount; i++)
                    {
                        values[valueIdx++] = PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                        values[valueIdx++] = PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                    }
                    currentRingCount++;
                    break;
                case CanvasCommand.ClosePath:
                    ringsPerPolygon.Add(currentRingCount);
                    currentRingCount = 0;
                    break;
                default:
                    if (VectorTile.ValidationLevel.HasFlag(PbfValidation.Geometry))
                        throw new PbfReadFailure("Encountered unexpected geometry" +
                            $"{commandId} command (x{commandCount}) when parsing {GeometryType.Polyline}(s)");
                    break;
            }
        }

        Console.Out.WriteLine($"{valueIdx} out of {values.Length} floats used");
        return new ReadOnlyPolygons(
            new ReadOnlyMemory<float>(values, 0, valueIdx), 
            ringsPerPolygon.ToArray(), 
            pointsPerRing.ToArray());
    }
}