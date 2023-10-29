#region using
using Common;
using EasySample600v2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Metrics = System.Collections.Generic.Dictionary<string, object>; // $$$
#endregion

namespace EasySample
{
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        //private static ActivitySource source = new ActivitySource("EasySamplev3.MainWindow", "1.0.0");
        static Type T = typeof(MainWindow);
        private ILogger<MainWindow> logger;
        private IClassConfigurationGetter<MainWindow> classConfigurationGetter;

        private string GetScope([CallerMemberName] string memberName = "") { return memberName; }

        static MainWindow()
        {
            var host = App.Host;
            using var scope = host.BeginMethodScope<MainWindow>();
            using Activity activity = TraceLogger.ActivitySource.StartActivity();
            //using var scope = TraceLogger.ActivitySource.StartMethodActivity(logger);

            //var logger = host.GetLogger<MainWindow>();
            //using (var scope = logger.BeginMethodScope())
            //{
            //}

        }
        public MainWindow(
            ILogger<MainWindow> logger,
            IClassConfigurationGetter<MainWindow> classConfigurationGetter
            )
        {
            this.logger = logger;
            this.classConfigurationGetter = classConfigurationGetter;
            // using (_logger.BeginMethodScope())
            //using var d = logger.BeginScope(TraceLogger.GetMethodName());
            //using Activity activity = TraceLogger.ActivitySource.StartActivity(TraceLogger.GetMethodName());
            using var scope = TraceLogger.ActivitySource.StartMethodActivity(logger);

            InitializeComponent();
        }
        private async void MainWindow_Initialized(object sender, EventArgs e)
        {
            //using var scope = logger.BeginMethodScope(() => new { sender, e });
            //using Activity activity = TraceLogger.ActivitySource.StartActivity(TraceLogger.GetMethodName());
            using var scope = TraceLogger.ActivitySource.StartMethodActivity(logger, new { e, sender });

            classConfigurationGetter.Get("SampleConfig", "");
            sampleMethod();
            await sampleMethod1Async();

            int i = 0;

            logger.LogDebug(() => new { i, e, sender }); // , properties: new Dictionary<string, object>() { { "", "" } }


            {
                TraceLogger.BeginNamedScope<MainWindow>("Standard code section");
                //, () => new { i, e = e.GetLogString(), sender = sender.GetLogString() }

                logger.LogTrace("this is a trace trace"); // , properties: new Dictionary<string, object>() { { "", "" } }
                logger.LogDebug("this is a debug trace"); // , properties: new Dictionary<string, object>() { { "", "" } }
            }

            {
                logger.BeginNamedScope("Optimized code section");

                logger.LogInformation(() => "this is a Information trace", "User"); // , properties: new Dictionary<string, object>() { { "", "" } }
                logger.LogInformation(() => "this is a Information trace", "Raw");
                logger.LogWarning(() => "this is a Warning trace", "User.Report");
                logger.LogError(() => $"this is a error trace", "Resource");

                logger.LogError(() => "this is a error trace", "Resource");

                //TraceManager.Debug("")
                scope.LogDebug(() => "this is a trace trace", "User"); // , properties: new Dictionary<string, object>() { { "", "" } }
                scope.LogDebug(() => "this is a debug trace", "User"); // , properties: new Dictionary<string, object>() { { "", "" } }
                scope.LogInformation(() => "this is a debug trace", "User"); // , properties: new Dictionary<string, object>() { { "", "" } }
                scope.LogInformation(() => "this is a Information trace", "Raw");
                scope.LogWarning(() => "this is a Warning trace", "User.Report");
                scope.LogError(() => "this is a error trace", "Resource");

                scope.LogError(() => "this is a error trace", "Resource");
            }

            var guid = Guid.NewGuid();
            var uri = new Uri("http://localhost:80");
            scope.LogDebug(new { guid, uri });
        }
        void sampleMethod()
        {
            //using Activity activity = TraceLogger.ActivitySource.StartActivity(TraceLogger.GetMethodName());
            using var scope = TraceLogger.ActivitySource.StartMethodActivity(logger);

            logger.LogDebug("pippo");

        }

        int i = 0;
        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //using var scope = logger.BeginMethodScope(() => new { sender, e }, SourceLevels.Verbose, LogLevel.Debug, null, new Dictionary<string, object>() { { "OperationId", Guid.NewGuid().ToString() } });
                //using Activity activity = TraceLogger.ActivitySource.StartActivity(TraceLogger.GetMethodName());
                using var scope = TraceLogger.ActivitySource.StartMethodActivity(logger, new { e, sender });

                // Custom metrics for the application
                var greeterMeter = new Meter("OtPrGrYa.Example", "1.0.0");
                var countGreetings = greeterMeter.CreateCounter<int>("greetings.count", description: "Counts the number of greetings");

                // Custom ActivitySource for the application
                //var greeterActivitySource = new ActivitySource("OtPrGrJa.Example");
                throw new InvalidOperationException("sample ex");
            }
            catch (Exception _) { }
        }

        public int SampleMethodWithResult(int i, string s)
        {
            //using var scope = logger.BeginMethodScope(new { i, s });
            //using Activity activity = TraceLogger.ActivitySource.StartActivity(TraceLogger.GetMethodName());
            using var scope = TraceLogger.ActivitySource.StartMethodActivity(logger, new { i, s });

            var result = 0;

            var j = i++; scope.LogDebug(new { i, j });

            Thread.Sleep(100); scope.LogDebug($"Thread.Sleep(100); completed");
            SampleMethodNested(); scope.LogDebug($"SampleMethodNested(); completed");
            SampleMethodNested1(); scope.LogDebug($"SampleMethodNested1(); completed");

            scope.Result = result;
            return result;
        }
        public void SampleMethod()
        {
            //using var sec = logger.BeginMethodScope();
            //using Activity activity = TraceLogger.ActivitySource.StartActivity(TraceLogger.GetMethodName());
            using var scope = TraceLogger.ActivitySource.StartMethodActivity(logger);

            Thread.Sleep(100);
            SampleMethodNested();
            SampleMethodNested1();

        }
        public void SampleMethodNested()
        {
            //using var scope = logger.BeginMethodScope();
            //using Activity activity = TraceLogger.ActivitySource.StartActivity(TraceLogger.GetMethodName());
            using var scope = TraceLogger.ActivitySource.StartMethodActivity(logger);

            Thread.Sleep(100);
        }
        public void SampleMethodNested1()
        {
            //using var scope = logger.BeginMethodScope();
            //using Activity activity = TraceLogger.ActivitySource.StartActivity(TraceLogger.GetMethodName());
            using var scope = TraceLogger.ActivitySource.StartMethodActivity(logger);

            Thread.Sleep(10);
        }
        async Task<bool> sampleMethod1Async()
        {
            //using var scope = logger.BeginMethodScope();
            //using Activity activity = TraceLogger.ActivitySource.StartActivity(TraceLogger.GetMethodName());
            using var scope = TraceLogger.ActivitySource.StartMethodActivity(logger);

            var res = true;

            await Task.Delay(0); scope.LogDebug($"await Task.Delay(0);");

            return res;

        }
    }
}
