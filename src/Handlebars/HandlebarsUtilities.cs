using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Handlebars
{
    public static class HandlebarsUtilities
    {
        /// <summary>
        /// Returns the index of the start of the contents in a StringBuilder
        /// </summary>        
        /// <param name="value">The string to find</param>
        /// <param name="startIndex">The starting index.</param>
        /// <param name="ignoreCase">if set to <c>true</c> it will ignore case</param>
        /// <see cref="http://stackoverflow.com/questions/1359948/why-doesnt-stringbuilder-have-indexof-method"/>
        /// <returns></returns>
        public static int IndexOf(this StringBuilder sb, string value, int startIndex, bool ignoreCase)
        {
            int index;
            int length = value.Length;
            int maxSearchLength = (sb.Length - length) + 1;

            if (ignoreCase)
            {
                for (int i = startIndex; i < maxSearchLength; ++i)
                {
                    if (Char.ToLower(sb[i]) == Char.ToLower(value[0]))
                    {
                        index = 1;
                        while ((index < length) && (Char.ToLower(sb[i + index]) == Char.ToLower(value[index])))
                            ++index;

                        if (index == length)
                            return i;
                    }
                }

                return -1;
            }

            for (int i = startIndex; i < maxSearchLength; ++i)
            {
                if (sb[i] == value[0])
                {
                    index = 1;
                    while ((index < length) && (sb[i + index] == value[index]))
                        ++index;

                    if (index == length)
                        return i;
                }
            }

            return -1;
        }   


        // public static string ToJavaScriptString(String instr)
        // {
        //     return instr.Replace("'", @"\'")
        //                 .Replace(@"""", @"\""");
        // } 

        public static string ToJavaScriptString(string value)
        {
            return ToJavaScriptString(value, false);
        }

        public static string ToJavaScriptString(string value, bool addDoubleQuotes)
        {
            if (String.IsNullOrEmpty(value))
                return addDoubleQuotes ? "\"\"" : String.Empty;

            int len = value.Length;
            bool needEncode = false;
            char c;
            for (int i = 0; i < len; i++)
            {
                c = value[i];

                if (c >= 0 && c <= 31 || c == 34 || c == 39 || c == 60 || c == 62 || c == 92)
                {
                    needEncode = true;
                    break;
                }
            }

            if (!needEncode)
                return addDoubleQuotes ? "\"" + value + "\"" : value;

            var sb = new StringBuilder();
            if (addDoubleQuotes)
                sb.Append('"');

            for (int i = 0; i < len; i++)
            {
                c = value[i];
                if (c >= 0 && c <= 7 || c == 11 || c >= 14 && c <= 31 || c == 39 || c == 60 || c == 62)
                    sb.AppendFormat("\\u{0:x4}", (int)c);
                else switch ((int)c)
                    {
                        case 8:
                            sb.Append("\\b");
                            break;

                        case 9:
                            sb.Append("\\t");
                            break;

                        case 10:
                            sb.Append("\\n");
                            break;

                        case 12:
                            sb.Append("\\f");
                            break;

                        case 13:
                            sb.Append("\\r");
                            break;

                        case 34:
                            sb.Append("\\\"");
                            break;

                        case 92:
                            sb.Append("\\\\");
                            break;

                        default:
                            sb.Append(c);
                            break;
                    }
            }

            if (addDoubleQuotes)
                sb.Append('"');

            return sb.ToString();
        }


        public static string ToJson(object context)
        {
            return JsonConvert.SerializeObject(context, Formatting, GetSettings());
        }


        private static JsonSerializerSettings _Settings;
        private static JsonSerializerSettings GetSettings()
        {
            if (_Settings != null)
                return _Settings;

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new ModelMappingResolver()
            };
            settings.Converters.Add(new StringEnumConverter
            {
                CamelCaseText = false
            });
            _Settings = settings;
            return settings;
        }


        private static Formatting Formatting = Formatting.Indented;

        private class ModelMappingResolver : DefaultContractResolver
        {
            public ModelMappingResolver()
                // : base(true)
            {
                this.DefaultMembersSearchFlags = BindingFlags.Public | BindingFlags.Instance;
            }

            private static readonly System.Text.RegularExpressions.Regex UnderscoreReplacement = new System.Text.RegularExpressions.Regex(@"([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", System.Text.RegularExpressions.RegexOptions.Compiled);

            protected override string ResolvePropertyName(string propertyName)
            {
                return UnderscoreReplacement.Replace(propertyName, "$1$3_$2$4")
                                            .ToLower();
                // return System.Text.RegularExpressions.Regex.Replace(propertyName, @"([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", "$1$3_$2$4").ToLower();
            }
        }
    }


}
