using MvtMesherCore;
using MvtMesherCore.Mapbox;
using MvtMesherCore.Models;
using VectorTile = MvtMesherCore.Mapbox.VectorTile;

namespace Tests.Protobuf;

[TestFixture]
public class PbfTests
{
    //[Ignore("Comparison test using MvtMesherCore parsing against expected JSON")]
    [TestCase(Constants.TestInputFolder, Constants.AtlanticPbfFile, Constants.AtlanticJsonFile)]
    [TestCase(Constants.TestInputFolder, Constants.EnoshimaPbfFile, Constants.EnoshimaJsonFile)]
    public void PbfFileShouldParseWithEquivalenceToJson(string inFolder, string pbfPath, string jsonPath)
    {
        using var stream = File.OpenRead(Path.Combine(inFolder, pbfPath));
        using var byteReader = new BinaryReader(stream);
        var numBytes = stream.Length;
        Assert.That(numBytes, Is.LessThan(int.MaxValue));
        var bytes = byteReader.ReadBytes((int)numBytes);
        var vectorTile = VectorTile.FromByteArray(bytes, CanonicalTileId.FromDelimitedPatternInString(pbfPath, '-'), Constants.ReadSettings);
        
        var expectedJson = Path.Combine(inFolder, jsonPath).LoadAsMvtJson();
        JsonUtility.AssertEquivalency(vectorTile, expectedJson);
    }
}