#region using
using System;
using Common;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
//using System.Windows.Media;
using System.Diagnostics;
using System.Collections.Generic;
#endregion

namespace Common
{
    #region Reference<T>
    internal class Reference<T> : INotifyPropertyChanged
    {
        #region .ctor
        public Reference() { }
        public Reference(T value) { Value = value; }
        #endregion

        #region Value
        T _value;
        public T Value { get { return _value; } set { SetAndNotifyChange(this, this.PropertyChanged, "Value", ref _value, value); } }
        #endregion

        // INotifyPropertyChanged
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        void PropertyChangedDelegateImpl(object sender, PropertyChangedEventArgs e) { if (PropertyChanged != null) { PropertyChanged(sender, e); } }
        #endregion

        #region SetAndNotifyChange
        public static void SetAndNotifyChange<T>(INotifyPropertyChanged pthis, PropertyChangedEventHandler del, string prop, ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) { return;  }

            field = value;
            if (del != null) { del(pthis, new PropertyChangedEventArgs(prop)); }
        }
        #endregion
    }
    #endregion
    #region Reference
    internal class Reference : Reference<object> { }
    #endregion
    #region ResultReference<T>
    internal class ResultReference<T> : INotifyPropertyChanged
    {
        #region .ctor
        public ResultReference() { }
        public ResultReference(T value) { Value = value; }
        #endregion

        #region Value
        T _value;
        public T Value
        {
            get { if (_exception != null) { throw new TargetInvocationException(_exception); } return _value; }
            set { SetAndNotifyChange(this, this.PropertyChanged, "Value", ref _value, value); }
        }
        #endregion
        #region Exception
        Exception _exception;
        public Exception Exception { get { return _exception; } set { _exception = value; } }
        #endregion

        // INotifyPropertyChanged
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        void PropertyChangedDelegateImpl(object sender, PropertyChangedEventArgs e) { if (PropertyChanged != null) { PropertyChanged(sender, e); } }
        #endregion
        #region SetAndNotifyChange
        public static void SetAndNotifyChange<T>(INotifyPropertyChanged pthis, PropertyChangedEventHandler del, string prop, ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) { return; }

            field = value;
            if (del != null) { del(pthis, new PropertyChangedEventArgs(prop)); }
        }
        #endregion
    }
    #endregion
    #region ResultReference
    internal class ResultReference : ResultReference<object> { }
    #endregion

    #region WeakReference<T>
    internal class ABCWeakReference<T> : WeakReference, INotifyPropertyChanged
    {
        #region .ctor
        public ABCWeakReference(T target) : base(target) { }
        #endregion

        #region Target
        public override object Target
        {
            get { return base.Target; }
            set { base.Target = value; if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs("Target")); } }
        }
        #endregion

        #region Value
        public T Value
        {
            get { return base.Target is T ? (T)base.Target : default(T); }
        }
        #endregion

        // INotifyPropertyChanged
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
    #endregion
    #region WeakEventHandlerHandler<T>
    internal class WeakEventHandlerHandler<T>
        where T : EventArgs
    {
        #region internal state
        WeakReference _targetReference = null;
        MethodInfo _method = null;
        #endregion

        #region .ctor
        public WeakEventHandlerHandler(EventHandler<T> handler)
        {
            _targetReference = new WeakReference(handler.Target);
            _method = handler.Method;
        }
        #endregion

        #region WeakHandler
        public EventHandler<T> WeakHandler
        {
            get
            {
                return delegate (object sender, T e)
                {
                    if (_targetReference == null) { return; }
                    object target = _targetReference.Target;
                    if (target == null) { return; }

                    EventHandler<T> handler = Delegate.CreateDelegate(typeof(EventHandler<T>), target, _method) as EventHandler<T>;
                    if (handler != null) { handler(sender, e); }
                };
            }
        }
        #endregion
    }
    #endregion

    #region Pair<L, R>
    internal class Pair<L, R>
    {
        public L Left { get; set; }
        public R Right { get; set; }
    }
    #endregion

    #region SwitchOnDispose
    internal class SwitchOnDispose : IDisposable
    {
        #region internal state
        private Reference<bool> _flag;
        #endregion
        #region .ctor
        public SwitchOnDispose(Reference<bool> flag)
        {
            _flag = flag;
            _flag.Value = !_flag.Value;
        }
        public SwitchOnDispose(Reference<bool> flag, bool init)
        {
            _flag = flag;
            _flag.Value = init;
        }
        #endregion
        #region Detach
        public Reference<bool> Detach()
        {
            Reference<bool> flag = _flag;
            _flag = null;
            return flag;
        }
        #endregion
        #region Dispose
        public void Dispose()
        {
            if (_flag != null) { _flag.Value = !_flag.Value; }
        }
        #endregion
    }
    #endregion

    #region SwitchOnDispose
    internal class SwitchOnDispose<T> : IDisposable
    {
        #region internal state
        private Reference<T> _state;
        private Action<Reference<T>> _disposeAction;
        #endregion
        #region .ctor
        public SwitchOnDispose(Reference<T> state) { }
        public SwitchOnDispose(Reference<T> state, Action<Reference<T>> disposeAction)
        {
            _state = state;
            _disposeAction = disposeAction;
        }
        public SwitchOnDispose(Reference<T> state, T initValue, Action<Reference<T>> disposeAction)
        {
            _state = state;
            _state.Value = initValue;
            _disposeAction = disposeAction;
        }
        public SwitchOnDispose(Reference<T> state, Action<Reference<T>> initAction, Action<Reference<T>> disposeAction)
        {
            _state = state;
            if (initAction != null) initAction(_state);
            _disposeAction = disposeAction;
        }
        #endregion
        #region Detach
        public Reference<T> Detach()
        {
            Reference<T> state = _state;
            _state = null;
            return state;
        }
        #endregion
        #region Dispose
        public void Dispose()
        {
            if (_disposeAction!=null && _state != null) { _disposeAction(_state); }
        }
        #endregion
    }
    #endregion

    internal static class Convert2
    {
        #region ToString(object)
        public static string ToString(object value)
        {
            string ret = "null";
            if (value != null) { ret = Convert.ToString(value); }
            return ret;
        }
        #endregion
        #region ToString(string)
        public static string ToString(string value)
        {
            string ret = "null";
            if (value != null) { ret = Convert.ToString(value); }
            return ret;
        }
        #endregion

        #region ToString(object, IFormatProvider)
        public static string ToString(object value, IFormatProvider provider)
        {
            string ret = "null";
            if (value != null) { ret = Convert.ToString(value, provider); }
            return ret;
        }
        #endregion
        #region ToString(string, IFormatProvider)
        public static string ToString(string value, IFormatProvider provider)
        {
            string ret = "null";
            if (value != null) { ret = Convert.ToString(value, provider); }
            return ret;
        }
        #endregion

        public static short ToInt16(byte[] data)
        {
            var word = BitConverter.IsLittleEndian ? ArrayHelper.SwapBytesInWord(data.Clone() as byte[]) : data;
            return BitConverter.ToInt16(word, 0);
        }

        public static string GetDescription<T>(this T e) where T : IConvertible
        {
            if (e is Enum)
            {
                Type type = e.GetType();
                Array values = System.Enum.GetValues(type);

                foreach (int val in values)
                {
                    if (val == e.ToInt32(CultureInfo.InvariantCulture))
                    {
                        var memInfo = type.GetMember(type.GetEnumName(val));
                        var descriptionAttribute = memInfo[0]
                            .GetCustomAttributes(typeof(DescriptionAttribute), false)
                            .FirstOrDefault() as DescriptionAttribute;

                        if (descriptionAttribute != null)
                        {
                            return descriptionAttribute.Description;
                        }
                    }
                }
            }

            return null; // could also return string.Empty
        }

    }

    [DebuggerNonUserCode]
    internal sealed class WeakEventHandler<TEventArgs> where TEventArgs : EventArgs
    {
        private readonly WeakReference _targetReference;
        private readonly MethodInfo _method;

        public WeakEventHandler(EventHandler<TEventArgs> callback)
        {
            _method = callback.Method;
            _targetReference = new WeakReference(callback.Target, true);
        }

        [DebuggerNonUserCode]
        public void Handler(object sender, TEventArgs e)
        {
            var target = _targetReference.Target;
            if (target != null)
            {
                var callback = (Action<object, TEventArgs>)Delegate.CreateDelegate(typeof(Action<object, TEventArgs>), target, _method, true);
                if (callback != null)
                {
                    callback(sender, e);
                }
            }
        }
    }
}
