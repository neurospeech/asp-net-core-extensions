using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMigrationConsole.Models
{
    [Table("CalenderEvents")]
    public class CalendarEvent
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CalendarEventID { get; set; }
        public DateTimeOffset Start { get; set; }

        public DateTimeOffset End { get; set; }
    }


}
