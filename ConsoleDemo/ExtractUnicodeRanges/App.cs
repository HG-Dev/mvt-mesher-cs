using MvtMesherCore.Mapbox;
using MvtMesherCore.Models;
using MvtMesherCore.Analysis.Language;
using ConsoleAppFramework;

const int MaxBytes = 50 * 1024 * 1024; // 50 MB

///../../Tests/Res

ConsoleApp.Run(args, (string inDir, string outDir) =>
{
    var dirInfo = new DirectoryInfo(inDir);
    if (!dirInfo.Exists)
    {
        Console.Error.WriteLine($"Folder '{inDir}' does not exist.");
        return;
    }

    var outDirInfo = new DirectoryInfo(outDir);
    if (!outDirInfo.Exists)
    {
        outDirInfo.Create();
    }

    Console.WriteLine($"Working folder: {inDir}");
    
    // Read Mapbox Vector Tile PBF files
    var pbfFiles = dirInfo.EnumerateFiles()
        .Where(f => f.Extension.Equals(".pbf", StringComparison.OrdinalIgnoreCase))
        .ToList();

    var readSettings = new VectorTile.ReadSettings()
    {
        ValidationLevel = PbfValidation.Standard,
        ScaleToLayerExtents = false
    };

    var maxBytes = MaxBytes;
    List<VectorTile> vectorTiles = new();
    foreach (var pbfFile in pbfFiles)
    {
        using var stream = pbfFile.OpenRead();
        using var byteReader = new BinaryReader(stream);
        var numBytes = stream.Length;
        if (numBytes >= (long)int.MaxValue)
        {
            Console.Error.WriteLine($"File '{pbfFile.FullName}' is too large to process.");
            continue;
        }
        maxBytes -= (int)numBytes;
        if (maxBytes < 0)
        {
            Console.Error.WriteLine($"Reached maximum byte limit when processing file '{pbfFile.FullName}'. Stopping further processing.");
            break;
        }
        var bytes = byteReader.ReadBytes((int)numBytes);
        var tileId = CanonicalTileId.FromDelimitedPatternInString(pbfFile.Name, '-');
        var vectorTile = VectorTile.FromByteArray(bytes, tileId, readSettings);
        vectorTiles.Add(vectorTile);
    }

    Console.WriteLine($"Opened {vectorTiles.Count} vector tiles. Total bytes processed: {MaxBytes - maxBytes}");
    Console.WriteLine($"Creating dictionaries...");
    Dictionary<string, UnicodeRangeSet> layerPropertyCharSets = new();
    var regex = new System.Text.RegularExpressions.Regex(@"name|label|text", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    foreach (var tile in vectorTiles)
    {
        tile.TabulateStringPropertyCharSets(regex, layerPropertyCharSets);
    }

    while (true)
    {
        Console.WriteLine("status | merge <a,b,...> | remove <a> OR <code_point_highpass> | simplify <tolerance> | export | exit");
        Console.Write("> ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }
        input = input.Trim();
        var elements = input.Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var command = elements[0].ToLowerInvariant();
        
        switch (command)
        {
            case "status":
            {
                foreach (var kvp in layerPropertyCharSets)
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value.Count} ranges, {kvp.Value.CodePointCount} code points");
                    Console.WriteLine(kvp.Value.VisualizeRanges(UnicodeRangeSetExtensions.Common));
                }
                break;
            }
            case "merge" when elements.Length >= 3:
            {
                if (string.IsNullOrWhiteSpace(elements[1]) || string.IsNullOrWhiteSpace(elements[2]))
                {
                    Console.WriteLine($"Empty property names: <{elements[1]}>, <{elements[2]}>");
                    break;
                }
                var primarySetName = elements[1];
                if (!layerPropertyCharSets.TryGetValue(primarySetName, out var primarySet))
                {
                    Console.WriteLine($"Property '{primarySetName}' not found.");
                    break;
                }

                Dictionary<string, UnicodeRangeSet> setsToMerge = new();
                foreach (var propName in elements[2..])
                {
                    if (!layerPropertyCharSets.TryGetValue(propName, out var set))
                    {
                        Console.WriteLine($"Property '{propName}' not found.");
                        break;
                    }
                    setsToMerge.Add(propName, set);
                }

                foreach (var kvp in setsToMerge)
                {
                    if (kvp.Key == primarySetName)
                        continue;
                    primarySet.UnionWith(kvp.Value);
                    layerPropertyCharSets.Remove(kvp.Key);
                }
                Console.Write("Merged set name (empty to keep first): ");
                var renameInput = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(renameInput) && renameInput != primarySetName)
                {
                    layerPropertyCharSets[renameInput] = primarySet;
                    Console.WriteLine($"Merged into new property '{renameInput}'.");
                    layerPropertyCharSets.Remove(primarySetName);
                    break;
                }
                layerPropertyCharSets[primarySetName] = primarySet;
            
                break;
            }
            case "remove" when int.TryParse(elements[1], out var highpass) && highpass >= 0:
            {
                var removed = 0;
                foreach (var key in layerPropertyCharSets.Keys.ToList())
                {
                    var existingSet = layerPropertyCharSets[key];
                    if (existingSet.CodePointCount <= highpass)
                    {
                        removed++;
                        layerPropertyCharSets.Remove(key);
                    }
                }
                Console.WriteLine($"Removed {removed} properties with code point count below {highpass}.");
                break;
            }
            case "remove" when !string.IsNullOrWhiteSpace(elements[1]) 
                && layerPropertyCharSets.TryGetValue(elements[1], out var set):
            {
                    layerPropertyCharSets.Remove(elements[1]);
                    Console.WriteLine($"Removed property '{elements[1]}'.");
                    break;
            }
            case "simplify" when int.TryParse(elements[1], out var tolerance) && tolerance >= 0:
            {
                var mergedRanges = 0;
                foreach (var key in layerPropertyCharSets.Keys.ToList())
                {
                    var (simplifiedSet, merged) = layerPropertyCharSets[key].Simplify(tolerance);
                    mergedRanges += merged;
                    layerPropertyCharSets[key] = simplifiedSet;
                }
                if (mergedRanges == 0)
                {
                    Console.WriteLine("No ranges were merged during simplification.");
                    break;
                }
                Console.WriteLine($"Simplified all properties: merged a total of {mergedRanges} ranges.");
                break;
            }
            case "export":
            {
                var exportFilePath = Path.Combine(outDir, "UnicodeRangeSets.txt");
                using var writer = new StreamWriter(exportFilePath, false);
                foreach (var kvp in layerPropertyCharSets)
                {
                    writer.WriteLine($"Property: {kvp.Key}");
                    writer.WriteLine($"Ranges ({kvp.Value.Count}), Code Points: {kvp.Value.CodePointCount}");
                    var ranges = string.Join(',', kvp.Value.EnumerateRangesHex());
                    writer.WriteLine(ranges);
                    writer.WriteLine();
                }
                Console.WriteLine($"Exported Unicode range sets to '{exportFilePath}'.");
                break;
            }
            default:
                break;            
        }
    }
});