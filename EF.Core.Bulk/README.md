[![Build status](https://ci.appveyor.com/api/projects/status/y19dkdfjl3lkokvp?svg=true)](https://ci.appveyor.com/project/neurospeech/ef-core-bulk)

[![NuGet](https://img.shields.io/nuget/v/EF.Core.Bulk.svg?label=NuGet)](https://www.nuget.org/packages/EF.Core.Bulk)


# EF-Core-Bulk
EF Core bulk operations - INSERT SELECT, UPDATE SELECT , DELETE SELECT

```c#
            using (var db = this.CreateContext()) {
            
                Seed(db);

                var googleProducts = db.Products.Where(p => p.Name.StartsWith("google"));


                // Copy all products whose name starts with google.
                // effective SQL is 
                // INSERT INTO Products (Name)
                //    (SELECT P.Name + 'V2' as Name FROM Products as P Where P.Name LIKE 'Google%' )
                int count = await db.InsertAsync(googleProducts.Select(p => new Product {
                    Name = p.Name + " V2"
                }));

                Assert.Equal(2, count);

                count = await db.Products.CountAsync();

                Assert.Equal(6, count);

                // Archive all products whose name starts with google
                // effective SQL is
                // UPDATE P SET P.Archived = 1 FROM Products as P WHERE P.Name LIKE 'Google%'
                //
                count = await db.UpdateAsync(googleProducts.Select(p => new Product {
                    Archived = true
                }));

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
            
         void Seed(AppDbContext db) {
             db.Products.AddRange(new Product {
                Name = "Apple iPhone"
            },
            new Product
            {
                Name = "Google Pixel"
            },
            new Product
            {
                Name = "Microsoft Surface"
            }, 
            new Product {
                Name = "Google Pixel Large"
            });

            db.SaveChanges();

        }

```
