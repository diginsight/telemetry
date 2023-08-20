using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common
{
    public interface IParallelService
    {
        int LowConcurrency { get; }
        int MediumConcurrency { get; }
        int HighConcurrency { get; }

        void ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource> body);

        Task ForEachAsync<TSource>(IEnumerable<TSource> source, ParallelOptions options, Func<TSource, Task> body);

        Task WhenAllAsync(IEnumerable<Func<Task>> taskFactories, ParallelOptions parallelOptions);
    }
}
