using EFCoreBulk.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace EFCoreBulk.Tests
{
    public class UnitTest1: BaseTest
    {
        public UnitTest1(ITestOutputHelper writer) : base(writer)
        {

        }

        [Fact]
        public async Task BulkUpdate()
        {
            using (var db = this.CreateContext())
            {
                await db.ProductAccounts
                    .Where(x => x.Account.Archived != false)
                    .Select(x => x.Product)
                    .Select(x => new Product {
                        Archived = true
                    })
                    .UpdateAsync();
            }
        }

        [Fact]
        public async Task BulkUpdate3()
        {
            using (var db = this.CreateContext())
            {
                await db.ProductAccounts
                    .Where(x => x.Account.Archived != false)
                    .Select(x => x.Product)
                    .Select(x => new Product
                    {
                        Archived = true
                    })
                    .UpdateAsync();
            }
        }


        [Fact]
        public async Task BulkUpdateDate()
        {
            using (var db = this.CreateContext())
            {
                var now = DateTime.UtcNow;

                await db.ProductAccounts
                    .Where(x => x.Account.Archived != false)
                    .Select(x => x.Product)
                    .Select(x => new Product
                    {
                        LastUpdated = now
                    })
                    .UpdateAsync();
            }
        }
        [Fact]
        public async Task BulkInsert()
        {
            using (var db = this.CreateContext()) {

                var googleProducts = db.Products.Where(p => p.Name.StartsWith("google"));

                int count = await db.InsertAsync(googleProducts.Select(p => new Product {
                    Name = p.Name + " V2"
                }));

                Assert.Equal(2, count);

                count = await db.Products.CountAsync();

                Assert.Equal(6, count);

                count = await googleProducts.Select(p => new Product {
                    Archived = true
                }).UpdateAsync();

                Assert.Equal(4, count);

                count = await db.Products.Where(x => x.Archived == true).CountAsync();

                Assert.Equal(4, count);

                count = await db.DeleteAsync(db.Products.Where(x => x.Archived == true));

                Assert.Equal(4, count);

                count = await db.Products.Where(x => x.Archived == true).CountAsync();

                Assert.Equal(0, count);

                count = await db.Products.CountAsync();

                Assert.Equal(2, count);
            }
        }
    }
}
