using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RetroCoreFit.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1Async()
        {

            var api = RetroClient.Create<IApi,TestBaseService>(new Uri("https://m.800casting.com"),new HttpClient(new TestHttpClient()));

            Assert.Null(api.Authorize);

            api.Authorize = "a";

            Assert.Equal("a", api.Authorize);

            var r = await api.UpdateAsync(1,new Product { }, " all , ackava@gmail.com");

            
            

        }

        public class TestHttpClient : HttpMessageHandler {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // return base.SendAsync(request, cancellationToken);

                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                });
            }
        }

        public class TestBaseService : BaseService {

            public TestBaseService()
            {
                this.BaseUrl = new Uri("https://m.800casting.com/");
            }

            protected override Task<T> InvokeAsync<T>(HttpMethod method, string path, IEnumerable<RestParameter> plist)
            {
                return base.InvokeAsync<T>(method, path, plist);
            }

        }

        public interface IApi
        {

            [Header("Authorize")]
            string Authorize { get; set; }

            [Put("products/{id}/edit")]
            Task<Product> UpdateAsync(
                [Path("id")] long productId,
                [Body] Product product, 
                [Query] string email = null);

        }

        public class Product {
            public string Name { get; set; }
        }
    }
}
