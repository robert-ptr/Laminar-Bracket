using System.Text;

public class BurrowsWheeler
{
    private const char Sentinel = '\u0003';

    public string Transform(string s)
    {
        string text = s + Sentinel;
        int n = text.Length;
        var rotations = new List<string>();

        for (int i = 0; i < n; i++) // generare rotatii ciclice
        {
            rotations.Add(text.Substring(i) + text.Substring(0, i));
        }
        
        rotations.Sort(StringComparer.Ordinal);

        StringBuilder lastColumn = new StringBuilder();
        foreach (var rotation in rotations)
        {
            lastColumn.Append(rotation[n - 1]);
        }
        
        return lastColumn.ToString();
    }

    public string InverseTransform(string bwtText)
    {
        int n = bwtText.Length;
        List<String> table = Enumerable.Repeat("", n).ToList();

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                table[j] = bwtText[j] + table[j];
            }
            table.Sort(StringComparer.Ordinal);
        }

        string result = table.First(s => s.EndsWith(Sentinel.ToString()));
        return result.TrimEnd(Sentinel);
    }
}