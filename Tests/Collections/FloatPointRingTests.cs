using MvtMesherCore.Collections;
using MvtMesherCore.Mapbox;

namespace Tests.Collections;

[TestFixture]
public class ReadOnlyPolylinesTests
{
    [Test]
    public void Constructor_ValidData_ShouldCreateRing()
    {
        var values = new float[] { 0f, 0f, 1f, 1f, 2f, 2f, 3f, 3f };

        var ring = new FloatPointRing(values);

        Assert.That(ring.Count, Is.EqualTo(4 + 1)); // 4 points + loopback
        Assert.That(ring[0], Is.EqualTo(new System.Numerics.Vector2(0f, 0f)));
        Assert.That(ring[3], Is.EqualTo(new System.Numerics.Vector2(3f, 3f)));
        Assert.That(ring[4], Is.EqualTo(new System.Numerics.Vector2(0f, 0f))); // loopback
    }

    [Test]
    public void UnclosedPoints_ShouldReturnOriginalValues()
    {
        var values = new float[] { 0f, 0f, 1f, 1f, 2f, 2f };

        var ring = new FloatPointRing(values);

        Assert.That(ring.Count, Is.EqualTo(3 + 1)); // 3 points + loopback
        Assert.That(ring[0], Is.EqualTo(new System.Numerics.Vector2(0f, 0f)));
        Assert.That(ring[2], Is.EqualTo(new System.Numerics.Vector2(2f, 2f)));
        Assert.That(ring[3], Is.EqualTo(new System.Numerics.Vector2(0f, 0f))); // loopback

        Assert.That(ring.UnclosedPoints.Count, Is.EqualTo(3));
        Assert.That(ring.UnclosedPoints[0], Is.EqualTo(new System.Numerics.Vector2(0f, 0f)));
        Assert.That(ring.UnclosedPoints[2], Is.EqualTo(new System.Numerics.Vector2(2f, 2f)));
        Assert.That(ring.UnclosedPoints.Count, Is.EqualTo(3));
        Assert.That(ring.UnclosedPoints.RawValues.Length, Is.EqualTo(6));
    }
}
