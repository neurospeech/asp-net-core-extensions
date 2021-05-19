using NeuroSpeech.EFCoreLiveMigration;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMigrationConsole.Models
{
    [Table("Accounts")]
    public class Account
    {

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AccountID { get; set; }

        [MaxLength(200)]
        [Index]
        [OldName("AccountName")]
        public string DisplayName { get; set; }

        [MaxLength(10)]
        public string AccountType { get; set; }

        [InverseProperty(nameof(Product.Vendor))]
        public Product[] VendorProducts { get; set; }

        public decimal Balance { get; set; }

        public decimal? Total { get; set; }

        public ICollection<AccountEvent> Events { get; set; }
    }
}