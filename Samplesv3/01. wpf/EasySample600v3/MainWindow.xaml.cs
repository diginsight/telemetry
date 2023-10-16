#region using
using Common;
using EasySample600v2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    //public class C : WeakEventManager { }
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        static Type T = typeof(MainWindow);
        private ILogger<MainWindow> logger;
        private IClassConfigurationGetter<MainWindow> classConfigurationGetter;

        private string GetScope([CallerMemberName] string memberName = "") { return memberName; }

        static MainWindow()
        {
            var host = App.Host;
            //var logger = host.GetLogger<MainWindow>();
            //using (var scope = logger.BeginMethodScope())
            //{
            //}
            using var scope = host.BeginMethodScope<MainWindow>();
        }
        public MainWindow(
            ILogger<MainWindow> logger,
            IClassConfigurationGetter<MainWindow> classConfigurationGetter
            )
        {
            this.logger = logger;
            this.classConfigurationGetter = classConfigurationGetter;
            // using (_logger.BeginMethodScope())
            using (logger.BeginScope(TraceLogger.GetMethodName()))
            {
                InitializeComponent();
            }
        }
        private async void MainWindow_Initialized(object sender, EventArgs e)
        {
            using var scope = logger.BeginMethodScope(() => new { sender, e });

            classConfigurationGetter.Get("SampleConfig", "");
            sampleMethod();
            await sampleMethod1Async();

            int i = 0;

            // scope.LogDebug
            logger.LogDebug(() => new { i, e = e.GetLogString(), sender = sender.GetLogString() }); // , properties: new Dictionary<string, object>() { { "", "" } }


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
            logger.LogDebug("pippo");

        }

        int i = 0;
        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            // _logger.PushOperationId();
            using var scope = logger.BeginMethodScope(() => new { sender = sender.GetLogString(), e = e.GetLogString() }, SourceLevels.Verbose, LogLevel.Debug, null, new Dictionary<string, object>() { { "OperationId", Guid.NewGuid().ToString() } });

            //var logger1 = new SampleLogger() { EnabledLevel = LogLevel.Warning };
            //var time = DateTime.Now;
            //logger1.LogDebug($"Error Level. CurrentTime: {time}. This is an error. It will be printed.");
            var time = DateTime.Now;
            var responseString = "pippo";
            logger.LogDebug($"Response {{ body ({(double?)responseString?.Length / 1024:#,##0} KB): {responseString}");
            // TODO: interpolated string should not be created => OK
            // TODO: placeholders should not be evaluated (with log )

            try
            {
                scope.LogDebug(() => new { sender = sender.GetLogString(), e = e.GetLogString() });

                {
                    using var scopeInner = logger.BeginNamedScope("OptimizedByInterpolatedStringHandler");

                    // log by means of the scope variable
                    scopeInner.LogTrace($"this is a long trace trace ({e.GetLogString()})", null, new Dictionary<string, object>() { { "MaxMessageLen", 0 } }); //  TraceLogger.LogDebug($"requestBody: {requestBody}");
                    scopeInner.LogDebug($"this is a debug trace ({e.GetLogString()})"); // , properties: new Dictionary<string, object>() { { "", "" } }
                    scopeInner.LogInformation($"this is a Information trace ({e.GetLogString()})"); // , properties: new Dictionary<string, object>() { { "", "" } }
                    scopeInner.LogWarning($"this is a Warning trace ({e.GetLogString()})");
                    scopeInner.LogError($"this is a error trace ({e.GetLogString()})");
                    scopeInner.LogError($"this is a error trace ({e.GetLogString()})");
                }

                {
                    using var scopeInner = logger.BeginNamedScope("OptimizedByDelegate");

                    // log by means of the scope variable
                    scopeInner.LogTrace(() => "this is a long trace trace", null, new Dictionary<string, object>() { { "MaxMessageLen", 0 } }); //  TraceLogger.LogDebug($"requestBody: {requestBody}");
                    scopeInner.LogDebug(() => "this is a debug trace"); // , properties: new Dictionary<string, object>() { { "", "" } }
                    scopeInner.LogInformation(() => "this is a Information trace"); // , properties: new Dictionary<string, object>() { { "", "" } }
                    scopeInner.LogWarning(() => "this is a Warning trace");
                    scopeInner.LogError(() => "this is a error trace");
                    scopeInner.LogError(() => "this is a error trace");
                }

                {
                    using var scopeInner = logger.BeginNamedScope("StandardCodeSection");

                    // log by means of standard ILogger Interface
                    logger.LogTrace("this is a Trace trace");
                    logger.LogDebug("this is a Debug trace");
                    logger.LogInformation("this is a Information trace");
                    logger.LogWarning("this is a Warning trace");
                    logger.LogError("this is a error trace");
                    logger.LogCritical("this is a critical trace");
                }

                {
                    using var scopeInner = logger.BeginNamedScope("OptimizedByDelegate (static methods)");

                    // log by means of static methods
                    TraceLogger.LogTrace(() => "this is a trace trace"); // , properties: new Dictionary<string, object>() { { "", "" } }
                    TraceLogger.LogDebug(() => "this is a debug trace"); // , properties: new Dictionary<string, object>() { { "", "" } }
                    TraceLogger.LogInformation(() => "this is a Information trace"); // , properties: new Dictionary<string, object>() { { "", "" } }
                    TraceLogger.LogWarning(() => "this is a Warning trace");
                    TraceLogger.LogError(() => "this is a error trace");
                    // TraceLogger.LogCritical(() => "this is a error trace");
                }
                {
                    using var scopeInner = logger.BeginNamedScope("OptimizedByInterpolatedStringHandler (static methods - TraceLogger)");

                    // log by means of the scope variable
                    TraceLogger.LogTrace($"this is a long trace trace ({e.GetLogString()})", null, new Dictionary<string, object>() { { "MaxMessageLen", 0 } }); //  TraceLogger.LogDebug($"requestBody: {requestBody}");
                    TraceLogger.LogDebug($"this is a debug trace ({e.GetLogString()})"); // , properties: new Dictionary<string, object>() { { "", "" } }
                    TraceLogger.LogInformation($"this is a Information trace ({e.GetLogString()})"); // , properties: new Dictionary<string, object>() { { "", "" } }
                    TraceLogger.LogWarning($"this is a Warning trace ({e.GetLogString()})");
                    TraceLogger.LogError($"this is a error trace ({e.GetLogString()})");
                    TraceLogger.LogError($"this is a error trace ({e.GetLogString()})");
                }
                {
                    using var scopeInner = logger.BeginNamedScope("OptimizedByInterpolatedStringHandler (static methods - TraceManager)");

                    // log by means of the scope variable
                    TraceManager.Trace($"this is a long trace trace ({e.GetLogString()})", null, new Dictionary<string, object>() { { "MaxMessageLen", 0 } }); //  TraceLogger.LogDebug($"requestBody: {requestBody}");
                    TraceManager.Debug($"this is a debug trace ({e.GetLogString()})"); // , properties: new Dictionary<string, object>() { { "", "" } }
                    TraceManager.Information($"this is a Information trace ({e.GetLogString()})"); // , properties: new Dictionary<string, object>() { { "", "" } }
                    TraceManager.Warning($"this is a Warning trace ({e.GetLogString()})");
                    TraceManager.Error($"this is a error trace ({e.GetLogString()})");
                    TraceManager.Error($"this is a error trace ({e.GetLogString()})");
                }
                int i = 0; string s = "sample parameter";
                var res = SampleMethodWithResult(i, s);

                throw new NullReferenceException();
            }
            catch (Exception ex)
            {
                scope.LogException(ex);
            }
        }

        public int SampleMethodWithResult(int i, string s)
        {
            using var scope = logger.BeginMethodScope(new { i, s });

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
            using (var sec = logger.BeginMethodScope())
            {
                Thread.Sleep(100);
                SampleMethodNested();
                SampleMethodNested1();

            }
        }
        public void SampleMethodNested()
        {
            using var scope = logger.BeginMethodScope();
            Thread.Sleep(100);
        }
        public void SampleMethodNested1()
        {
            using var scope = logger.BeginMethodScope();
            Thread.Sleep(10);
        }
        async Task<bool> sampleMethod1Async()
        {
            using (var scope = logger.BeginMethodScope())
            {
                var res = true;

                await Task.Delay(0); scope.LogDebug($"await Task.Delay(0);");

                return res;
            }
        }
    }
}
