namespace Diginsight.Diagnostics.TextWriting;

public interface ILineToken
{
    void Apply(ref MutableLineDescriptor lineDescriptor);

    ILineToken Clone();
}
