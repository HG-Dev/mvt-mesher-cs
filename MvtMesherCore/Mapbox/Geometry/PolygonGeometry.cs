using System;
using System.Collections.Generic;
using System.Linq;
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
    public override IEnumerable<System.Numerics.Vector2> EnumerateAllPoints() => Polygons.SelectMany(pgon => pgon.AllRings.SelectMany(ring => ring));

    internal static PolygonGeometry CreateFromCommands(ReadOnlySpan<byte> field, float scale)
    {
        // Ensure evenly sized float array
        var floats = new float[field.Length >> 1 << 1];
        var readOnlyPolygons = Populate(floats, field);
        ScaleAll(floats, readOnlyPolygons.RawValues.Length, scale);
        return new PolygonGeometry(readOnlyPolygons);
    }

    public override string ToString() => $"{nameof(PolylineGeometry)}({Polygons.Count} pgons)";

    /// <summary>
    /// Populate polygon data from canvas commands.
    /// Each polygon may contain one or more rings; each ring contains two or more points.
    /// An exterior ring is wound clockwise; interior rings (holes) are wound counter-clockwise.
    /// </summary>
    /// <param name="values"></param>
    /// <param name="field"></param>
    /// <param name="closeRings"></param>
    /// <returns></returns>
    /// <exception cref="PbfReadFailure"></exception>
    static ReadOnlyPolygons Populate(float[] values, ReadOnlySpan<byte> field)
    {
        // Obtain the float values, polyline lengths, and polylines/rings per polygon
        int valueIdx = 0;
        int offset = 0;
        /////////
        // Cursor
        // Initialized only once; all subsequent command parameters are relative to last position
        long cX = 0;
        long cY = 0;
        /////////
        int currentRingCount = 0;
        List<int> rawPointsPerRing = new List<int>();
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
                    cX += PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                    cY += PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                    values[valueIdx++] = cX;
                    values[valueIdx++] = cY;
                    break;
                case CanvasCommand.LineTo when commandCount > 1:
                    // Consume `commandCount` points to get rest of line
                    rawPointsPerRing.Add(commandCount + 1); // Plus one for MoveTo starting point
                    for (int i = 0; i < commandCount; i++)
                    {
                        cX += PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                        cY += PbfSpan.ReadVarint(field, ref offset).ZigZagDecode();
                        values[valueIdx++] = cX;
                        values[valueIdx++] = cY;
                    }

                    break;
                case CanvasCommand.ClosePath:
                    // Preview and identify current ring's winding order
                    var ringPreview = new ReadOnlyPoints(new ReadOnlyMemory<float>(
                            values, 
                            valueIdx - rawPointsPerRing[^1] * 2, 
                            rawPointsPerRing[^1] * 2), 
                            ensureClosedRing: true);

                    var (area, winding) = Formulae.ShoelaceAlgorithm(ringPreview);

                    if (winding is CartesianWinding.Invalid)
                    {
                        throw new PbfReadFailure("Encountered invalid polygon ring with " +
                            "insufficient points when parsing polygon ring");
                    }
                    
                    currentRingCount++;
                    if (currentRingCount == 1)
                    {
                        // First ring in polygon must be exterior ring
                        if (winding.ToAxisFlippedRingType() is not RingType.Exterior)
                        {
                            throw new PbfReadFailure("First ring in polygon must be exterior ring " +
                                $"(clockwise winding order on Y-flipped canvas): {string.Join(", ", ringPreview)} area={area}");
                        }
                    }
                    // If the ring is an exterior ring, or if this is the final ring in the polygon,
                    // record the number of rings in the polygon.
                    else if (winding.ToAxisFlippedRingType() is RingType.Exterior)
                    {
                        // Exterior ring or final ring in current polygon
                        ringsPerPolygon.Add(currentRingCount);
                        // Start new polygon
                        currentRingCount = 0;
                    }

                    break;
                default:
                    throw new PbfReadFailure("Encountered unexpected geometry" +
                        $"{commandId} command (x{commandCount}) when parsing {GeometryType.Polygon}(s)");
            }
        }

        // Record rings in final polygon
        if (currentRingCount > 0)
        {
            ringsPerPolygon.Add(currentRingCount);
        }

        return new ReadOnlyPolygons(
            new ReadOnlyMemory<float>(values, 0, valueIdx), 
            ringsPerPolygon.ToArray(), 
            rawPointsPerRing.ToArray());
    }
}