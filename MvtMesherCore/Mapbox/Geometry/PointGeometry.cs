using System;
using System.Collections.Generic;
using MvtMesherCore.Collections;

namespace MvtMesherCore.Mapbox.Geometry;

/// <summary>
/// Container for point data parsed from sequential geometry (canvas) commands,
/// creating one or more UV coordinates.
/// </summary>
/// <param name="points">Points collection to be held by container.</param>
public class PointGeometry(FloatPoints points) : ParsedGeometry(GeometryType.Point)
{
    public readonly FloatPoints Points = points;
    public override int MajorElementCount => Points.Count;
    public override IEnumerable<System.Numerics.Vector2> EnumerateAllPoints() => Points;

    internal static PointGeometry CreateFromCommands(ReadOnlySpan<byte> field, float scale)
    {
        // Ensure evenly sized float array (additive)
        var floats = new float[Math.Max(field.Length >> 1, 2)];
        var points = Populate(floats, field);
        ScaleAll(floats, points.RawValues.Length, scale);
        return new PointGeometry(points);
    }

    public override string ToString() => $"{nameof(PointGeometry)}({Points.Count} pts)";

    static FloatPoints Populate(float[] values, ReadOnlySpan<byte> field)
    {
        int valueIdx = 0;
        int offset = 0;
        /////////
        // Cursor
        // Initialized only once; all subsequent command parameters are relative to last position
        long cX = 0;
        long cY = 0;
        /////////
        while (offset < field.Length)
        {
            var cmdInteger = PbfSpan.ReadVarint(field, ref offset).ToUInt32();
            CanvasCommand commandId = (CanvasCommand)(cmdInteger & 0x07);
            var pointsToConsume = (int)(cmdInteger >> 3);

            if (commandId is not CanvasCommand.MoveTo)
            {
                throw new PbfReadFailure(
                    $"Encountered unexpected geometry command command {commandId} when parsing {GeometryType.Point}(s)");
            }
            
            // For every time MoveTo is repeated, obtain normalized coordinates.
            for (int i = 0; i < pointsToConsume; i++)
            {
                cX += PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                cY += PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                values[valueIdx++] = cX;
                values[valueIdx++] = cY;
            }
        }

        //Console.Out.WriteLine($"{valueIdx} out of {values.Length} floats used");
        return new FloatPoints(new ReadOnlyMemory<float>(values, 0, valueIdx));
    }
}