using MvtMesherCore;
using MvtMesherCore.Mapbox;
using MvtMesherCore.Models;
using VectorTile = MvtMesherCore.Mapbox.VectorTile;

namespace Tests.Protobuf;

[TestFixture]
public class PbfTests
{
    //[Ignore("Comparison test using MvtMesherCore parsing against expected JSON")]
    [TestCase(Constants.AtlanticPbfPath, Constants.AtlanticJsonPath)]
    [TestCase(Constants.EnoshimaPbfPath, Constants.EnoshimaJsonPath)]
    public void PbfFileShouldParseWithEquivalenceToJson(string pbfPath, string jsonPath)
    {
        using var stream = File.OpenRead(pbfPath);
        using var byteReader = new BinaryReader(stream);
        var numBytes = stream.Length;
        Assert.That(numBytes, Is.LessThan(int.MaxValue));
        var bytes = byteReader.ReadBytes((int)numBytes);
        var vectorTile = VectorTile.FromByteArray(bytes, CanonicalTileId.FromDelimitedPatternInString(pbfPath, '-'), Constants.ReadSettings);
        
        var expectedJson = jsonPath.LoadAsMvtJson();
        JsonUtility.AssertEquivalency(vectorTile, expectedJson);
    }
}