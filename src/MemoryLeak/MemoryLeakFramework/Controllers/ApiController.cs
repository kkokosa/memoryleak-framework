using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebPages;
using Microsoft.Extensions.FileProviders;

namespace MemoryLeakFramework.Controllers
{
    public class WebApiController : ApiController
    {
        public WebApiController()
        {
            Interlocked.Increment(ref DiagnosticsController.Requests);
        }

        private static ConcurrentBag<string> _staticStrings = new ConcurrentBag<string>();

        [Route("api/staticstring")]
        public IHttpActionResult GetStaticString()
        {
            var bigString = new String('x', 10 * 1024);
            _staticStrings.Add(bigString);
            return Ok(bigString);
        }

        [Route("api/bigstring")]
        public IHttpActionResult GetBigString()
        {
            return Ok(new String('x', 10 * 1024));
        }

        [Route("api/loh/{size=85000}")]
        public int GetLOH(int size)
        {
            return new byte[size].Length;
        }

        private static readonly string TempPath = Path.GetTempPath();

        [Route("api/fileprovider")]
        public void GetFileProvider()
        {
            var fp = new PhysicalFileProvider(TempPath);
            fp.Watch("*.*");
        }

        [Route("api/httpclient1")]
        public async Task<int> GetHttpClient1(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.GetAsync(url);
                return (int)result.StatusCode;
            }
        }

        private static readonly HttpClient _httpClient = new HttpClient();

        [Route("api/httpclient2")]
        public async Task<int> GetHttpClient2(string url)
        {
            var result = await _httpClient.GetAsync(url);
            return (int)result.StatusCode;
        }

        [Route("api/array/{size}")]
        public byte[] GetArray(int size)
        {
            var array = new byte[size];

            var random = new Random();
            random.NextBytes(array);

            return array;
        }

        private static ArrayPool<byte> _arrayPool = ArrayPool<byte>.Create();

        private class PooledArray : IDisposable
        {
            public byte[] Array { get; private set; }

            public PooledArray(int size)
            {
                Array = _arrayPool.Rent(size);
            }

            public void Dispose()
            {
                _arrayPool.Return(Array);
            }
        }

        [Route("api/pooledarray/{size}")]
        public byte[] GetPooledArray(int size)
        {
            var pooledArray = new PooledArray(size);

            var random = new Random();
            random.NextBytes(pooledArray.Array);

            HttpContext httpContext = HttpContext.Current;
            if (httpContext != null)
            {
                HttpContextWrapper httpContextWrapper = new HttpContextWrapper(httpContext);
                httpContextWrapper.RegisterForDispose(pooledArray);
            }

            return pooledArray.Array;
        }
    }
}
