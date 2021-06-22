using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace LiveMigrationConsole.Models
{
    [Table("Orders")]
    public class Order
    {

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OrderID { get; set; }


        public Address Shipping { get; set; }

        public Address Billing { get; set; }
    }

    [Owned]
    public class Address
    {
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        
        public string ZipCode { get; set; }
    }
}
