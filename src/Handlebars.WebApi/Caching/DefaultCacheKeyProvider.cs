using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using System.Data.HashFunction;
using System.Data.HashFunction.Utilities;

namespace Handlebars.WebApi
{
    public class DefaultCacheKeyProvider : ICacheKeyProvider
    {
        #region // Constructor //
        public DefaultCacheKeyProvider()
        {
            _murmur3 = new MurmurHash3();            
        }
        #endregion

        #region // Dependency Injection //
        private readonly MurmurHash3 _murmur3; 
        #endregion 

        public async Task<string> GetKey(HttpContext context, CacheControlOptions options, string hash)
        {
            return $"{context.Request.Path.Value}:{hash}";
        }

        /// <summary>
        /// This function encaptulates the business logic of the applications caching rules, 
        /// it performs cache lookups etc to build keys which represent the unique state 
        /// of the id's passed in via the "Build Hash With" variable. 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<string[], string[]>> GetKeyValue(HttpContext context, CacheControlOptions options)
        {
            if (options.BuildHashWith == null)
                return new KeyValuePair<string[], string[]>();

            var keys = new string[options.BuildHashWith.Length];
            var values = new string[options.BuildHashWith.Length];
            for (var i = 0; i < options.BuildHashWith.Length; i ++)
            {
                keys[i] = i.ToString();
                values[i] = i.ToString();
            }
            return new KeyValuePair<string[], string[]>(keys, values);
        }

        /// <summary>
        /// The hash calculation also uses items from querystring, routedata, httpcontext items, 
        /// the identity, the roles the identity has claimed, potenally the version of the assembly
        /// and then the custom set which has been returned from the GetKeyValue function in this 
        /// class.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="set"></param>
        /// <returns></returns>
        public async Task<string> GetHashOfValue(HttpContext context,
                                                 CacheControlOptions options,
                                                 string[] set)
        {
            // Perf: use allocation whenever possible
            var sb = new StringBuilder();

            // Todo: find a way to avoid this sort and use an array instead
            if (set != null && set.Length > 0)
            {
                foreach (var k in set.OrderBy(_ => _, StringComparer.OrdinalIgnoreCase))
                {
                    sb.Append(string.IsNullOrEmpty(k) ? "<null>" : k.Trim('\n', ' ') + ";");
                }
            }

            if (options.VaryByUser)
            {
                sb.Append($"user:{(context.User == null ? "<null>" : context.User.Identity.Name)};");
            }

            for (int i = 0; i < options.VaryByItem?.Length; i++)
            {
                var value = "<null>";
                if (context.Items.ContainsKey(options.VaryByItem[i]))
                    value = Convert.ToString(context.Items[options.VaryByItem[i]]);
                sb.Append($"{value};");
            }

            for (int i = 0; i < options.VaryByRole?.Length; i++)
            {
                var value = "<null>";
                if (context.User != null)
                    value = context.User.IsInRole(options.VaryByRole[i]).ToString();
                sb.Append($"{value};");
            }

            for (int i = 0; i < options.VaryByQuery?.Length; i++)
            {
                var value = "<null>";
                if (context.Request.Query.ContainsKey(options.VaryByQuery[i]))
                    value = context.Request.Query[options.VaryByQuery[i]];
                sb.Append($"{value};");
            }

            var routing = context.Features.Get<IRoutingFeature>();
            var routeData = routing.RouteData.Values;

            for (int i = 0; i < options.VaryByRoute?.Length; i++)
            {
                var value = "<null>";
                if (routeData.ContainsKey(options.VaryByRoute[i]))
                    value = Convert.ToString(routeData[options.VaryByRoute[i]]);
                sb.Append($"{value};");
            }

            // This is highly optimised given the frequency with which it is called 
            // which is almost every request 
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())))
            {
                var bytes = await _murmur3.ComputeHashAsync(ms);
                var hash = FastBitConverter.ByteArrayToHexViaLookup32Unsafe(bytes);
                return hash;
            }
        }


    }

    public unsafe class FastBitConverter
    {
        private static readonly uint[] _lookup32Unsafe = CreateLookup32Unsafe();
        private static readonly uint* _lookup32UnsafeP = (uint*)System.Runtime.InteropServices.GCHandle.Alloc(_lookup32Unsafe, System.Runtime.InteropServices.GCHandleType.Pinned).AddrOfPinnedObject();

        private static uint[] CreateLookup32Unsafe()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                if (BitConverter.IsLittleEndian)
                    result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
                else
                    result[i] = ((uint)s[1]) + ((uint)s[0] << 16);
            }
            return result;
        }

        public static string ByteArrayToHexViaLookup32Unsafe(byte[] bytes)
        {
            var lookupP = _lookup32UnsafeP;
            var result = new char[bytes.Length * 2];
            fixed (byte* bytesP = bytes)
            fixed (char* resultP = result)
            {
                uint* resultP2 = (uint*)resultP;
                for (int i = 0; i < bytes.Length; i++)
                {
                    resultP2[i] = lookupP[bytesP[i]];
                }
            }
            return new string(result);
        }
    }

}
