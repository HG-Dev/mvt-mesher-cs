using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using MvtMesherCore.Collections;

namespace MvtMesherCore.Mapbox.Geometry;

/// <summary>
/// Container for polyline data parsed from sequential geometry (canvas) commands.
/// Each polyline should contain at least two UV coordinates.
/// </summary>
/// <param name="plines">Polyline collection to be held by container.</param>
public class PolylineGeometry(IReadOnlyList<FloatPoints> plines) : ParsedGeometry(GeometryType.Polyline)
{
    public readonly IReadOnlyList<FloatPoints> Polylines = plines;
    public override int MajorElementCount => Polylines.Count;
    public override IEnumerable<System.Numerics.Vector2> EnumerateAllPoints() => Polylines.SelectMany(pline => pline);

    internal static PolylineGeometry CreateFromCommands(ReadOnlySpan<byte> field, float scale)
    {
        // Ensure evenly sized float array
        var floats = new float[field.Length >> 1 << 1];
        var (polylines, actualLength) = Populate(floats, field);
        ScaleAll(floats, actualLength, scale);
        return new PolylineGeometry(polylines);
    }

    static (List<FloatPoints> polylines, int actualLength) Populate(float[] values, ReadOnlySpan<byte> field)
    {
        List<FloatPoints> polylines = new List<FloatPoints>();
        int valueIdx = 0;
        int offset = 0;
        /////////
        // Cursor
        // Initialized only once; all subsequent command parameters are relative to last position
        long cX = 0;
        long cY = 0;
        /////////
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
                    cX += PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                    cY += PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                    //Console.Out.WriteLine($"  MoveTo: ({cX}, {cY})");
                    values[valueIdx++] = cX;
                    values[valueIdx++] = cY;
                    break;
                case CanvasCommand.LineTo when commandCount > 0:
                    // Consume `commandCount` points to get rest of line
                    pointCounts.Add(commandCount + 1); // One added for MoveTo starting point
                    for (int i = 0; i < commandCount; i++)
                    {
                        cX += PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                        cY += PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                        //Console.Out.WriteLine($"  LineTo: ({cX}, {cY})");
                        values[valueIdx++] = cX;
                        values[valueIdx++] = cY;
                    }
                    // Create ReadOnlyPoints for this polyline
                    polylines.Add(new FloatPoints(new ReadOnlyMemory<float>(values, valueIdx - (pointCounts.Last() << 1), pointCounts.Last() << 1)));
                    break;
                default:
                    throw new PbfReadFailure("Encountered unexpected" +
                        $"{commandId} command (x{commandCount}) when parsing {GeometryType.Polyline}(s)");
            }
        }
        
        return (polylines, valueIdx);
    }

    public override string ToString() => $"{nameof(PolylineGeometry)}({Polylines.Count} plines)";
}