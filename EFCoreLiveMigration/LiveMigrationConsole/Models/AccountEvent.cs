using System.ComponentModel.DataAnnotations.Schema;

namespace LiveMigrationConsole.Models
{
    public class AccountEvent: CalendarEvent
    {
        public bool IsBusy { get; set; }

        public long AccountID { get; set; }

        [ForeignKey(nameof(AccountID))]
        public Account Account { get; set; }
    }



}
