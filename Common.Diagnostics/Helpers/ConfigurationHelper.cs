#region using
//using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
#endregion

namespace Common
{
    internal static partial class ConfigurationHelper
    {
        #region constants
        private const string E_ARGUMENTNULLEXCEPTION = "argument '{name}' cannot be null";
        private const string S_NAME_PLACEHOLDER = "{name}";
        #endregion
        //public static IConfiguration Configuration { get; internal set; }

        static ConfigurationHelper() { }

        //public static void Init(IConfiguration configuration)
        //{
        //    ConfigurationHelper.Configuration = configuration;
        //}

        //#region GetSetting
        //public static T GetSetting<T>(string key, T defaultValue = default(T))
        //{
        //    var configurationValue = Configuration.GetValue($"AppSettings:{key}", defaultValue);
        //    return configurationValue;
        //}
        //#endregion
        //#region GetClassSetting
        //public static TRet GetClassSetting<TClass, TRet>(string name, TRet defaultValue = default(TRet), CultureInfo culture = null, string suffix = null)
        //{
        //    if (culture == null) { culture = CultureInfo.CurrentCulture; }

        //    var ret = defaultValue;
        //    try
        //    {
        //        var type = typeof(TClass); Assembly assembly = type.Assembly;
        //        var assemblyName = GetConfigName(assembly); var className = GetConfigName(type);
        //        var specificName = default(string);
        //        var sectionName = default(string);
        //        var groupName = default(string);

        //        var valueString = default(string);
        //        if (!string.IsNullOrEmpty(suffix))
        //        {
        //            specificName = $"{assemblyName}.{className}.{suffix}.{name}";
        //            sectionName = $"{className}.{suffix}.{name}";
        //            groupName = $"{suffix}.{name}";

        //            valueString = GetSetting<string>(specificName, null);
        //            if (valueString == null) { valueString = GetSetting<string>(sectionName, null); }
        //            if (valueString == null) { valueString = GetSetting<string>(groupName, null); }
        //        }

        //        specificName = $"{assemblyName}.{className}.{name}";
        //        sectionName = $"{className}.{name}";
        //        groupName = $"{name}";
        //        if (valueString == null) { valueString = GetSetting<string>(specificName, null); }
        //        if (valueString == null) { valueString = GetSetting<string>(sectionName, null); }
        //        if (valueString == null) { valueString = GetSetting<string>(groupName, null); }
        //        if (valueString == null) { return ret; }

        //        var converter = TypeDescriptor.GetConverter(typeof(TRet));
        //        ret = (TRet)converter.ConvertFrom(null, culture, valueString);
        //        return ret;
        //    }
        //    catch (Exception ex) { TraceManager.Exception(ex); return ret; }
        //    finally { TraceManager.Trace($"GetClassSetting('{name}') returned '{ret.GetLogString()}'", "config"); }
        //}
        //#endregion

        #region GetConfigName
        ///<summary>gets the name of the assembly to be used as a configsection name or a prefix for appettings values.</summary>
        public static string GetConfigName(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly", E_ARGUMENTNULLEXCEPTION.Replace(S_NAME_PLACEHOLDER, "assembly"));

            ConfigurationNameAttribute assemblyConfignameAttribute = (ConfigurationNameAttribute)Attribute.GetCustomAttribute(assembly, typeof(ConfigurationNameAttribute));
            if (assemblyConfignameAttribute != null) { return assemblyConfignameAttribute.Name; } else { return assembly.GetName().Name; }
        }
        ///<summary>gets the name of the configuration section associated with the class.</summary>
        public static string GetConfigName(Type type)
        {
            if (type == null) throw new ArgumentNullException("type", E_ARGUMENTNULLEXCEPTION.Replace(S_NAME_PLACEHOLDER, "type"));

            ConfigurationNameAttribute classConfignameAttribute = (ConfigurationNameAttribute)Attribute.GetCustomAttribute(type, typeof(ConfigurationNameAttribute));
            if (classConfignameAttribute != null) { return classConfignameAttribute.Name; } else { return type.Name; }
        }
        #endregion
    }

    #region ConfigurationNameAttribute
    ///<summary>this attribute is used to define a name for an assembly or class to be used in configuration files.</summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
    internal sealed class ConfigurationNameAttribute : Attribute
    {
        #region internal state
        private string _name;
        #endregion
        #region construction
        ///<summary>constructs the attribute: gets a reference to the name.</summary>
        public ConfigurationNameAttribute(string name) { _name = name; }
        #endregion
        #region properties
        ///<summary>returns the name to be used for the assembly or the class.</summary>
        public string Name { get { return _name; } set { _name = value; } }
        #endregion
    }
    #endregion
}
