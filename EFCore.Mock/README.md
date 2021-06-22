# EFCore Mock

Mocking EF Core is difficult with InMemory as it does not support Raw SQL. Real life projects are way too complicated to be tested directly on in memory.

So we have made a Sql LocalDB mocking library which you can use to create unit tests against localdb.

## Features

1. Create a new database for given version first time. And run migrations. Since creation of database will be slower, to speed up process, we detach the database and keep copy.
2. For every unit test, we copy the detached database as a new database and attach to server as a new localdb instance.
3. Run the unit test and delete the database after end of it.
4. To retain copy of database, you can set `MockDatabaseContext.Current.DoNotDelete = true` in your unit test.
5. You can also get ConnectionString from `MockDatabaseContext.Current.ConnectionString` of currently active databse.


## Create Base Test

You need to create an Context that derives from your existing context. And you can write up few lines in `BaseTest` to write your unit tests.

```c#

    // Assuming you have `ShopContext` as your EF DbContext in side your actual application project
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

        public BaseTest()
        {
        }

        protected override void DumpLogs()
        {
            System.Diagnostics.Debug.WriteLine(base.GeneratedLog);
        }

        public ITestOutputHelper Writer { get; private set; }

        public AppDbTestContext CreateContext()
        {
            AppDbTestContext db = new AppDbTestContext(ConnectionString);
            
            // use EFCoreLiveMigration to create database structure
            db.MigrateSqlServer()
                .Migrate();
            Seed(db);
            return db;
        }

        public void Seed(AppDbTestContext db) {
        }
    }

```

## Write Unit Test

```c#

[TestClass]
public class UnitTest1: BaseTest {


    // Each unit test instance runs in a separate
    // database instance
    // After success or failed test, database will be deleted


    [Test]
    public async Task SampleTest1() {


        using(var db = this.CreateContext()) {
            // database is only alive within this scope...
        }
    }

    [Test]
    public async Task KeepDatabase() {

        // following line will keep database in the sql localdb
        MockDatabaseContext.Current.DoNotDelete = true;

        
    }

}


```