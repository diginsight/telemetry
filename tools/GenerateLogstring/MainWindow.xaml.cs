#region using
using Common;
using Microsoft.Extensions.DependencyInjection;
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

namespace GenerateLogstring
{
    //public class C : WeakEventManager { }
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        static Type T = typeof(MainWindow);
        private static ILogger<MainWindow> _logger;

        private string GetScope([CallerMemberName] string memberName = "") { return memberName; }

        static MainWindow()
        {
            var host = App.Host;
            var logger = host.GetLogger<MainWindow>();
            using (var scope = logger.BeginMethodScope())
            {
            }
        }
        public MainWindow(ILogger<MainWindow> logger)
        {
            _logger = logger;
            // using (_logger.BeginMethodScope())
            using (_logger.BeginScope(TraceLogger.GetMethodName()))
            {
                InitializeComponent();
            }
        }
        private async void MainWindow_Initialized(object sender, EventArgs e)
        {
            using (var scope = _logger.BeginMethodScope())
            {
                sampleMethod();
                await sampleMethod1Async();

                _logger.LogTrace("this is a trace trace"); // , properties: new Dictionary<string, object>() { { "", "" } }
                _logger.LogDebug("this is a debug trace"); // , properties: new Dictionary<string, object>() { { "", "" } }
                _logger.LogInformation(() => "this is a Information trace", "User"); // , properties: new Dictionary<string, object>() { { "", "" } }
                _logger.LogInformation(() => "this is a Information trace", "Raw");
                _logger.LogWarning(() => "this is a Warning trace", "User.Report");
                _logger.LogError(() => "this is a error trace", "Resource");

                _logger.LogError(() => "this is a error trace", "Resource");

                //TraceManager.Debug("")
                scope.LogDebug(() => "this is a trace trace", "User"); // , properties: new Dictionary<string, object>() { { "", "" } }
                scope.LogDebug(() => "this is a debug trace", "User"); // , properties: new Dictionary<string, object>() { { "", "" } }
                scope.LogInformation(() => "this is a debug trace", "User"); // , properties: new Dictionary<string, object>() { { "", "" } }
                scope.LogInformation(() => "this is a Information trace", "Raw");
                scope.LogWarning(() => "this is a Warning trace", "User.Report");
                scope.LogError(() => "this is a error trace", "Resource");

                scope.LogError(() => "this is a error trace", "Resource");

                var guid = Guid.NewGuid();
                var uri = new Uri("http://localhost:80");
                scope.LogDebug(new { guid, uri });
            }
        }
        void sampleMethod()
        {
            _logger.LogDebug("pippo");

        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            // _logger.PushOperationId();
            using var scope = _logger.BeginMethodScope(new { sender = sender.GetLogString(), e = e.GetLogString() }, SourceLevels.Verbose, LogLevel.Debug, null, new Dictionary<string, object>() { { "OperationId", Guid.NewGuid().ToString() } });

            try
            {
                scope.LogDebug(new { sender = sender.GetLogString(), e = e.GetLogString() });

                // log by means of the scope variable
                scope.LogTrace(() => "this is a long trace trace", null, new Dictionary<string, object>() { { "MaxMessageLen", 0 } }); //  TraceLogger.LogDebug($"requestBody: {requestBody}");
                scope.LogDebug(() => "this is a debug trace"); // , properties: new Dictionary<string, object>() { { "", "" } }
                scope.LogInformation(() => "this is a Information trace"); // , properties: new Dictionary<string, object>() { { "", "" } }
                scope.LogWarning(() => "this is a Warning trace");
                scope.LogError(() => "this is a error trace");
                scope.LogError(() => "this is a error trace");

                // log by means of standard ILogger Interface
                _logger.LogTrace("this is a Trace trace");
                _logger.LogDebug("this is a Debug trace");
                _logger.LogInformation("this is a Information trace");
                _logger.LogWarning("this is a Warning trace");
                _logger.LogError("this is a error trace");
                _logger.LogCritical("this is a critical trace");

                // log by means of static methods
                TraceLogger.LogTrace(() => "this is a trace trace"); // , properties: new Dictionary<string, object>() { { "", "" } }
                TraceLogger.LogDebug(() => "this is a debug trace"); // , properties: new Dictionary<string, object>() { { "", "" } }
                TraceLogger.LogInformation(() => "this is a Information trace"); // , properties: new Dictionary<string, object>() { { "", "" } }
                TraceLogger.LogWarning(() => "this is a Warning trace");
                TraceLogger.LogError(() => "this is a error trace");
                // TraceLogger.LogCritical(() => "this is a error trace");

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
            using var scope = _logger.BeginMethodScope(new { i, s });

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
            using (var sec = _logger.BeginMethodScope())
            {
                Thread.Sleep(100);
                SampleMethodNested();
                SampleMethodNested1();

            }
        }
        public void SampleMethodNested()
        {
            using var scope = _logger.BeginMethodScope();
            Thread.Sleep(100);
        }
        public void SampleMethodNested1()
        {
            using var scope = _logger.BeginMethodScope();
            Thread.Sleep(10);
        }
        async Task<bool> sampleMethod1Async()
        {
            using (var scope = _logger.BeginMethodScope())
            {
                var res = true;

                await Task.Delay(0); scope.LogDebug($"await Task.Delay(0);");

                return res;
            }
        }
    }
}
