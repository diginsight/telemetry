using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EasySample462 
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static Type T = typeof(App);
        
        static App()
        {
            //TraceManager.Init(SourceLevels.All, null);
            using (var sec = TraceManager.GetCodeSection(T))
            {
                try
                {
                    sec.Debug("this is a debug trace");
                    sec.Information("this is a Information trace");
                    sec.Warning("this is a Warning trace");
                    sec.Error("this is a error trace");

                    throw new InvalidOperationException("this is an exception");
                }
                catch (Exception ex)
                {
                    sec.Exception(ex);
                }
            }
        }

        public App()
        {
            using (var sec = TraceManager.GetCodeSection(T))
            {
                //LogStringExtensions.RegisterLogstringProvider(this);
            }
        }


    }
}
