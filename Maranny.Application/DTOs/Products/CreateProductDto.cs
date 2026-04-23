using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Maranny.Application.DTOs.Products
{
    public class CreateProductDto
    {
        [Required(ErrorMessage = "Product name is required")]
        [MinLength(3, ErrorMessage = "Product name must be at least 3 characters")]
        [MaxLength(200, ErrorMessage = "Product name cannot exceed 200 characters")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [MinLength(10, ErrorMessage = "Description must be at least 10 characters")]
        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 1000000, ErrorMessage = "Price must be between 0.01 and 1,000,000")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Condition is required")]
        [MaxLength(50)]
        public string Condition { get; set; } = string.Empty; // "New", "Like New", "Used - Good", "Used - Fair"

        [Required(ErrorMessage = "Category ID is required")]
        public int CategoryID { get; set; }

        // Optional: Sport IDs for product-sport relationships
        public List<int>? SportIDs { get; set; }

        // Product images (optional - can be added later via separate endpoint)
        [MaxLength(500)]
        public string? ImageUrl { get; set; }
    }
}