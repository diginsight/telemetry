using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
//using static System.Formats.Asn1.AsnWriter;

namespace Common
{
    public interface IObservabilityOptions
    {
        LogLevel DefaultActivityLogLevel { get; }
    }
    public sealed class ObservabilityOptions : IObservabilityOptions
    {
        public LogLevel DefaultActivityLogLevel { get; set; } = LogLevel.Debug;
    }

    public sealed class ObservabilityLogProcessor : BaseProcessor<Activity>
    {
        private readonly ILogger<ObservabilityLogProcessor> logger;
        private readonly IOptionsMonitor<ObservabilityOptions> observabilityOptionsMonitor;

        public ObservabilityLogProcessor(
            ILogger<ObservabilityLogProcessor> logger,
            IOptionsMonitor<ObservabilityOptions> observabilityOptionsMonitor
        )
        {
            this.logger = logger;
            this.observabilityOptionsMonitor = observabilityOptionsMonitor;
        }

        public override void OnStart(Activity activity)
        {
        }

        public override void OnEnd(Activity activity)
        {
            var scope = activity.GetCustomProperty("Scope") as IDisposable;
            if (scope!= null) { scope.Dispose(); }
        }

    }

    //public class ActivityExporter : BaseExporter<Activity>
    //    //where T : class
    //{
    //    //private readonly ConsoleExporterOptions options;

    //    public ActivityExporter() // ConsoleExporterOptions options
    //    {
    //        //this.options = options ?? new ConsoleExporterOptions();
    //        //ConsoleTagTransformer.LogUnsupportedAttributeType = (string tagValueType, string tagKey) =>
    //        //{
    //        //    this.WriteLine($"Unsupported attribute type {tagValueType} for {tagKey}.");
    //        //};
    //        this.WriteLine($"ActivityExporter() added");
    //    }
    //    public override ExportResult Export(in Batch<Activity> batch)
    //    {
    //        foreach (var activity in batch)
    //        {

    //            this.WriteLine($"activity {activity.DisplayName} (IsStopped:{activity.IsStopped},Duration:{activity.Duration}) export");
    //        }

    //        return ExportResult.Success;
    //    }

    //    public void WriteLine(string message)
    //    {
    //        //if (this.options.Targets.HasFlag(ConsoleExporterOutputTargets.Console))
    //        //{
    //        Console.WriteLine(message);
    //        Debug.WriteLine(message);
    //        //}

    //        //if (this.options.Targets.HasFlag(ConsoleExporterOutputTargets.Debug))
    //        //{
    //        //    System.Diagnostics.Trace.WriteLine(message);
    //        //}
    //    }
    //}
}
