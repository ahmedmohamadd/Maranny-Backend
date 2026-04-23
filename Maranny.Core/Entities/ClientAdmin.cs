using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class ClientAdmin
    {
        [Key, Column(Order = 0)]
        public int ClientID { get; set; }

        [Key, Column(Order = 1)]
        public int AdminID { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ClientID))]
        public virtual Client Client { get; set; } = null!;

        [ForeignKey(nameof(AdminID))]
        public virtual Admin Admin { get; set; } = null!;
    }
}
