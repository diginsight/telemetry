namespace Diginsight.SmartCache.Externalization;

public interface ICachePayloadHolder
{
    string GetAsString();

    byte[] GetAsBytes();
}
