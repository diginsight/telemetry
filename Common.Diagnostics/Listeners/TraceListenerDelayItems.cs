#region using
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
#endregion

namespace Common
{
    public class TraceListenerDelayItems : TraceListener, ISupportFilters, ISupportInnerListener
    {
        #region const
        const int MINDELAY = 100;
        const int MINDELAYFREQ = 10;
        const int MAXDELAY = 500;
        const int MAXDELAYFREQ = 100;
        const int DELAY = 200;
        const int FLUSHDELAY = 400;
        #endregion
        #region internal state
        static Stopwatch _stopwatch = TraceManager.Stopwatch;
        Timer _timer = null;
        long _lastTickWriteTimestamp = 0;
        ConcurrentQueue<object> _entries = new ConcurrentQueue<object>();
        ConcurrentQueue<object> _nextEntries = new ConcurrentQueue<object>();
        #endregion

        #region ISupportFilters
        string ISupportFilters.Filter { get; set; }
        #endregion
        #region ISupportInnerListener
        public TraceListener InnerListener { get; set; }
        #endregion

        #region .ctor
        static TraceListenerDelayItems() { }
        public TraceListenerDelayItems()
        {
            _timer = new Timer(new TimerCallback(this._timer_tick), null, 0, FLUSHDELAY);
        }
        public TraceListenerDelayItems(TraceListener innerListener) : this()
        {
            InnerListener = innerListener;
        }
        #endregion

        public override void Write(string s) { Write((object)s); }
        public override void Write(object o)
        {
            if (InnerListener == null) { return; }

            var delay = MINDELAY;
            var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds - _lastTickWriteTimestamp;
            if (elapsedMilliseconds > MINDELAY)
            {
                var freq = _entries.Count * 1000 / elapsedMilliseconds;
                var factor = (freq - MINDELAYFREQ) / (MAXDELAYFREQ - MINDELAYFREQ);
                delay = MINDELAY + (int)(factor * (MAXDELAY - MINDELAY));
            }
            if (elapsedMilliseconds < delay) { _entries.Enqueue(o); return; }

            var entries = _entries; _entries = _nextEntries;
            _nextEntries = new ConcurrentQueue<object>();
            _lastTickWriteTimestamp = _stopwatch.ElapsedMilliseconds;
            entries.Enqueue(o);

            InnerListener.Write(entries);
        }

        public override void WriteLine(string s) { WriteLine((object)s); }
        public override void WriteLine(object o)
        {
            if (InnerListener == null) { return; }

            var delay = MINDELAY;
            var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds - _lastTickWriteTimestamp;
            if (elapsedMilliseconds > MINDELAY)
            {
                var freq = _entries.Count * 1000 / elapsedMilliseconds;
                var factor = (freq - MINDELAYFREQ) / (MAXDELAYFREQ - MINDELAYFREQ);
                delay = MINDELAY + (int)(factor * (MAXDELAY - MINDELAY));
            }
            if (elapsedMilliseconds < delay)
            {
                object oSave = o is string ? (o as string) + Environment.NewLine : o; // save lines with added NewLine for later playback with Write() method
                _entries.Enqueue(oSave); return;
            }

            var entries = _entries; _entries = _nextEntries;
            _nextEntries = new ConcurrentQueue<object>();
            _lastTickWriteTimestamp = _stopwatch.ElapsedMilliseconds;
            object oSave1 = o is string ? (o as string) + Environment.NewLine : o;
            entries.Enqueue(oSave1); // save lines with added NewLine for later playback with Write() method

            InnerListener.Write(entries);
        }
        public override void Flush()
        {
            var delay = MINDELAY;
            var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds - _lastTickWriteTimestamp;
            if (elapsedMilliseconds > MINDELAY)
            {
                var freq = _entries.Count * 1000 / elapsedMilliseconds;
                var factor = (freq - MINDELAYFREQ) / (MAXDELAYFREQ - MINDELAYFREQ);
                delay = MINDELAY + (int)(factor * (MAXDELAY - MINDELAY));
            }
            if (elapsedMilliseconds < delay) { return; }

            var entries = _entries; _entries = _nextEntries;
            _nextEntries = new ConcurrentQueue<object>();
            _lastTickWriteTimestamp = _stopwatch.ElapsedMilliseconds;

            if (InnerListener == null) { return; }
            if (entries == null || entries.Count <= 0) { return; }
            InnerListener.Write(entries);
        }
        private void _timer_tick(object state)
        {
            Flush();
        }
    }
}
