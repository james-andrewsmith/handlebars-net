
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Formatters.Json.Internal;

using Newtonsoft.Json;

namespace Handlebars.WebApi
{
    public class JsonWithCacheOutputFormatter : JsonOutputFormatter
    {
        public JsonWithCacheOutputFormatter(JsonSerializerSettings serializerSettings, ArrayPool<char> charPool) : base(serializerSettings, charPool)
        {
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (selectedEncoding == null)
            {
                throw new ArgumentNullException(nameof(selectedEncoding));
            }

            var response = context.HttpContext.Response;
            using (var writer = context.WriterFactory(response.Body, selectedEncoding))
            using (var textWriter = new StringWriter())
            using (var manyWriter = new MultiTextWriter(writer, textWriter))
            {
                WriteObject(manyWriter, context.Object);
                
                // Perf: call FlushAsync to call WriteAsync on the stream with any content left in the TextWriter's
                // buffers. This is better than just letting dispose handle it (which would result in a synchronous
                // write).
                await manyWriter.FlushAsync();
                 
                // Cache: ensure the resource filter has something in the cache item 
                // to us as the content of the cache when it processes this request
                if (context.HttpContext.Items.ContainsKey("cache"))
                    context.HttpContext.Items["cache"] = textWriter.ToString();
            }
        }
    }
}
