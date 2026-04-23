using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Maranny.Application.DTOs.Products
{
    public class UpdateProductDto
    {
        [MinLength(3)]
        [MaxLength(200)]
        public string? ProductName { get; set; }

        [MinLength(10)]
        [MaxLength(2000)]
        public string? Description { get; set; }

        [Range(0.01, 1000000)]
        public decimal? Price { get; set; }

        [MaxLength(50)]
        public string? Condition { get; set; }

        public int? CategoryID { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }
    }
}