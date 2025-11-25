using System.Numerics;
using MvtMesherCore.Collections;

namespace Tests.Collections;

[TestFixture]
public class ReadOnlyPointsTests
{
    [Test]
    public void Constructor_DefaultIsEmpty()
    {
        ReadOnlyPoints points = default;
        Assert.That(points, Is.Empty);
        Assert.That(points.RawValues, Is.EqualTo(ReadOnlyMemory<float>.Empty));
    }
    
    [TestCase(new float[] { 1f })]
    [TestCase(new float[] { 1f, 0f, 3f })]
    public void Constructor_ThrowsOnOddLength(float[] input)
    {
        Assert.That(() => new ReadOnlyPoints(input), Throws.ArgumentException);
    }

    [Test]
    public void CountIsHalfOfLength()
    {
        var points = new ReadOnlyPoints(new float[] { 1f, 2f, 3f, 4f });
        Assert.That(points.Count, Is.EqualTo(2));
    }

    [Test]
    public void IndexerReturnsCorrectVector2()
    {
        var points = new ReadOnlyPoints(new float[] { 1f, 2f, 3f, 4f });
        Assert.That(points[0], Is.EqualTo(new Vector2(1f, 2f)));
        Assert.That(points[1], Is.EqualTo(new Vector2(3f, 4f)));
    }

    [Test]
    public void IndexerThrowsOnOutOfRange()
    {
        var points = new ReadOnlyPoints(new float[] { 1f, 2f });
        Assert.That(() => { var _ = points[1]; }, Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void EnumeratorYieldsAllVectors()
    {
        var points = new ReadOnlyPoints(new float[] { 1f, 2f, 3f, 4f });
        var list = points.ToList();
        Assert.That(list.Count, Is.EqualTo(2));
        Assert.That(list[0], Is.EqualTo(new Vector2(1f, 2f)));
        Assert.That(list[1], Is.EqualTo(new Vector2(3f, 4f)));
    }
}
