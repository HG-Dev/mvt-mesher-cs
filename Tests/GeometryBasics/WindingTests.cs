using System.Numerics;
using MvtMesherCore.Mapbox.Geometry;

[TestFixture]
public class WindingTests
{
    const string TenBoxCCW = @"
        0, 0
        10, 0
        10, 10
        0, 10
        0, 0
    ";
    const string TenBoxRotatedCCW = @"
        10, 0
        10, 10
        0, 10
        0, 0
        10, 0
    ";
    const string EnoshimaSimpleSampleCCW = @"
        5, 1
        5, 2
        4, 2
        4, 1
        5, 1
    ";
    const string EnoshimaSampleCCW = @"
        53, 16
        54, 20
        48, 22
        47, 18
        53, 16
    ";
    const string CrownRingCCW = @"
        2, 2
        8, 2
        8, 8
        5, 5
        2, 8
        2, 2
    ";

    private List<Vector2> ParsePoints(string points)
    {
        var result = new List<Vector2>();
        var lines = points.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            var parts = trimmed.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) throw new FormatException($"Invalid point format: '{line}'");
            if (!float.TryParse(parts[0], out float x) || !float.TryParse(parts[1], out float y))
            {
                throw new FormatException($"Invalid numeric values in point: '{line}'");
            }
            result.Add(new Vector2(x, y));
        }
        return result;
    }

    [TestCase(TenBoxCCW)]
    [TestCase(TenBoxRotatedCCW)]
    [TestCase(EnoshimaSimpleSampleCCW)]
    [TestCase(EnoshimaSampleCCW)]
    [TestCase(CrownRingCCW)]
    public void ExteriorRingWindingOrderShouldBeCorrectlyInterpreted(string exteriorRingPoints)
    {
        var exteriorRing = ParsePoints(exteriorRingPoints);
        var (area, winding) = Formulae.ShoelaceAlgorithm(exteriorRing);
        var (area2, winding2) = Formulae.ShoelaceAlgorithm((IEnumerable<Vector2>)exteriorRing);
                Assert.That(winding, Is.EqualTo(winding2), "Winding computed from IEnumerable<Vector2> should match winding from List<Vector2>");
        Assert.That(area, Is.EqualTo(area2), "Area computed from IEnumerable<Vector2> should match area from List<Vector2>");
        Assert.That(area, Is.GreaterThan(0), "Exterior ring should have positive area");
        Assert.That(winding, Is.EqualTo(CartesianWinding.CounterClockwise), 
            "Exterior ring should have counter-clockwise winding order on standard Cartesian canvas");
        Assert.That(winding.ToAxisFlippedRingType(), Is.EqualTo(RingType.Exterior),
            "Exterior ring should have clockwise winding order on Y-flipped canvas");
    }
    
    const string TenBoxCW = @"
        0, 0
        0, 10
        10, 10
        10, 0
        0, 0
    ";

    const string CrownRingCW = @"
        2, 2
        2, 8
        5, 5
        8, 8,
        8, 2
        2, 2
    ";

    [TestCase(TenBoxCW)]
    [TestCase(CrownRingCW)]
    public void InteriorRingWindingOrderShouldBeCorrectlyInterpreted(string interiorRingPoints)
    {
        var interiorRing = ParsePoints(interiorRingPoints);
        var (area, winding) = Formulae.ShoelaceAlgorithm(interiorRing);
        var (area2, winding2) = Formulae.ShoelaceAlgorithm((IEnumerable<Vector2>)interiorRing);
        Assert.That(winding, Is.EqualTo(winding2), "Winding computed from IEnumerable<Vector2> should match winding from List<Vector2>");
        Assert.That(area, Is.EqualTo(area2), "Area computed from IEnumerable<Vector2> should match area from List<Vector2>");
        Assert.That(area, Is.LessThan(0), "Interior ring should have negative area");
        Assert.That(winding, Is.EqualTo(CartesianWinding.Clockwise), 
            "Interior ring should have clockwise winding order on standard Cartesian canvas");
        Assert.That(winding.ToAxisFlippedRingType(), Is.EqualTo(RingType.Interior),
            "Interior ring should have counter-clockwise winding order on Y-flipped canvas");
    }
}   