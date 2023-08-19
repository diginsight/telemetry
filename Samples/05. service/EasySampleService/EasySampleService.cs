#region using
using Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
#endregion

namespace EasySampleService
{
    public partial class EasySampleService : ServiceBase
    {
        #region const
        private const int QUERYINTERVAL_DEFAULT = 1000;
        #endregion
        #region internal state
        private System.Timers.Timer _timer = null;
        private Task<bool> _requestsListenerTask = null;
        private readonly ClassConfigurationGetter<EasySampleService> classConfigurationGetter;
        #endregion

        public EasySampleService()
        {
            InitializeComponent();

            this.classConfigurationGetter = new ClassConfigurationGetter<EasySampleService>(TraceManager.Configuration);
        }

        public Task<bool> Run(string[] args)
        {
            using (var sec = this.GetCodeSection(new { args }))
            {
                OnStart(args);
                return _requestsListenerTask;
            }
        }

        #region OnStart
        protected override void OnStart(string[] args)
        {
            using (var sec = this.GetCodeSection(new { args }))
            {
                try
                {
                    base.OnStart(args);

                    if (_timer != null)
                    {
                        _timer.Enabled = false;
                        _timer.Elapsed -= new ElapsedEventHandler(this.ServiceTimer_Tick);
                        _timer = null;
                    }
                    // var interval = !string.IsNullOrEmpty(intervalSetting) ? Convert.ToInt32(intervalSetting) : SBR_SMARTCOMANAGER_QUERYINTERVAL_DEFAULT;
                    _timer = new System.Timers.Timer(1000); sec.Debug($"..._timer = new System.Timers.Timer({1000}) completed");
                    _timer.Elapsed += new ElapsedEventHandler(this.ServiceTimer_Tick); sec.Debug($"..._timer.Elapsed += new ElapsedEventHandler(this.ServiceTimer_Tick);");
                    _timer.AutoReset = true;
                    _timer.Enabled = true;
                    _timer.Start();
                }
                catch (Exception ex) { sec.Error($"{ex}"); }
            }
        }
        #endregion
        #region OnStop
        protected override void OnStop()
        {
            using (var sec = this.GetCodeSection())
            {
                if (_timer != null)
                {
                    _timer.AutoReset = false;
                    _timer.Enabled = false;
                    //Environment.Exit(0);
                }
            }
        }
        #endregion
        #region OnContinue
        protected override void OnContinue()
        {
            using (var sec = this.GetCodeSection())
            {
                base.OnContinue();
                if (_timer != null)
                {
                    this._timer.Start();
                }
            }
        }
        #endregion
        #region OnPause
        protected override void OnPause()
        {
            using (var sec = this.GetCodeSection())
            {
                base.OnPause();
                if (_timer != null)
                {
                    this._timer.Stop();
                }
            }
        }
        #endregion
        #region OnShutdown
        protected override void OnShutdown()
        {
            using (var sec = this.GetCodeSection())
            {
                base.OnShutdown();
            }
        }
        #endregion

        private void ServiceTimer_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            this._timer.Stop();
            using (var sec = this.GetCodeSection())
            {
                try
                {


                    this._timer.Start();
                }
                catch
                {
                    //sec.Error($"{ex}");
                }
                finally
                {
                    var interval = classConfigurationGetter.Get("QUERYINTERVAL", QUERYINTERVAL_DEFAULT);
                    //var interval = ConfigurationHelper.GetSetting("QUERYINTERVAL", QUERYINTERVAL_DEFAULT);
                    this._timer.Interval = interval;
                }
            }
        }
    }
}
