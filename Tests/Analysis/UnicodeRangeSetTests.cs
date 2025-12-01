
using MvtMesherCore.Analysis.Language;
using MvtMesherCore.Models;
using Tests.Protobuf;

namespace Tests.Analysis;


[TestFixture]
public class UnicodeRangeSetTests
{
    [Test]
    public void Add_SingleCodePoint_ShouldCreateSingleRange()
    {
        var set = new UnicodeRangeSet();
        var result = set.Add(0x0041); // 'A'
        Assert.That(result, Is.True);
        Assert.That(set.Count, Is.EqualTo(1));
        Assert.That(set[0].Start, Is.EqualTo(0x0041));
        Assert.That(set[0].End, Is.EqualTo(0x0041));
    }

    [Test]
    public void Add_AdjacentCodePoints_ShouldMergeIntoSingleRange()
    {
        var set = new UnicodeRangeSet();
        set.Add(0x0041);
        set.Add(0x0042);
        Assert.That(set.Count, Is.EqualTo(1));
        Assert.That(set[0].Start, Is.EqualTo(0x0041));
        Assert.That(set[0].End, Is.EqualTo(0x0042));
    }

    [Test]
    public void Add_NonAdjacentCodePoints_ShouldCreateSeparateRanges()
    {
        var set = new UnicodeRangeSet();
        set.Add(0x0041);
        set.Add(0x0061);
        Assert.That(set.Count, Is.EqualTo(2));
        Assert.That(set[0].Start, Is.EqualTo(0x0041));
        Assert.That(set[1].Start, Is.EqualTo(0x0061));
    }

    [Test]
    public void AddCharactersFromSpan_ShouldHandleSurrogatePairs()
    {
        var set = new UnicodeRangeSet();
        var text = "A\uD83D\uDE00"; // 'A' + 😀 (U+1F600)
        var addedCount = set.AddCharactersFromSpan(text.AsSpan());
        Assert.That(addedCount, Is.EqualTo(2));
        Assert.That(set.CodePointCount, Is.EqualTo(2));
        Assert.That(set.Count, Is.EqualTo(2));
        Assert.That(set[1].Start, Is.EqualTo(0x1F600));
    }

    [Test]
    public void EnumerateRangesHex_ShouldReturnCorrectFormat()
    {
        var set = new UnicodeRangeSet();
        set.Add(0x0041);
        set.Add(0x0042);
        var hexRanges = set.EnumerateRangesHex().ToList();
        Assert.That(hexRanges[0], Is.EqualTo("0041-0042"));
    }

    [Test]
    public void CharCount_ShouldReturnTotalCharacters()
    {
        var set = new UnicodeRangeSet
        {
            0x0041,
            0x0042,
            0x0061
        };
        Assert.That(set.CodePointCount, Is.EqualTo(3));
    }

    [Test]
    public void Equals_SameRanges_ShouldReturnTrue()
    {
        var set1 = new UnicodeRangeSet
        {
            0x0041,
            0x0042
        };

        var set2 = new UnicodeRangeSet
        {
            0x0041,
            0x0042
        };

        Assert.That(set1.Equals(set2), Is.True);
    }

    [Test]
    public void Equals_DifferentRanges_ShouldReturnFalse()
    {
        var set1 = new UnicodeRangeSet
        {
            0x0041,
            0x0042
        };
        var set2 = new UnicodeRangeSet
        {
            0x0041,
            0x0061
        };
        Assert.That(set1.Equals(set2), Is.False);
    }

    [Test]
    public void Simplify_ShouldReduceNumberOfRanges()
    {
        var set = new UnicodeRangeSet();
        for (int i = 0; i < 100; i += 2)
        {
            set.Add(i);
        }
        var (simplifiedSet, numMerged) = set.Simplify(2);
        Assert.That(simplifiedSet.Count, Is.LessThan(set.Count));
        Assert.That(numMerged, Is.GreaterThan(0));
    }

    [Test]
    public void UnionWith_ShouldCombineRanges()
    {
        var set1 = new UnicodeRangeSet
        {
            0x0041,
            0x0042
        };
        var set2 = new UnicodeRangeSet
        {
            0x0042,
            0x0061
        };
        set1.UnionWith(set2);
        Assert.That(set1.Count, Is.EqualTo(2));
        Assert.That(set1[0].Start, Is.EqualTo(0x0041));
        Assert.That(set1[1].Start, Is.EqualTo(0x0061));
    }

    [TestCase(Constants.TestInputFolder, Constants.EnoshimaPbfFile, Constants.EnoshimaJsonFile, Constants.TestOutputFolder)]
    [TestCase(Constants.TestInputFolder, Constants.AtlanticPbfFile, Constants.AtlanticJsonFile, Constants.TestOutputFolder)]
    public void TabulateStringPropertyCharSets_FromVectorTile_ShouldReturnExpectedResults(string inFolder, string pbfFile, string jsonFile, string outFolder)
    {
        using var stream = File.OpenRead(Path.Combine(inFolder, pbfFile));
        using var byteReader = new BinaryReader(stream);
        var numBytes = stream.Length;
        Assert.That(numBytes, Is.LessThan(int.MaxValue));
        var bytes = byteReader.ReadBytes((int)numBytes);
        var tileId = CanonicalTileId.FromDelimitedPatternInString(pbfFile, '-');
        var vectorTile = MvtMesherCore.Mapbox.VectorTile.FromByteArray(bytes, tileId, Constants.ReadSettings);
        var charSets = vectorTile.TabulateStringPropertyCharSets(Constants.LabelRegex);
        Assert.That(charSets.Values.All(set => set.CodePointCount > 0), Is.True, "All character sets should have at least one character");

        var expectedJson = Path.Combine(inFolder, jsonFile).LoadAsMvtJson();

        foreach (var layerKvp in expectedJson.Layers)
        {
            var layerName = layerKvp.Key;
            var layer = layerKvp.Value;
            foreach (var feature in layer.Features)
            {
                // Ensure at least one label property exists
                foreach (var property in feature.Properties)
                {
                    if (Constants.LabelRegex.IsMatch(property.Key))
                    {
                        var testCharSet = UnicodeRangeSet.FromSpan(property.Value);
                        var comparisonCharSet = charSets[property.Key];
                        Assert.That(testCharSet.IsSubsetOf(comparisonCharSet), Is.True, $"Character set for property '{property.Key}' in layer '{layerName}' is not a subset of the tabulated set");
                    }
                }
            }
        }

        TestContext.Out.WriteLine($"Character sets for tile {pbfFile}: {charSets.Count} properties, total unique code points: {charSets.Values.Sum(set => set.CodePointCount)}");
        using var outJsonFile = File.Open(Path.Combine(outFolder, Path.GetFileNameWithoutExtension(jsonFile) + "_charsets.json"), FileMode.Create, FileAccess.Write);
        using var jsonWriter = new StreamWriter(outJsonFile);
        Newtonsoft.Json.JsonSerializer serializer = new()
        {
            Formatting = Newtonsoft.Json.Formatting.Indented
        };
        serializer.Serialize(jsonWriter, MvtCharsetJson.FromDictionary(tileId.ToShortString(), charSets));
    }
}
