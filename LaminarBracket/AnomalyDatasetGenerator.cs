using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class AnomalyDatasetGenerator
{
    private static readonly string DataRoot = Path.Combine("..", "..", "..", "..");
    private static readonly string OutputDir = Path.Combine("..", "..", "..", "DWT", "created_data");
    private const int ChunkSize = 1024; 
    
    public static void Run()
    {
        var files = new[]
        {
            "chat_data_for_csharp.txt",
            "minecraft_dataset.bin",
            "twitch_chat_raw.txt"
        };

        var compressor = new PipelineCompressor(
            new LzssCompressor(8192, 255, lazyMatching: true),
            new ArithmeticCompressor()
        ); // LZSS + Arithmetic

        var allResults = new List<ChunkCompressionResult>();

        foreach (var filename in files)
        {
            string fullPath = Path.Combine(DataRoot, filename);
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"File not found: {fullPath}");
                continue;
            }

            Console.WriteLine($"\nProcessing {filename}");
            byte[] data = File.ReadAllBytes(fullPath);
            
            var results = ProcessFile(data, filename, compressor);
            allResults.AddRange(results);
            
            Console.WriteLine($"Total chunks: {results.Count}");
            Console.WriteLine($"Avg compression ratio: {results.Average(r => r.CompressionRatio):F4}%");
        }

        // Calculam 1% threshold pentru anomalii 0.5 - 99 - 0.5, echivalent cu 0.5 percentile/centile
        var ratios = allResults.Select(r => r.CompressionRatio).OrderBy(x => x).ToList();
        double minThreshold = ratios[(int)(ratios.Count * 0.005)];
        double maxThreshold = ratios[(int)(ratios.Count * 0.995)];

        int normalCount = 0;
        int abnormalCount = 0;

        foreach (var result in allResults)
        {
            result.IsAbnormal = result.CompressionRatio < minThreshold || 
                                result.CompressionRatio > maxThreshold;
            if (result.IsAbnormal) abnormalCount++;
            else normalCount++;
        }

        Console.WriteLine($"\nNormal chunks: {normalCount}");
        Console.WriteLine($"Abnormal chunks: {abnormalCount}");

        Directory.CreateDirectory(OutputDir);
        string outputPath = Path.Combine(OutputDir, "compression_dataset.csv");
        SaveLabeledDataset(allResults, outputPath);
        
        Console.WriteLine("\nDataset generation complete");
    }

    private static List<ChunkCompressionResult> ProcessFile(
        byte[] data, string filename, ICompressor compressor)
    {
        var results = new List<ChunkCompressionResult>();
        int chunkIndex = 0;

        for (int i = 0; i < data.Length; i += ChunkSize)
        {
            int size = Math.Min(ChunkSize, data.Length - i);
            byte[] chunk = new byte[size];
            Array.Copy(data, i, chunk, 0, size);

            try
            {
                byte[] compressed = compressor.Compress(chunk);
                double ratio = (double)compressed.Length / chunk.Length * 100.0;

                results.Add(new ChunkCompressionResult
                {
                    SourceFile = filename,
                    ChunkIndex = chunkIndex++,
                    OriginalSize = chunk.Length,
                    CompressedSize = compressed.Length,
                    CompressionRatio = ratio,
                    OriginalData = chunk,
                    CompressedData = compressed
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error compressing chunk {chunkIndex}: {ex.Message}");
            }
        }

        return results;
    }

    private static void SaveLabeledDataset(
        List<ChunkCompressionResult> results, string outputFile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SourceFile,ChunkIndex,OriginalSize,CompressedSize,CompressionRatio,IsAbnormal");

        foreach (var result in results)
        {
            sb.AppendLine($"{result.SourceFile},{result.ChunkIndex}," +
                         $"{result.OriginalSize},{result.CompressedSize}," +
                         $"{result.CompressionRatio:F4},{(result.IsAbnormal ? 1 : 0)}");
        }

        File.WriteAllText(outputFile, sb.ToString());
        Console.WriteLine($"\nLabeled dataset saved to: {outputFile}");
    }
}

public class ChunkCompressionResult
{
    public string SourceFile { get; set; }
    public int ChunkIndex { get; set; }
    public int OriginalSize { get; set; }
    public int CompressedSize { get; set; }
    public double CompressionRatio { get; set; }
    public bool IsAbnormal { get; set; }
    public byte[] OriginalData { get; set; }
    public byte[] CompressedData { get; set; }
}

