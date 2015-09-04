using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

#if __MonoCS__
#else
using ProtoBuf;
#endif

namespace Handlebars.Proxy
{
#if __MonoCS__
#else
    [ProtoContract]
    public sealed class LocalCacheItem
    {
        [ProtoMember(1)]
        public DateTime Created
        {
            get;
            set;
        }

        [ProtoMember(2)]
        public Dictionary<string, string> Headers
        {
            get;
            set;
        }

        [ProtoMember(3)]
        public byte[] ContentResponse
        {
            get;
            set;
        }

    }
#endif

    public static class WebClientExtensions
    {
        public static Dictionary<string, string> GetHeaders(this WebHeaderCollection webHeaderCollection)
        {
            string[] keys = webHeaderCollection.AllKeys;
            var keyVals = new Dictionary<string, string>(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                keyVals.Add(keys[i], webHeaderCollection[keys[i]]);
            }
            return keyVals;
        }

        private static string RemoveDiacritics(string value)
        {
            if (String.IsNullOrEmpty(value))
                return value;

            string normalized = value.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char c in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            Encoding nonunicode = Encoding.GetEncoding(850);
            Encoding unicode = Encoding.Unicode;

            byte[] nonunicodeBytes = Encoding.Convert(unicode, nonunicode, unicode.GetBytes(sb.ToString()));
            char[] nonunicodeChars = new char[nonunicode.GetCharCount(nonunicodeBytes, 0, nonunicodeBytes.Length)];
            nonunicode.GetChars(nonunicodeBytes, 0, nonunicodeBytes.Length, nonunicodeChars, 0);

            return new string(nonunicodeChars);
        }

        public static string Urlify(this string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            return RemoveDiacritics(url).ToLower()
                                        .Replace("&", "-and-")
                                        .Replace("+", "-and-")
                                        .Replace(' ', '-')
                                        .Replace('*', '-')
                                        .Replace('(', '-')
                                        .Replace(')', '-')
                                        .Replace('[', '-')
                                        .Replace(']', '-')
                                        .Replace('{', '-')
                                        .Replace('}', '-')
                // .Replace('|', '-')
                                        .Replace("'", "")
                                        .Replace("·", "-")
                                        .Replace("/", "")
                                        .Replace("_", "-")
                                        .Replace("=", "-")
                                        .Replace(",", "-")
                                        .Replace(":", "-")
                                        .Replace(";", "-")
                                        .Replace("?", "-")
                                        .Replace("%", "-")
                                        .Replace("#", "-")
                                        .Replace("\\", "-")
                                        .Replace("\"", "-")
                                        .Replace("------", "-")
                                        .Replace("-----", "-")
                                        .Replace("----", "-")
                                        .Replace("----", "-")
                                        .Replace("---", "-")
                                        .Replace("--", "-")
                                        .Replace("-", "-")
                                        .Trim('-', ' ');
        }
    }
    
#if __MonoCS__
#else
    public sealed class WebClientWithLocalCache : IDisposable
    {

        #region // Constructors //
        public WebClientWithLocalCache()
        {
            _client = new WebClient();
        }
        #endregion

        #region // Properties //
        private WebClient _client;
        private WebHeaderCollection _responseHeaders;

        public WebHeaderCollection ResponseHeaders
        {
            get { return _responseHeaders == null ? _client.ResponseHeaders : _responseHeaders; }
        }

        public WebHeaderCollection Headers
        {
            get { return _client.Headers; }
            set { _client.Headers = value; }
        }

        #endregion

        #region // Methods //

        private string GetLocalAddressPath(Uri address)
        {
            // Directory.CreateDirectory(address.DnsSafeHost.Urlify());
            return address.DnsSafeHost.Urlify() + 
                   "\\" + 
                   address.PathAndQuery.ToString().Urlify() + 
                   ".cache";
        }

        public byte[] DownloadData(string address)
        {
            return DownloadData(new Uri(address));            
        }

        public byte[] DownloadData(Uri address)
        {
            var localAddress = GetLocalAddressPath(address);
            if (File.Exists(localAddress)) // && ViewEngineProxy.Configuration.Instance.Offline)
            {
                using (var ms = new MemoryStream(File.ReadAllBytes(localAddress)))
                {
                    var localCacheItem = Serializer.Deserialize<LocalCacheItem>(ms);

                    _responseHeaders = new System.Net.WebHeaderCollection();
                    foreach (var header in localCacheItem.Headers)
                        _responseHeaders.Add(header.Key, header.Value);

                    return localCacheItem.ContentResponse;
                }
            }

            var data = _client.DownloadData(address);

            using (var ms = new MemoryStream())
            {
                var localCacheItem = new LocalCacheItem();
                localCacheItem.ContentResponse = data;
                localCacheItem.Created = DateTime.Now;
                localCacheItem.Headers = _client.ResponseHeaders.GetHeaders();

                Serializer.Serialize<LocalCacheItem>(ms, localCacheItem);
                
                if (!Directory.Exists(Path.GetDirectoryName(localAddress)))
                    Directory.CreateDirectory(Path.GetDirectoryName(localAddress));

                File.WriteAllBytes(localAddress, ms.ToArray());
            }

            return data;
        }

        #endregion

        #region // Disposable //

        public void Dispose()
        {
            _client.Dispose();
        }

        #endregion

    }
#endif
}
