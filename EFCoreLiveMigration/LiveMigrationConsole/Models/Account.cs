using System;
using NeuroSpeech.EFCoreLiveMigration;
using System.Collections.Generic;
using System.ComponentModel;
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

        [Column(TypeName = "varchar(20)")]
        public string AccountType { get; set; }

        [MaxLength(200)]
        public string EmailAddress { get; set; }

        [InverseProperty(nameof(Product.Vendor))]
        public Product[] VendorProducts { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        public decimal? Total { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime DateCreated { get; set; }

        public ICollection<AccountEvent> Events { get; set; }
    }
}