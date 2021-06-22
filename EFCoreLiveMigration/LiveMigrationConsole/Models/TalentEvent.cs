using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMigrationConsole.Models
{
    public class TalentEvent: CalendarEvent
    {
        public long TalentID { get; set; }

        [ForeignKey(nameof(TalentID))]
        public Talent Talent { get; set; }
    }
}