using System;
using System.Collections.Generic;
using System.Numerics;

public static class Formulae
{
    /// <summary>
    /// Also known as Gauss's area formula, this algorithm computes the signed area of a polygon
    /// given three or more vertices in order. The final and first points must be identical to close the polygon.
    /// The sign of the area indicates the winding order of the polygon: 
    /// positive for counter-clockwise, negative for clockwise.
    /// The algorithm works by summing the cross products of each edge of the polygon
    /// with the next edge, effectively calculating the area of trapezoids formed by
    /// projecting the edges onto the X-axis.
    /// </summary>
    /// <param name="points">A sequence of three or more polygon vertices in order</param>
    /// <returns>The signed area of the polygon and its winding order</returns>
    public static (float area, CartesianWinding winding) ShoelaceAlgorithm(in IEnumerable<Vector2> points)
    {
        float area = 0f;
        int totalPoints = 0;
        Vector2 initialPoint = new Vector2();
        Vector2 acPoint = new Vector2();
        Vector2 bdPoint = new Vector2();

        foreach (var pt in points)
        {
            totalPoints++;
            switch (totalPoints)
            {
                case 1: // Acquire first point
                    initialPoint = pt;
                    acPoint = pt;
                    break;
                case 2: // Acquire second point and begin area calculation
                    bdPoint = pt;
                    goto AddArea;
                default:
                    acPoint = bdPoint;
                    bdPoint = pt;
                    goto AddArea;
            }

            AddArea:
            {
                area += (acPoint.X * bdPoint.Y) - (bdPoint.X * acPoint.Y);
            }
        }

        if (totalPoints < 3)
        {
            // Not enough points to form a polygon
            throw new ArgumentException("At least three points are required to compute the area of a polygon.");
        }

        if (bdPoint != initialPoint)
        {
            throw new ArgumentException("Points must form a closed polygon by repeating the first point as the last point.");
        }

        CartesianWinding sign = area switch
        {
            > float.Epsilon => CartesianWinding.CounterClockwise,
            < -float.Epsilon => CartesianWinding.Clockwise,
            _ => CartesianWinding.Invalid
        };

        return (area * 0.5f, sign);
    }

    /// <summary>
    /// Also known as Gauss's area formula, this algorithm computes the signed area of a polygon
    /// given three or more vertices in order. The final and first points must be identical to close the polygon.
    /// The sign of the area indicates the winding order of the polygon: 
    /// positive for counter-clockwise, negative for clockwise.
    /// The algorithm works by summing the cross products of each edge of the polygon
    /// with the next edge, effectively calculating the area of trapezoids formed by
    /// projecting the edges onto the X-axis.
    /// </summary>
    /// <remarks>
    /// More efficient than the IEnumerable overload since it avoids enumerator overhead.
    /// </remarks>
    /// <param name="points">A sequence of three or more polygon vertices in order</param>
    /// <returns>The signed area of the polygon and its winding order</returns>
    public static (float area, CartesianWinding winding) ShoelaceAlgorithm(IReadOnlyList<Vector2> points)
    {
        if (points.Count < 3)
        {
            // Not enough points to form a polygon
            throw new ArgumentException("At least three points are required to compute the area of a polygon.");
        }

        if (points[points.Count - 1] != points[0])
        {
            throw new ArgumentException("Points must form a closed polygon by repeating the first point as the last point.");
        }

        float area = 0f;
        for (int i = 1; i < points.Count; i++)
        {
            var acPoint = points[i - 1];
            var bdPoint = points[i];
            area += (acPoint.X * bdPoint.Y) - (bdPoint.X * acPoint.Y);
        }

        CartesianWinding sign = area switch
        {
            > float.Epsilon => CartesianWinding.CounterClockwise,
            < -float.Epsilon => CartesianWinding.Clockwise,
            _ => CartesianWinding.Invalid
        };

        return (area * 0.5f, sign);
    }
}