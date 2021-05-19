using LiveMigrationConsole.Models;
using Microsoft.EntityFrameworkCore;
using NeuroSpeech.EFCoreLiveMigration;
using System;
using System.Collections.Generic;

namespace LiveMigrationConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            DbContextOptionsBuilder<ERPContext> options = new DbContextOptionsBuilder<ERPContext>();
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ERPModel;Trusted_Connection=True;MultipleActiveResultSets=true");

            using (var db = new ERPContext(options.Options)) {

                MigrationHelper.ForSqlServer(db).Migrate();

                var acc = new Account {
                    DisplayName = "A",
                    AccountType = "Admin",
                    Events = new List<AccountEvent>
                    {
                        new AccountEvent{ 
                             Start = DateTimeOffset.UtcNow,
                             End = DateTimeOffset.UtcNow.AddDays(1),
                             IsBusy = true
                        }
                    }
                };

                db.Accounts.Add(acc);

                db.SaveChanges();

            }

        }
    }
}
