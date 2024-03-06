namespace Diginsight.SmartCache.Externalization.ServiceBus;

internal sealed class ChunkedBody : IDisposable
{
    private readonly ManualResetEventSlim mre = new ();
    private Chunk[]? chunks;

    public DateTime Timestamp { get; }

    public ChunkedBody(DateTime timestamp)
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

        long lengthSoFar = 0;
        byte[] fullBody = new byte[fullLength];
        for (int i = 0; i < chunkCount; i++)
        {
            byte[] body = bodies[i];
            long length = body.Length;
            Array.Copy(body, 0, fullBody, lengthSoFar, length);
            lengthSoFar += length;
        }

        return fullBody;
    }

    // ReSharper disable once ParameterHidesMember
    public void Set(byte[] body, int chunkIndex, int chunkCount)
    {
        chunks ??= Enumerable.Range(0, chunkCount).Select(static _ => new Chunk()).ToArray();
        chunks[chunkIndex].Set(body);

        mre.Set();
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
        public void Set(byte[] body)
        {
            if (this.body is not null)
                return;

            this.body = body;
            mre.Set();
        }

        public void Dispose()
        {
            mre.Dispose();
        }
    }
}
