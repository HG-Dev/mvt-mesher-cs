using System;
using System.Buffers;
using System.Collections.Generic;
using MvtMesherCore.Collections;

namespace MvtMesherCore.Mapbox.Geometry;

/// <summary>
/// Container for polyline data parsed from sequential geometry (canvas) commands.
/// Each polyline should contain at least two UV coordinates.
/// </summary>
/// <param name="plines">Polyline collection to be held by container.</param>
public class PolylineGeometry(ReadOnlyPolylines plines) : ParsedGeometry(GeometryType.Polyline)
{
    public readonly ReadOnlyPolylines Polylines = plines;
    public override int MajorElementCount => Polylines.Count;

    internal static PolylineGeometry CreateFromCommands(ReadOnlySpan<byte> field, float scale)
    {
        // Ensure evenly sized float array
        var floats = new float[field.Length >> 1 << 1];
        var readOnlyPoints = Populate(floats, field);
        ScaleAll(floats, readOnlyPoints.RawValues.Length, scale);
        return new PolylineGeometry(readOnlyPoints);
    }

    public override string ToString() => $"{nameof(PolylineGeometry)}({Polylines.Count} plines)";

    static ReadOnlyPolylines Populate(float[] values, ReadOnlySpan<byte> field)
    {
        int valueIdx = 0;
        int offset = 0;
        List<int> pointCounts = new List<int>();
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
                case CanvasCommand.LineTo when commandCount > 0:
                    // Consume `commandCount` points to get rest of line
                    pointCounts.Add(commandCount + 1); // One added for MoveTo starting point
                    for (int i = 0; i < commandCount; i++)
                    {
                        values[valueIdx++] = PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                        values[valueIdx++] = PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                    }
                    break;
                default:
                    if (VectorTile.ValidationLevel.HasFlag(PbfValidation.Geometry))
                        throw new PbfReadFailure("Encountered unexpected" +
                            $"{commandId} command (x{commandCount}) when parsing {GeometryType.Polyline}(s)");
                    break;
            }
        }

        //Console.Out.WriteLine($"{valueIdx} out of {values.Length} pline floats used");
        return new ReadOnlyPolylines(new ReadOnlyMemory<float>(values, 0, valueIdx), pointCounts.ToArray());
    }
}