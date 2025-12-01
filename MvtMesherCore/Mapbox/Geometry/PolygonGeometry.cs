using System;
using System.Collections.Generic;
using System.Linq;
using MvtMesherCore.Collections;

namespace MvtMesherCore.Mapbox.Geometry;

/// <summary>
/// Container for polyline data parsed from sequential geometry (canvas) commands.
/// Each polyline should contain at least two UV coordinates.
/// </summary>
/// <param name="pgons">Polygon collection to be held by container.</param>
public class PolygonGeometry(IReadOnlyList<FloatPolygon> pgons) : ParsedGeometry(GeometryType.Polygon)
{
    /// <summary>
    /// Polygons contained in this geometry.
    /// </summary>
    public readonly IReadOnlyList<FloatPolygon> Polygons = pgons;
    /// <inheritdoc/>
    public override int MajorElementCount => Polygons.Count;
    /// <inheritdoc/>
    public override IEnumerable<System.Numerics.Vector2> EnumerateAllPoints() => Polygons.SelectMany(pgon => pgon.AllRings.SelectMany(ring => ring));

    internal static PolygonGeometry CreateFromCommands(ReadOnlySpan<byte> field, float scale)
    {
        // Ensure evenly sized float array
        var floats = new float[field.Length >> 1 << 1];
        (var polygons, var actualLength) = Populate(floats, field);
        ScaleAll(floats, actualLength, scale);
        return new PolygonGeometry(polygons);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(PolygonGeometry)}({Polygons.Count} pgons)";

    /// <summary>
    /// Populate polygon data from canvas commands.
    /// Each polygon may contain one or more rings; each ring contains two or more points.
    /// An exterior ring is wound clockwise; interior rings (holes) are wound counter-clockwise.
    /// </summary>
    /// <param name="values">1D float array for writing to</param>
    /// <param name="field">Span of bytes representing the encoded geometry commands</param>
    /// <returns>List of polygons and the actual length of floats used</returns>
    /// <exception cref="PbfReadFailure">Thrown when an invalid polygon ring is encountered</exception>
    static (List<FloatPolygon> polygons, int actualLength) Populate(float[] values, ReadOnlySpan<byte> field)
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
        var currentPointsPerRing = 0;
        List<FloatPolygon> polygons = new List<FloatPolygon>();
        List<FloatPointRing> currentRings = new List<FloatPointRing>();
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
                    currentPointsPerRing = commandCount + 1; // Plus one for MoveTo starting point
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
                    var ring = new FloatPointRing(new ReadOnlyMemory<float>(
                            values, 
                            valueIdx - currentPointsPerRing * 2, 
                            currentPointsPerRing * 2));

                    var (area, winding) = Formulae.ShoelaceAlgorithm(ring);

                    if (winding is CartesianWinding.Invalid)
                    {
                        throw new PbfReadFailure("Encountered invalid polygon ring with " +
                            "insufficient points when parsing polygon ring");
                    }
                    
                    currentRings.Add(ring);
                    if (currentRings.Count == 1)
                    {
                        // First ring in polygon must be exterior ring
                        if (winding.ToAxisFlippedRingType() is not RingType.Exterior)
                        {
                            throw new PbfReadFailure("First ring in polygon must be exterior ring " +
                                $"(clockwise winding order on Y-flipped canvas): {string.Join(", ", ring)} area={area}");
                        }
                    }
                    // If the ring is an exterior ring, export all current rings as a polygon
                    else if (winding.ToAxisFlippedRingType() is RingType.Exterior)
                    {
                        // Exterior ring or final ring in current polygon
                        polygons.Add(new FloatPolygon(currentRings.ToArray()));
                        // Start new polygon
                        currentRings = new List<FloatPointRing>();
                    }

                    break;
                default:
                    throw new PbfReadFailure("Encountered unexpected geometry" +
                        $"{commandId} command (x{commandCount}) when parsing {GeometryType.Polygon}(s)");
            }
        }

        // Record rings in final polygon
        if (currentRings.Any())
        {
            polygons.Add(new FloatPolygon(currentRings.ToArray()));
        }

        return new(polygons, valueIdx);
    }
}