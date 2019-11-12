using EFCoreBulk.Model;
using Microsoft.EntityFrameworkCore;
using NeuroSpeech.EFCore.Mock;
using NeuroSpeech.EFCoreLiveMigration;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace EFCoreBulk.Tests
{
    // Assuming you have `AppDbContext` as your EF DbContext in side your actual application project
    // For test purposes, you will have to use AppDbTestContext 
    // or you can use AppDbTestContext as dependency in your DI container
    public class AppDbTestContext : ShopContext
    {

        // this is important
        // since databases are dynamically created and destroyed
        // MockDatabaseContext.Current.ConnectionString contains 
        // correct database for current test context

        // MockDatabaseContext.Current will work correctly with async await
        // without worrying about passing context

        public AppDbTestContext(DbContextOptions options): base(options)
        {

        }

        public AppDbTestContext(): base(Create(MockDatabaseContext.Current.ConnectionString))
        {

        }

        public AppDbTestContext(string cnstr) : base(Create(cnstr)) {

        }

        public static DbContextOptions<AppDbTestContext> Create(string connectionString) {
            DbContextOptionsBuilder<AppDbTestContext> builder = new DbContextOptionsBuilder<AppDbTestContext>();
            builder.UseSqlServer(connectionString);
            return builder.Options;
        }
    }


    public abstract class BaseTest : 
        MockSqlDatabaseContext<AppDbTestContext>
    {

        public BaseTest(ITestOutputHelper writer)
        {
            this.Writer = writer;
        }

        protected override void DumpLogs()
        {
            this.Writer.WriteLine(base.GeneratedLog);
        }

        public ITestOutputHelper Writer { get; private set; }

        public AppDbTestContext CreateContext()
        {
            AppDbTestContext db = new AppDbTestContext(ConnectionString);
            // db.Database.EnsureCreated();
            MigrationHelper.ForSqlServer(db).Migrate();
            Seed(db);
            return db;
        }

        public void Seed(AppDbTestContext db) {
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
    }
}
