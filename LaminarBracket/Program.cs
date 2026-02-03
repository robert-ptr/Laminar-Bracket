using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

public class Program
{
    private static string[] DatasetFiles = new[]
    {
        "chat_data_for_csharp.txt",
        "twitch_chat_raw.txt",
        "esa_telemetry.bin", 
        "minecraft_dataset.bin"
    };

    private const string DataRoot = "/home/robert/Desktop/coding_projects/csharp/Laminar_Bracket/";
    private const int MaxSizeBytes = 1024 * 1024; // 1MB

    public static void Main()
    {
        Console.WriteLine("Starting Benchmark.");
        
        var sb = new StringBuilder();
        sb.AppendLine("Benchmark Results");
        sb.AppendLine();
        sb.AppendLine("| Dataset | Algorithm | Original Size | Compressed Size | Ratio (%) | Time (ms) | Speed (MB/s) |");
        sb.AppendLine("|---|---|---|---|---|---|---|");

        Console.WriteLine("\n| Dataset | Algorithm | Original Size | Compressed Size | Ratio (%) | Time (ms) | Speed (MB/s) |");
        Console.WriteLine("|---|---|---|---|---|---|---|");

        var pipelines = new List<ICompressor>();

        // Base case
        pipelines.Add(new HuffmanCompressor());
        pipelines.Add(new ArithmeticCompressor());

        // LZSS(Greedy)
        // 8KB
        pipelines.Add(new PipelineCompressor(new LzssCompressor(8192, 255, lazyMatching: false), new HuffmanCompressor()));
        pipelines.Add(new PipelineCompressor(new LzssCompressor(8192, 255, lazyMatching: false), new ArithmeticCompressor()));
        pipelines.Add(new PipelineCompressor(new LzssCompressor(8192, 255, lazyMatching: false)));
        // 64KB
        pipelines.Add(new PipelineCompressor(new LzssCompressor(65535, 255, lazyMatching: false), new HuffmanCompressor()));
        pipelines.Add(new PipelineCompressor(new LzssCompressor(65535, 255, lazyMatching: false), new ArithmeticCompressor()));
        pipelines.Add(new PipelineCompressor(new LzssCompressor(65535, 255, lazyMatching: false)));
        // LZSS (Lazy)
        // 8KB
        pipelines.Add(new PipelineCompressor(new LzssCompressor(8192, 255, lazyMatching: true), new HuffmanCompressor()));
        pipelines.Add(new PipelineCompressor(new LzssCompressor(8192, 255, lazyMatching: true), new ArithmeticCompressor()));
        pipelines.Add(new PipelineCompressor(new LzssCompressor(8192, 255, lazyMatching: true)));
        // 64KB
        pipelines.Add(new PipelineCompressor(new LzssCompressor(65535, 255, lazyMatching: true), new HuffmanCompressor()));
        pipelines.Add(new PipelineCompressor(new LzssCompressor(65535, 255, lazyMatching: true), new ArithmeticCompressor()));
        pipelines.Add(new PipelineCompressor(new LzssCompressor(65535, 255, lazyMatching: true)));

        // BWT
        pipelines.Add(new PipelineCompressor(new BurrowsWheeler(), new HuffmanCompressor()));
        pipelines.Add(new PipelineCompressor(new BurrowsWheeler(), new ArithmeticCompressor()));

        foreach (var filename in DatasetFiles)
        {
            try 
            {
                string fullPath = Path.Combine(DataRoot, filename);
                if (!File.Exists(fullPath)) continue;

                Console.WriteLine($"Loading {filename}..."); 
                byte[] originalData = LoadData(fullPath);
                string datasetName = filename;

                foreach (var compressor in pipelines)
                {
                    try
                    {
                        var resultLine = RunBenchmark(datasetName, compressor, originalData);
                        Console.WriteLine(resultLine);
                        sb.AppendLine(resultLine);
                    }
                    catch (Exception ex)
                    {
                        string err = $"| {datasetName} | {compressor.Name} | {originalData.Length} | *ERROR* | - | - | - |";
                        Console.WriteLine(err);
                        sb.AppendLine(err);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        string outputFile = "benchmark_results.md";
        File.WriteAllText(outputFile, sb.ToString());
        Console.WriteLine($"\nResults saved to {outputFile}");
    }

    private static byte[] LoadData(string path)
    {
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            long len = Math.Min(fs.Length, MaxSizeBytes);
            byte[] buffer = new byte[len];
            int read = fs.Read(buffer, 0, (int)len);
            if (read < len) Array.Resize(ref buffer, read);
            return buffer;
        }
    }

    private static string RunBenchmark(string dataset, ICompressor compressor, byte[] original)
    {
        GC.Collect();
        var sw = Stopwatch.StartNew();
        byte[] compressed = compressor.Compress(original);
        sw.Stop();
        double encodeTime = sw.Elapsed.TotalMilliseconds;

        double ratio = (double)compressed.Length / original.Length * 100.0;
        double speed = (original.Length / 1024.0 / 1024.0) / (encodeTime / 1000.0);

        return $"| {dataset} | {compressor.Name} | {original.Length} | {compressed.Length} | {ratio:F2}% | {encodeTime:F2} | {speed:F2} |";
    }
}