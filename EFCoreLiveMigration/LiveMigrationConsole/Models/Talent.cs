using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMigrationConsole.Models
{
    [Table("Talents")]
    public class Talent: Account {


        public Name Legal { get; set; }

    }

    [Owned]
    public class Name
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}