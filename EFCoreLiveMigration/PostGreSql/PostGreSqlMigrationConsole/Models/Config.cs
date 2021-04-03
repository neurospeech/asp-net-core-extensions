using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMigrationConsole.Models
{
    [Table("Configs")]
    public class Config
    {

        [Key, MaxLength(20)]
        public string Key { get; set; }

        public string Value { get; set; }

    }
}