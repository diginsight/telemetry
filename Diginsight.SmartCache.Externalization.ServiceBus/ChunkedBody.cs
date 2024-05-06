namespace Diginsight.SmartCache.Externalization.ServiceBus;

internal sealed class ChunkedBody : IDisposable
{
    private readonly ManualResetEventSlim mre = new ();
    private Chunk[]? chunks;
    private int chunkCount = -1;

    public DateTimeOffset Timestamp { get; }

    public ChunkedBody(DateTimeOffset timestamp)
    {
        Timestamp = timestamp;
    }

    public byte[] Get(CancellationToken cancellationToken)
    {
        mre.Wait(cancellationToken);

        long fullLength = 0;
        int chunkCount = chunks!.Length;
        byte[][] bodies = new byte[chunkCount][];
        for (int i = 0; i < chunkCount; i++)
        {
            byte[] body = chunks[i].Get(cancellationToken);
            fullLength += body.LongLength;
            bodies[i] = body;
        }

        long partialLength = 0;
        byte[] fullBody = new byte[fullLength];
        for (int i = 0; i < chunkCount; i++)
        {
            byte[] body = bodies[i];
            long length = body.Length;
            Array.Copy(body, 0, fullBody, partialLength, length);
            partialLength += length;
        }

        return fullBody;
    }

    // ReSharper disable once ParameterHidesMember
    public bool Set(byte[] body, int chunkIndex, int chunkCount)
    {
        Interlocked.CompareExchange(ref this.chunkCount, chunkCount, -1);

        // ReSharper disable once LocalVariableHidesMember
        Chunk[] chunks = LazyInitializer.EnsureInitialized(
#if NET
            ref this.chunks,
#else
            ref this.chunks!,
#endif
            () => Enumerable.Range(0, chunkCount).Select(static _ => new Chunk()).ToArray()
        );
        bool decrement = chunks[chunkIndex].Set(body);

        mre.Set();

        return (decrement ? Interlocked.Decrement(ref this.chunkCount) : this.chunkCount) == 0;
    }

    public void Dispose()
    {
        mre.Dispose();

        if (chunks is null)
            return;

        foreach (Chunk chunk in chunks)
        {
            chunk.Dispose();
        }
    }

    private sealed class Chunk : IDisposable
    {
        private readonly ManualResetEventSlim mre = new ();
        private byte[]? body;

        public byte[] Get(CancellationToken cancellationToken)
        {
            mre.Wait(cancellationToken);
            return body!;
        }

        // ReSharper disable once ParameterHidesMember
        public bool Set(byte[] body)
        {
            if (this.body is not null)
                return false;

            this.body = body;
            mre.Set();
            return true;
        }

        public void Dispose()
        {
            mre.Dispose();
        }
    }
}
