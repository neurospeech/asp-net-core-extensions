using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFCoreBulk.Model
{
    public class ShopContext : DbContext
    {
        public ShopContext(DbContextOptions options)
            :base(options)
        {
            
        }

        public ShopContext():base(new DbContextOptionsBuilder<ShopContext>()
            .UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Shop2;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False")
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

        public DateTime? LastUpdated { get; set; }

        [InverseProperty(nameof(ProductAccount.Product))]
        public ICollection<ProductAccount> ProductAccounts { get; set; }

        [InverseProperty(nameof(Email.Product))]
        public ICollection<Email> Emails { get; set; }

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

    [Table("Emails")]
    public class Email
    {

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EmailID { get; set; }

        public string Subject { get; set; }

        public long? ProductID { get; set; }

        [ForeignKey(nameof(ProductID))]
        [InverseProperty(nameof(Model.Product.Emails))]
        public Product Product { get; set; }

        [InverseProperty(nameof(EmailRecipient.Email))]
        public List<EmailRecipient> EmailRecipients { get; set; }

    }

    [Table("EmailRecipients")]
    public class EmailRecipient
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long EmailRecipientID { get; set; }

        public long EmailID { get; set; }

        public DateTime? TimeRead { get; set; }

        [ForeignKey(nameof(EmailID))]
        [InverseProperty(nameof(Model.Email.EmailRecipients))]
        public Email Email { get; set; }



    }
}
