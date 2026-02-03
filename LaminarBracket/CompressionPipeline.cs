using System.Collections.Generic;
using System.Linq;

public class PipelineCompressor : ICompressor
{
    private readonly ICompressor[] _stages;
    public string Name => string.Join(" + ", _stages.Select(s => s.Name));
    
    public PipelineCompressor(params ICompressor[] stages)
    {
        _stages = stages;
    }
    
    public byte[] Compress(byte[] data)
    {
        foreach (var stage in _stages)
        {
            data = stage.Compress(data);
        }
        return data;
    }
    
    public byte[] Decompress(byte[] data)
    {
        for (int i = _stages.Length - 1; i >= 0; i--)
        {
            data = _stages[i].Decompress(data);
        }
        return data;
    }
}
