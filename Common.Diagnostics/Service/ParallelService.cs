using Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Common
{
    public class ParallelService : IParallelService
    {
        private const int LOWCONCURRENCY_DEFAULT = 3;
        private const int MEDIUMCONCURRENCY_DEFAULT = 6;
        private const int HIGHCONCURRENCY_DEFAULT = 12;

        private readonly ILogger<ParallelService> logger;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IParallelServiceOptions options;

        private int? lowConcurrency;
        private int? mediumConcurrency;
        private int? highConcurrency;

        public int LowConcurrency
        {
            get
            {
                if (lowConcurrency != null) { return lowConcurrency.Value; }
                lowConcurrency = options.LowConcurrency > 0 ? options.LowConcurrency : LOWCONCURRENCY_DEFAULT;
                return lowConcurrency.Value;
            }
        }
        public int MediumConcurrency
        {
            get
            {
                if (mediumConcurrency != null) { return mediumConcurrency.Value; }
                mediumConcurrency = options.MediumConcurrency > 0 ? options.MediumConcurrency : MEDIUMCONCURRENCY_DEFAULT;
                return mediumConcurrency.Value;
            }
        }
        public int HighConcurrency
        {
            get
            {
                if (highConcurrency != null) { return highConcurrency.Value; }
                highConcurrency = options.HighConcurrency > 0 ? options.HighConcurrency : HIGHCONCURRENCY_DEFAULT;
                return highConcurrency.Value;
            }
        }

        public ParallelService(ILogger<ParallelService> logger,
                               IHttpContextAccessor httpContextAccessor,
                               IOptions<ParallelServiceOptions> options)
        {
            this.logger = logger;
            this.options = options.Value;
            this.httpContextAccessor = httpContextAccessor;
        }

        public void ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource> body)
        {
            using (var scope = logger.BeginMethodScope(new Func<object>(() => new { source = source.GetLogString(), parallelOptions = parallelOptions.GetLogString() })))
            {
                if (source?.Any() != true) { return; }

                SetMaxConcurrency(parallelOptions, scope);

                Parallel.ForEach(source, parallelOptions ?? new ParallelOptions(), body);
            }
        }

        public async Task ForEachAsync<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TSource, Task> body)
        {
            using (var scope = logger.BeginMethodScope(new Func<object>(() => new { source = source.GetLogString(), parallelOptions = parallelOptions.GetLogString() })))
            {
                if (source?.Any() != true) { return; }

                SetMaxConcurrency(parallelOptions, scope);

                try
                {
#if NET6_0_OR_GREATER
                    await Parallel.ForEachAsync(
                        source,
                        parallelOptions ?? new ParallelOptions(),
                        async (item, _) => { await body(item); });
#else
                    await Parallel_ForEachAsync<TSource>(source, parallelOptions.MaxDegreeOfParallelism, body);
#endif
                }
                catch (BreakLoopException ex)
                {
                    var item = ex.Data.Contains("item") ? ex.Data["item"] : null;
                    scope.LogDebug($"Break Loop occurred on source:{source.GetLogString()}, item:{item.GetLogString()}");
                }
            }
        }

        async Task IParallelService.WhenAllAsync(IEnumerable<Func<Task>> taskFactories, ParallelOptions parallelOptions)
        {
            using (var scope = logger.BeginMethodScope(new Func<object>(() => new { tasks = taskFactories.GetLogString(), parallelOptions = parallelOptions.GetLogString() })))
            {
                if (taskFactories?.Any() != true) { return; }

                await ForEachAsync(taskFactories, parallelOptions, async (taskFactory) => { await taskFactory(); });
            }
        }

        const string headerName = "MaxConcurrency";
        private void SetMaxConcurrency(ParallelOptions parallelOptions, CodeSectionScope scope)
        {
            if (parallelOptions == null) { return; }

            const string settingName = "MaxConcurrency";

            if (httpContextAccessor?.HttpContext?.Request.Headers.TryGetValue(settingName, out StringValues headerValues) == true
                && int.TryParse(headerValues.LastOrDefault(), out int headerMaxConcurrency))
            {
                scope.LogInformation($"From header: {settingName}={headerMaxConcurrency}");
                parallelOptions.MaxDegreeOfParallelism = headerMaxConcurrency;
                return;
            }

            var maxConcurrencyVariable = Environment.GetEnvironmentVariable(settingName);
            if (!string.IsNullOrEmpty(maxConcurrencyVariable) && int.TryParse(maxConcurrencyVariable, out int variableMaxConcurrency))
            {
                scope.LogInformation($"From environment: {settingName}={variableMaxConcurrency}");
                parallelOptions.MaxDegreeOfParallelism = variableMaxConcurrency;
                return;
            }
        }

        public static Task Parallel_ForEachAsync<T>(IEnumerable<T> source, int maxDegreeOfParallelism, Func<T, Task> action)
        {
            var options = new ExecutionDataflowBlockOptions();
            options.MaxDegreeOfParallelism = maxDegreeOfParallelism;
            var block = new ActionBlock<T>(action, options);
            foreach (var item in source) block.Post(item);
            block.Complete();
            return block.Completion;
        }

    }
}
