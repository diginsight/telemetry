namespace Diginsight.Diagnostics.TextWriting;

internal sealed class BreakingMessageLineResizer : IMessageLineResizer
{
    private readonly int maxLength;

    public BreakingMessageLineResizer(int maxLength)
    {
        this.maxLength = maxLength;
    }

    public IEnumerable<string> Resize(IEnumerable<string> lines)
    {
        string[] inputLines = lines.ToArray();

        for (int i = 0; i < inputLines.Length; i++)
        {
            string line = inputLines[i];

            if (line.Length <= maxLength)
            {
                yield return line;
            }
            else
            {
                yield return $"{line[..(maxLength - 1)]}↓";
                inputLines[i] = line[(maxLength - 1)..];

                i--;
            }
        }
    }
}
