using Microsoft.Extensions.Logging;

namespace Diginsight.AspNetCore;

internal sealed class LogLevelBox
{
    private readonly LinkedList<LogLevel> list = new ();
    private readonly object syncRoot = new ();

    public LogLevel Value
    {
        get
        {
            lock (syncRoot)
            {
                return list.Last?.Value ?? LogLevel.None;
            }
        }
    }

    public LogLevelBox(LogLevel? value = null)
    {
        if (value is { } logLevel)
        {
            list.AddLast(logLevel);
        }
    }

    public IDisposable Push(LogLevel value)
    {
        lock (syncRoot)
        {
            void Insert()
            {
                for (LinkedListNode<LogLevel>? current = list.Last; current is not null; current = current.Previous)
                {
                    if (value > current.Value)
                        continue;
                    list.AddAfter(current, value);
                    return;
                }

                list.AddFirst(value);
            }

            Insert();
            return new Popper(this, value);
        }
    }

    private sealed class Popper : IDisposable
    {
        private readonly LogLevelBox box;
        private readonly LogLevel value;

        public Popper(LogLevelBox box, LogLevel value)
        {
            this.box = box;
            this.value = value;
        }

        public void Dispose()
        {
            lock (box.syncRoot)
            {
                box.list.Remove(value);
            }
        }
    }
}
