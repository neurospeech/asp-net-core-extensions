using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching;
using NeuroSpeech;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NodeServerTest
{
    //public class MockCacheEntry: ICacheEntry
    //{

    //}

    //public class MockCache : IMemoryCache
    //{
    //    public ICacheEntry CreateEntry(object key)
    //    {
    //        return null;
    //    }

    //    public void Dispose()
    //    {
            
    //    }

    //    public void Remove(object key)
    //    {
            
    //    }

    //    public bool TryGetValue(object key, out object value)
    //    {
    //        value = null;
    //        return false;
    //    }
    //}

    public class InstallTest
    {

        // public IServiceProvider sp { get; }
        // [Fact]
        public async Task InstallAsync()
        {
            var serviceProvider = new ServiceCollection()
                .BuildServiceProvider();

            var server = new NodePackageService(serviceProvider, new NodePackageServiceOptions
            {
                TempFolder = "D:\\tempg\\" + Guid.NewGuid(),
                NPMRegistry = "https://proget-2018-10-29-ns.800casting.com/npm/NS-NPM/",
                PrivatePackages = new string[] {
                    "@c8private/email-templates@1.0.14",
                    "@c8private/lang@1.0.1"
                }
            });

            var package = await server.GetInstalledPackageAsync("@c8private/email-templates@1.0.14");
            Assert.NotNull(package);
        }

    }
}
