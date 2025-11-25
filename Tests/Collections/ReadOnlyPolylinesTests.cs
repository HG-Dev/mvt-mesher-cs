using MvtMesherCore.Collections;
using MvtMesherCore.Mapbox;

namespace Tests.Collections;

[TestFixture]
public class ReadOnlyPolylinesTests
{
    [Test]
    public void Constructor_ValidData_ShouldCreatePolylines()
    {
        var values = new float[] { 0f, 0f, 1f, 1f, 2f, 2f, 3f, 3f };
        var sliceLengths = new int[] { 2, 2 }; // Two polylines, each with 2 points (4 floats)

        var polylines = new ReadOnlyPolylines(values, sliceLengths);

        Assert.That(polylines.Count, Is.EqualTo(2));
        Assert.That(polylines[0].RawValues.Length, Is.EqualTo(4));
        Assert.That(polylines[1].RawValues.Length, Is.EqualTo(4));
    }

    [Test]
    public void Constructor_OnePointLine_ShouldNotThrow()
    {
        var values = new float[] { 0f, 1f, 2f, 3f, 4f, 5f }; // Last polyline has only one point
        var sliceLengths = new int[] { 2, 1 };

        Assert.DoesNotThrow(() => new ReadOnlyPolylines(values, sliceLengths));
    }


    [Test]
    public void Constructor_OddValuesLength_ShouldThrow()
    {
        var values = new float[] { 0f, 1f, 2f }; // Odd length
        var sliceLengths = new int[] { 1, 1 };

        Assert.Throws<ArgumentException>(() => new ReadOnlyPolylines(values, sliceLengths));
    }

    [Test]
    public void Constructor_SliceLengthsSumMismatch_ShouldThrow()
    {
        var values = new float[] { 0f, 0f, 1f, 1f };
        var sliceLengths = new int[] { 1 }; // Sum = 2, but values.Length = 4

        Assert.Throws<ArgumentException>(() => new ReadOnlyPolylines(values, sliceLengths));
    }

    [Test]
    public void SliceMethod_ShouldIterateAllExpectedPolylines()
    {
        var values = new float[] { 0f, 0f, 1f, 1f, 2f, 2f, 3f, 3f, 0f, 0f, -1f, -1f, -5, -5 };
        var sliceLengths = new int[] { 2, 4, 1 };

        var polylines = new ReadOnlyPolylines(values, sliceLengths);

        var exteriorRing = polylines.Slice(0, 1);
        Assert.That(exteriorRing[0].Count, Is.EqualTo(2));
        var interiorRings = polylines.Slice(1..);
        Assert.That(interiorRings.Count, Is.EqualTo(2));
        Assert.That(interiorRings[0].Count, Is.EqualTo(4));
        Assert.That(interiorRings[1].Count, Is.EqualTo(1));
        var firstInteriorRing = polylines.Slice(1..^1);
        Assert.That(firstInteriorRing[0].Count, Is.EqualTo(4));
        Assert.That(firstInteriorRing[^1].Count, Is.EqualTo(4));
    }

    [Test]
    public void Enumerator_ShouldIterateAllPolylines()
    {
        var values = new float[] { 0f, 0f, 1f, 1f, 2f, 2f, 3f, 3f };
        var sliceLengths = new int[] { 2, 2 };

        var polylines = new ReadOnlyPolylines(values, sliceLengths);

        int count = 0;
        foreach (var polyline in polylines)
        {
            Assert.That(polyline.RawValues.Length, Is.EqualTo(4));
            count++;
        }

        Assert.That(count, Is.EqualTo(2));
    }
}
