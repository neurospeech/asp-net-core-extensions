using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMigrationConsole.Models
{
    [Table("Talents")]
    public class Talent {

        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long TalentID { get; set; }

        public ICollection<TalentEvent> Events { get; set; }

    }
}