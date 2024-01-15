namespace Diginsight.Diagnostics.TextWriting;

public sealed class CategoryToken : ILineToken
{
    public int? Length { get; set; }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(new CategoryAppender(Length));
    }

    public ILineToken Clone() => new CategoryToken() { Length = Length };
}
