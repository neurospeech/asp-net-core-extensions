using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EF.Core.Bulk.Model
{
    public class ShopContext : DbContext
    {
        public ShopContext(DbContextOptions options)
            :base(options)
        {
            
        }

        public ShopContext():base(new DbContextOptionsBuilder<ShopContext>()
            .UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Shop;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False")
            .Options)
        {

        }

       
        public DbSet<Product> Products { get; set; }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<ProductAccount> ProductAccounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductAccount>().HasKey(x => new { x.AccountID, x.ProductID });

            modelBuilder.Entity<Product>().Property(x => x.Archived).HasDefaultValueSql("0");
        }
    }

    [Table("Products")]
    public class Product {

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ProductID { get; set; }

        public string Name { get; set; }

        
        public bool Archived { get; set; }

        [InverseProperty(nameof(ProductAccount.Product))]
        public ICollection<ProductAccount> ProductAccounts { get; set; }

    }

    [Table("ProductAccounts")]
    public class ProductAccount {

        public long ProductID { get; set; }

        public long AccountID { get; set; }

        [ForeignKey(nameof(ProductID))]
        public Product Product { get; set; }

        [ForeignKey(nameof(AccountID))]
        public Account Account { get; set; }
    }

    [Table("Accounts")]
    public class Account {

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AccountID { get; set; }

        public string Name { get; set; }

        public bool Archived { get; set; }

        [InverseProperty(nameof(ProductAccount.Account))]
        public ICollection<ProductAccount> ProductAccounts { get; set; }
    }
}
