#region using
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
#endregion

namespace Common
{
    internal partial class IdentityLogstringImpl : IProvideLogString
    {
        protected static IdentityLogstringImpl Current { get; set; }
        static IdentityLogstringImpl()
        {
            IdentityLogstringImpl.Current = new IdentityLogstringImpl();
            LogStringExtensions.RegisterLogstringProvider(IdentityLogstringImpl.Current);
        }

        public static string ToLogStringInternal(IPublicClientApplication pthis)
        {
            string logString = $"{{IPublicClientApplication:{{UserTokenCache:{pthis.UserTokenCache},AppConfig:{pthis.AppConfig},Authority:{pthis.Authority},IsSystemWebViewAvailable:{pthis.IsSystemWebViewAvailable},UserTokenCache:{pthis.UserTokenCache}}}}}";
            return logString;
        }
        public static string ToLogStringInternal(IAccount pthis)
        {
            string logString = $"{{IAccount:{{Username:{pthis.Username},Environment:{pthis.Environment},HomeAccountId:{pthis.HomeAccountId.GetLogString()}}}}}";
            return logString;
        }
        public static string ToLogStringInternal(AuthenticationResult pthis)
        {
            string logString = $"{{AuthenticationResult:{{UniqueId:{pthis.UniqueId},AccessToken:{pthis.AccessToken},Scopes:{pthis.Scopes.GetLogString()},TenantId:{pthis.TenantId},CorrelationId:{pthis.CorrelationId},Account:{pthis.Account.GetLogString()},AuthenticationResultMetadata:{pthis.AuthenticationResultMetadata.GetLogString()},ExpiresOn:{pthis.ExpiresOn},ExtendedExpiresOn:{pthis.ExtendedExpiresOn},IdToken:{pthis.IdToken},IsExtendedLifeTimeToken:{pthis.IsExtendedLifeTimeToken}}}}}";
            return logString;
        }
        //public static string ToLogStringInternal(Identity pthis)
        //{
        //    string logString = $"{{Identity:{{Name:{pthis.Name},Email:{pthis.Email},Upn:{pthis.Upn},Manager:{pthis.Manager.GetLogString()}}}}}";
        //    return logString;
        //}

        public string ToLogString(object t, HandledEventArgs arg)
        {
            switch (t)
            {
                //case ApplicationInfo obj: arg.Handled = true; return global::Common.LogstringHelper.ToLogStringInternal(obj);
                //case Label obj: arg.Handled = true; return global::Common.LogstringHelper.ToLogStringInternal(obj);
                //case Common.FileOptions obj: arg.Handled = true; return global::Common.LogstringHelper.ToLogStringInternal(obj);
                // MSAL
                //case Common.Identity obj: arg.Handled = true; return Common.LogstringImpl.ToLogStringInternal(obj);
                case IPublicClientApplication obj: arg.Handled = true; return Common.IdentityLogstringImpl.ToLogStringInternal(obj);
                case IAccount obj: arg.Handled = true; return Common.IdentityLogstringImpl.ToLogStringInternal(obj);
                case AuthenticationResult obj: arg.Handled = true; return Common.IdentityLogstringImpl.ToLogStringInternal(obj);
                default: break;
            }
            return null;
        }
    }
}
