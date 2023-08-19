#region using
using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace EasySampleService
{
    internal class Program
    {
        public static Type T = typeof(Program);
        public const string CONFIGSETTING_STARTUPSLEEP = "StartupSleep"; public const int CONFIGDEFAULT_STARTUPSLEEP = 0;
        public const string CONFIGSETTING_STARTSERVICEONINSTALL = "StartServiceOnInstall"; public const bool CONFIGDEFAULT_STARTSERVICEONINSTALL = true;

        /// <summary>The main entry point for the application.</summary>
        static void Main(string[] args)
        {
            using (var sec = TraceManager.GetCodeSection(T))
            {
                try
                {
                    //ConfigurationHelper.Init(TraceManager.Configuration);
                    var classConfigurationGetter = new ClassConfigurationGetter<EasySampleService>(TraceManager.Configuration);

                    Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
                    // Thread.Sleep(10000);
                    var currentDirectory = Directory.GetCurrentDirectory();
                    TraceManager.Debug($"currentDirectory:{currentDirectory}");

                    //var startupSleep = ConfigurationHelper.GetClassSetting<Program, int>(CONFIGSETTING_STARTUPSLEEP, CONFIGDEFAULT_STARTUPSLEEP);
                    var startupSleep = classConfigurationGetter.Get(CONFIGSETTING_STARTUPSLEEP, CONFIGDEFAULT_STARTUPSLEEP);
                    if (startupSleep > 0) { Thread.Sleep(startupSleep); sec.Debug($"Thread.Sleep({startupSleep}); completed"); }

                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[] { new EasySampleService() };
                    //ServiceBase.Run(ServicesToRun);
                    if (Environment.UserInteractive)
                    {
                        var argument = args != null && args.Length > 0 ? args[0]?.Trim()?.ToLower() : null;

                        #region /install
                        if (!string.IsNullOrEmpty(argument) && argument == "/i" || argument == "/install") // 
                        {
                            var message = $"installing {sec.Assembly.GetName().Name} (argument: '{argument}')";
                            sec.Information(message); Console.WriteLine(message);
                            var services = ServiceController.GetServices(".");
                            var svc = services.FirstOrDefault(s => s.ServiceName == "EasySampleService");
                            if (svc != null)
                            {
                                message = "Service EkipDeviceSimulator is already installed please uninstall the old version before installing it again";
                                sec.Information(message); Console.WriteLine(message);
                                throw new ApplicationException(message);
                            }

                            using (AssemblyInstaller inst = new AssemblyInstaller(typeof(Program).Assembly, args))
                            {
                                IDictionary state = new Hashtable();
                                inst.UseNewContext = true;
                                try
                                {
                                    inst.Install(state);
                                    inst.Commit(state);
                                }
                                catch { try { inst.Rollback(state); } catch { } throw; }
                            }
                            message = $"{sec.Assembly.GetName().Name} install completed";
                            sec.Information(message); Console.WriteLine(message);

                            //var startServiceOnInstall = ConfigurationHelper.GetClassSetting<Program, bool>(CONFIGSETTING_STARTSERVICEONINSTALL, CONFIGDEFAULT_STARTSERVICEONINSTALL);
                            var startServiceOnInstall = classConfigurationGetter.Get(CONFIGSETTING_STARTSERVICEONINSTALL, CONFIGDEFAULT_STARTSERVICEONINSTALL);
                            if (startServiceOnInstall)
                            {
                                ServiceController service = new ServiceController("EasySampleService");
                                if ((service.Status.Equals(ServiceControllerStatus.Stopped)) || (service.Status.Equals(ServiceControllerStatus.StopPending)))
                                {
                                    service.Start(); sec.Debug("service.Start();");
                                }
                            }
                            //else service.Stop();

                            return;
                        }
                        #endregion

                        #region /uninstall
                        if (!string.IsNullOrEmpty(argument) && argument == "/u" || argument == "/uninstall")
                        {
                            var message = $"Removing {sec.Assembly.GetName().Name} install (argument: '{argument}')";
                            sec.Information(message); Console.WriteLine(message);
                            using (AssemblyInstaller inst = new AssemblyInstaller(typeof(Program).Assembly, args))
                            {
                                IDictionary state = new Hashtable();
                                inst.UseNewContext = true;
                                try { inst.Uninstall(state); }
                                catch { try { inst.Rollback(state); } catch { } throw; }
                            }
                            return;
                        }
                        #endregion

                        Task.Run(() => ServiceBase.Run(ServicesToRun)).Wait();

                        Console.WriteLine("Press any key to exit...");
                        Console.ReadLine();
                    }
                    else
                    {
                        //Thread.Sleep(5000); sec.Debug("Thread.Sleep(5000); completed");
                        ServiceBase.Run(ServicesToRun); sec.Debug("ServiceBase.Run(ServicesToRun); completed");
                    }

                }
                catch (Exception ex) { sec.Exception(ex); }
            }
        }

    }
}
