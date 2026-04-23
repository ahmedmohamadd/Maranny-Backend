using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maranny.Core.Entities
{
    public class SportProduct
    {
        [Key, Column(Order = 0)]
        public int SportID { get; set; }

        [Key, Column(Order = 1)]
        public int ProductID { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(SportID))]
        public virtual Sport Sport { get; set; } = null!;

        [ForeignKey(nameof(ProductID))]
        public virtual Product Product { get; set; } = null!;
    }
}