namespace Tests.Collections;

[TestFixture]
public class IndexedReadOnlyDictionaryTests
{
    List<string> _keys;
    List<int> _values;

    [SetUp]
    public void Setup()
    {
        _keys = new List<string> { "A", "B", "C" };
        _values = new List<int> { 10, 20, 30 };
    }

    [Test]
    public void Constructor_ShouldSortPairsAndValidateDistinctKeys()
    {
        var pairs = new List<(int keyIndex, int valueIndex)> { (2, 2), (0, 0), (1, 1) };
        var dict = new IndexedReadOnlyDictionary<string, int>(_keys, _values, pairs);

        Assert.That(dict.Keys.ToList(), Is.EqualTo(new[] { "A", "B", "C" }));
    }

    [Test]
    public void Constructor_ShouldThrowOnDuplicateKeyIndices()
    {
        var pairs = new List<(int keyIndex, int valueIndex)> { (0, 0), (0, 1) };
        Assert.That(() => new IndexedReadOnlyDictionary<string, int>(_keys, _values, pairs), Throws.ArgumentException);
    }

    [Test]
    public void ContainsKey_ShouldReturnTrueOrFalse()
    {
        var pairs = new List<(int keyIndex, int valueIndex)> { (0, 0), (1, 1) };
        var dict = new IndexedReadOnlyDictionary<string, int>(_keys, _values, pairs);

        Assert.That(dict.ContainsKey("A"), Is.True);
        Assert.That(dict.ContainsKey("C"), Is.False);
    }

    [Test]
    public void TryGetValue_ShouldReturnCorrectValue()
    {
        var pairs = new List<(int keyIndex, int valueIndex)> { (0, 0), (1, 1) };
        var dict = new IndexedReadOnlyDictionary<string, int>(_keys, _values, pairs);

        Assert.That(dict.TryGetValue("B", out int value), Is.True);
        Assert.That(value, Is.EqualTo(20));

        Assert.That(dict.TryGetValue("C", out _), Is.False);
    }

    [Test]
    public void Indexer_ShouldReturnCorrectValueOrThrow()
    {
        var pairs = new List<(int keyIndex, int valueIndex)> { (0, 0), (1, 1) };
        var dict = new IndexedReadOnlyDictionary<string, int>(_keys, _values, pairs);

        Assert.That(dict["A"], Is.EqualTo(10));
        Assert.That(() => { var _ = dict["C"]; }, Throws.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public void KeysAndValues_ShouldEnumerateCorrectly()
    {
        var pairs = new List<(int keyIndex, int valueIndex)> { (0, 0), (2, 2) };
        var dict = new IndexedReadOnlyDictionary<string, int>(_keys, _values, pairs);

        Assert.That(dict.Keys, Is.EqualTo(new[] { "A", "C" }));
        Assert.That(dict.Values, Is.EqualTo(new[] { 10, 30 }));
    }

    [Test]
    public void EmptyPairs_ShouldResultInEmptyDictionary()
    {
        var dict = new IndexedReadOnlyDictionary<string, int>(_keys, _values, Array.Empty<int>());

        Assert.That(dict.Count, Is.EqualTo(0));
        Assert.That(dict.ContainsKey("A"), Is.False);
    }

    [Test]
    public void TryGetValue_MaskedKey_ShouldReturnFalse()
    {
        var pairs = new List<(int keyIndex, int valueIndex)> { (0, 2), (2, 2) }; // Keys "A" and "C" map to value 30
        var dict = new IndexedReadOnlyDictionary<string, int>(_keys, _values, pairs);

        // "B" is not included in the pairs
        Assert.That(dict.TryGetValue("B", out _), Is.False);
        // "B" is still not included after index map update
        Assert.That(dict.TryGetValue("B", out _), Is.False);
    }

    [Test]
    public void TryGetValue_EmptyDictionary_ShouldReturnFalse()
    {
        var dict = new IndexedReadOnlyDictionary<string, int>(_keys, _values, Array.Empty<int>());

        Assert.That(dict.TryGetValue("A", out _), Is.False);
    }
}
