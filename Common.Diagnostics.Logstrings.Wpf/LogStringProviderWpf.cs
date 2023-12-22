using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Window = System.Windows.Window;

namespace Common
{
    public partial class LogStringProviderWpf : IProvideLogString
    {
        public static string ToLogStringInternal(Window pthis)
        {
            if (pthis == null) { return null; }
            string logString = $"{{Window:{{Name:{pthis.Name},ActualHeight:{pthis.ActualHeight},ActualWidth:{pthis.ActualWidth},AllowsTransparency:{pthis.AllowsTransparency},Background:{pthis.Background},Width:{pthis.Width},Height:{pthis.Height}}}}}";
            return logString;
        }
        public static string ToLogStringInternal(PropertyChangedEventArgs pthis)
        {
            if (pthis == null) { return null; }
            string logString = $"{{PropertyChangedEventArgs:{{PropertyName:{pthis.PropertyName}}}}}";
            return logString;
        }
        public static string ToLogStringInternal(ExecutedRoutedEventArgs pthis)
        {
            if (pthis == null) { return null; }
            string logString = $"{{ExecutedRoutedEventArgs:{{Command:{pthis.Command.GetLogString()},{pthis.Parameter.GetLogString()},RoutedEvent:{pthis.RoutedEvent.GetLogString()},Source:{pthis.Source},OriginalSource:{pthis.OriginalSource}}}}}";
            return logString;
        }
        public static string ToLogStringInternal(RoutedEventArgs pthis)
        {
            if (pthis == null) { return null; }
            string logString = $"{{RoutedEventArgs:{{RoutedEvent:{pthis.RoutedEvent.GetLogString()},Source:{pthis.Source.GetLogString()},OriginalSource:{pthis.OriginalSource.GetLogString()}}}}}";
            return logString;
        }
        public static string ToLogStringInternal(RoutedEvent pthis)
        {
            if (pthis == null) { return null; }
            string logString = $"{{RoutedEvent:{{Name:{pthis.Name},OwnerType:{pthis.OwnerType},RoutingStrategy:{pthis.RoutingStrategy},HandlerType:{pthis.HandlerType}}}}}";
            return logString;
        }
        public static string ToLogStringInternal(RoutedUICommand pthis)
        {
            if (pthis == null) { return null; }
            string logString = $"{{RoutedUICommand:{{Name:{pthis.Name},Text:{pthis.Text},OwnerType:{pthis.OwnerType},InputGestures:{pthis.InputGestures.GetLogString()}}}}}";
            return logString;
        }
        public static string ToLogStringInternal(Button pthis)
        {
            if (pthis == null) { return null; }
            string logString = $"{{Button:{{Name:{pthis.Name},Content:{pthis.Content.GetLogString()}}}}}";
            return logString;
        }

        public string ToLogString(object t, HandledEventArgs arg)
        {
            switch (t)
            {
                case Window w: arg.Handled = true; return ToLogStringInternal(w);
                case Button w: arg.Handled = true; return ToLogStringInternal(w);
                case RoutedUICommand w: arg.Handled = true; return ToLogStringInternal(w);
                case ExecutedRoutedEventArgs w: arg.Handled = true; return ToLogStringInternal(w);
                case PropertyChangedEventArgs w: arg.Handled = true; return ToLogStringInternal(w);
                case RoutedEventArgs w: arg.Handled = true; return ToLogStringInternal(w);
                case RoutedEvent w: arg.Handled = true; return ToLogStringInternal(w);

                //
                //case Thread w: arg.Handled = true; return LogstringHelper.ToLogStringInternal(w);
                //case Microsoft.Graph.Models.Application w: arg.Handled = true; return LogstringHelper.ToLogStringInternal(w);
                //case Identity w: arg.Handled = true; return LogstringHelper.ToLogStringInternal(w);
                //case TenantData w: arg.Handled = true; return LogstringHelper.ToLogStringInternal(w);
                //case TenantResource w: arg.Handled = true; return LogstringHelper.ToLogStringInternal(w);
                //case TokenCacheNotificationArgs w: arg.Handled = true; return LogstringHelper.ToLogStringInternal(w);

                //case EventProcessorClient w: arg.Handled = true; return LogstringHelper.ToLogStringInternal(w);
                default:
                    break;
            }
            return null;
        }
    }
}
