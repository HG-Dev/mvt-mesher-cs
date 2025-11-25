using System;
using MvtMesherCore.Collections;

namespace MvtMesherCore.Mapbox.Geometry;

/// <summary>
/// Container for point data parsed from sequential geometry (canvas) commands,
/// creating one or more UV coordinates.
/// </summary>
/// <param name="points">Points collection to be held by container.</param>
public class PointGeometry(ReadOnlyPoints points) : ParsedGeometry(GeometryType.Point)
{
    public readonly ReadOnlyPoints Points = points;
    public override int MajorElementCount => Points.Count;

    internal static PointGeometry CreateFromCommands(ReadOnlySpan<byte> field, float scale)
    {
        // Ensure evenly sized float array (additive)
        var floats = new float[Math.Max(field.Length >> 1, 2)];
        var readOnlyPoints = Populate(floats, field);
        ScaleAll(floats, readOnlyPoints.RawValues.Length, scale);
        return new PointGeometry(readOnlyPoints);
    }

    public override string ToString() => $"{nameof(PointGeometry)}({Points.Count} pts)";

    static ReadOnlyPoints Populate(float[] values, ReadOnlySpan<byte> field)
    {
        int valueIdx = 0;
        int offset = 0;
        while (offset < field.Length)
        {
            var cmdInteger = PbfSpan.ReadVarint(field, ref offset).ToUInt32();
            CanvasCommand commandId = (CanvasCommand)(cmdInteger & 0x07);
            var pointsToConsume = (int)(cmdInteger >> 3);

            if (commandId is not CanvasCommand.MoveTo)
            {
                if (VectorTile.ValidationLevel.HasFlag(PbfValidation.Geometry))
                {
                    throw new PbfReadFailure(
                        $"Encountered unexpected geometry command command {commandId} when parsing {GeometryType.Point}(s)");
                }
            }
            
            // For every time MoveTo is repeated, obtain normalized coordinates.
            for (int i = 0; i < pointsToConsume; i++)
            {
                values[valueIdx++] = PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                values[valueIdx++] = PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
            }
        }

        //Console.Out.WriteLine($"{valueIdx} out of {values.Length} floats used");
        return new ReadOnlyPoints(new ReadOnlyMemory<float>(values, 0, valueIdx));
    }
}