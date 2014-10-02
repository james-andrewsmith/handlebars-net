using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace HandlebarsViewEngine
{
    public static class JsonHelper
    {
        public static string ToJson(this object o)
        {
            return JsonConvert.SerializeObject(o, JsonExtensions.Formatting, JsonExtensions.GetSettings());
        }

        public static string ToJsonAsPretty(this object o)
        {
            return JsonConvert.SerializeObject(o, Formatting.Indented, JsonExtensions.GetSettings());
        }

        public static string ToJsonAsCompact(this object o)
        {
            return JsonConvert.SerializeObject(o, Formatting.None, JsonExtensions.GetSettings());
        }

        public static T FromJson<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(json, JsonExtensions.Settings);
        }
    }

    public static class JsonExtensions
    {         
        public static JsonSerializerSettings Settings
        {
            get
            {
                return GetSettings();
            }
        }

        private static JsonSerializerSettings _Settings;

        public static JsonSerializerSettings GetSettings()
        {
            if (_Settings != null)
                return _Settings;

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new UnderscoreMappingResolver()
            };
            settings.Converters.Add(new StringEnumConverter
            {
                CamelCaseText = false
            });
            _Settings = settings;
            return settings;
        }

#if DEBUG
        public static Formatting Formatting = Newtonsoft.Json.Formatting.Indented;
#else
        public static Formatting Formatting = Newtonsoft.Json.Formatting.None;
#endif
    }

    public class UnderscoreMappingResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return System.Text.RegularExpressions.Regex.Replace(propertyName, @"([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", "$1$3_$2$4").ToLower();
        }
    }
}