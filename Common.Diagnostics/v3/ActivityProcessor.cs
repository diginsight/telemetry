using OpenTelemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Common
{
    public class ActivityProcessor : BaseProcessor<Activity>
    //where T : class
    {
        protected readonly BaseExporter<Activity> exporter;
        private readonly string friendlyTypeName;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseExportProcessor{T}"/> class.
        /// </summary>
        /// <param name="exporter">Exporter instance.</param>
        public ActivityProcessor(BaseExporter<Activity> exporter)
        {
            //Guard.ThrowIfNull(exporter);

            this.friendlyTypeName = $"{this.GetType().Name}{{{exporter.GetType().Name}}}";
            this.exporter = exporter;
        }

        internal BaseExporter<Activity> Exporter => this.exporter;

        /// <inheritdoc />
        public override string ToString() => this.friendlyTypeName;

        /// <inheritdoc />
        public sealed override void OnStart(Activity data)
        {
            this.exporter.Export(new Batch<Activity>(new[] { data }, 1));
        }

        public override void OnEnd(Activity data)
        {
            this.exporter.Export(new Batch<Activity>(new[] { data }, 1));
        }

        //public override void SetParentProvider(BaseProvider parentProvider)
        //{
        //    base.SetParentProvider(parentProvider);

        //    this.exporter.ParentProvider = parentProvider;
        //}

        //protected override void OnExport(Activity data)
        //{
        //    //lock (this.syncObject)
        //    //{
        //        //try
        //        //{
        //        //    //this.exporter.Export(new Batch<Activity>(data));
        //        //}
        //        //catch (Exception ex)
        //        //{
        //        //    //OpenTelemetrySdkEventSource.Log.SpanProcessorException(nameof(this.OnExport), ex);
        //        //}
        //    //}
        //}

        /// <inheritdoc />
        protected override bool OnForceFlush(int timeoutMilliseconds)
        {
            return this.exporter.ForceFlush(timeoutMilliseconds);
        }

        /// <inheritdoc />
        protected override bool OnShutdown(int timeoutMilliseconds)
        {
            return this.exporter.Shutdown(timeoutMilliseconds);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    try
                    {
                        this.exporter.Dispose();
                    }
                    catch (Exception _)
                    {
                        //OpenTelemetrySdkEventSource.Log.SpanProcessorException(nameof(this.Dispose), ex);
                    }
                }

                this.disposed = true;
            }

            base.Dispose(disposing);
        }
    }

    public class ActivityExporter : BaseExporter<Activity>
        //where T : class
    {
        //private readonly ConsoleExporterOptions options;

        public ActivityExporter() // ConsoleExporterOptions options
        {
            //this.options = options ?? new ConsoleExporterOptions();
            //ConsoleTagTransformer.LogUnsupportedAttributeType = (string tagValueType, string tagKey) =>
            //{
            //    this.WriteLine($"Unsupported attribute type {tagValueType} for {tagKey}.");
            //};
            this.WriteLine($"ActivityExporter() added");
        }
        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var activity in batch)
            {

                this.WriteLine($"activity {activity.DisplayName} (IsStopped:{activity.IsStopped},Duration:{activity.Duration}) export");
            }

            return ExportResult.Success;
        }

        public void WriteLine(string message)
        {
            //if (this.options.Targets.HasFlag(ConsoleExporterOutputTargets.Console))
            //{
            Console.WriteLine(message);
            Debug.WriteLine(message);
            //}

            //if (this.options.Targets.HasFlag(ConsoleExporterOutputTargets.Debug))
            //{
            //    System.Diagnostics.Trace.WriteLine(message);
            //}
        }
    }
}
