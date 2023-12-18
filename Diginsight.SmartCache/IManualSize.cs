namespace Diginsight.SmartCache;

public interface IManualSize
{
    (long Sz, bool Fxd) GetSize(Func<object?, (long Sz, bool Fxd)> innerGetSize);
}
