using Maranny.Application.DTOs.Products;
using Maranny.Core.Entities;
using Maranny.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Maranny.API.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public ProductsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Create product (Client or Coach)
        [HttpPost]
[Authorize(Roles = "Client")]
public async Task<IActionResult> CreateProduct(CreateProductDto dto)
        {
            // Get current user
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            // Get client ID (only clients can sell products based on ERD)
            var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null)
            {
                return BadRequest(new { error = "Only clients can create product listings" });
            }

            // Verify category exists
            var category = await _dbContext.Categories.FindAsync(dto.CategoryID);
            if (category == null)
            {
                return NotFound(new { error = "Category not found" });
            }

            // Create product
            var product = new Product
            {
                ClientID = client.ClientID,
                ProductName = dto.ProductName,
                Description = dto.Description,
                Price = dto.Price,
                Condition = dto.Condition,
                CategoryID = dto.CategoryID,
                ID = dto.ImageUrl
            };

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            // Add sport relationships if provided
            if (dto.SportIDs != null && dto.SportIDs.Any())
            {
                foreach (var sportId in dto.SportIDs)
                {
                    var sport = await _dbContext.Sports.FindAsync(sportId);
                    if (sport != null)
                    {
                        var sportProduct = new SportProduct
                        {
                            SportID = sportId,
                            ProductID = product.ProductID
                        };
                        _dbContext.SportProducts.Add(sportProduct);
                    }
                }
                await _dbContext.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "Product created successfully",
                productId = product.ProductID
            });
        }

        // Browse products with filters
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int? categoryId = null,
            [FromQuery] int? sportId = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? condition = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _dbContext.Products
                .Include(p => p.Client)
                .Include(p => p.Category)
                .Include(p => p.SportProducts)
                    .ThenInclude(sp => sp.Sport)
                .AsQueryable();

            // Filter by category
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            // Filter by sport
            if (sportId.HasValue)
            {
                query = query.Where(p => p.SportProducts.Any(sp => sp.SportID == sportId.Value));
            }

            // Filter by max price
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // Filter by condition
            if (!string.IsNullOrWhiteSpace(condition))
            {
                query = query.Where(p => p.Condition!.ToLower() == condition.ToLower());
            }

            // Search by name or description
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(searchLower) ||
                    p.Description!.ToLower().Contains(searchLower));
            }

            // Order by newest first
            query = query.OrderByDescending(p => p.ProductID);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.ProductID,
                    p.ProductName,
                    p.Description,
                    p.Price,
                    p.Condition,
                    ImageUrl = p.ID,
                    Category = new
                    {
                        p.Category.CategoryID,
                        p.Category.CategoryName
                    },
                    Seller = new
                    {
                        p.Client.ClientID,
                        Name = p.Client.F_name + " " + p.Client.L_name
                    },
                    Sports = p.SportProducts.Select(sp => new
                    {
                        id = sp.SportID,
                        sp.Sport.Name
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                products = products
            });
        }

        // Get product details
        [HttpGet("{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductDetails(int productId)
        {
            var product = await _dbContext.Products
                .Include(p => p.Client)
                    .ThenInclude(c => c.User)
                .Include(p => p.Category)
                .Include(p => p.SportProducts)
                    .ThenInclude(sp => sp.Sport)
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            if (product == null)
            {
                return NotFound(new { error = "Product not found" });
            }

            var result = new
            {
                product.ProductID,
                product.ProductName,
                product.Description,
                product.Price,
                product.Condition,
                ImageUrl = product.ID,
                Category = new
                {
                    product.Category.CategoryID,
                    product.Category.CategoryName
                },
                Seller = new
                {
                    product.Client.ClientID,
                    Name = product.Client.F_name + " " + product.Client.L_name,
                    Email = product.Client.User.Email,
                    Phone = product.Client.User.PhoneNumber
                },
                Sports = product.SportProducts.Select(sp => new
                {
                    id = sp.SportID,
                    sp.Sport.Name
                }).ToList()
            };

            return Ok(result);
        }

        // Update product
        [HttpPut("{productId}")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> UpdateProduct(int productId, UpdateProductDto dto)
        {
            // Get current user
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            // Get client ID
            var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null)
            {
                return NotFound(new { error = "Client profile not found" });
            }

            // Get product
            var product = await _dbContext.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound(new { error = "Product not found" });
            }

            // Verify ownership
            if (product.ClientID != client.ClientID)
            {
                return Forbid();
            }

            // Update fields (only if provided)
            if (!string.IsNullOrWhiteSpace(dto.ProductName))
                product.ProductName = dto.ProductName;

            if (!string.IsNullOrWhiteSpace(dto.Description))
                product.Description = dto.Description;

            if (dto.Price.HasValue)
                product.Price = dto.Price.Value;

            if (!string.IsNullOrWhiteSpace(dto.Condition))
                product.Condition = dto.Condition;

            if (dto.CategoryID.HasValue)
            {
                var category = await _dbContext.Categories.FindAsync(dto.CategoryID.Value);
                if (category != null)
                    product.CategoryID = dto.CategoryID.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
                product.ID = dto.ImageUrl;

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Product updated successfully" });
        }

        // Delete product
        [HttpDelete("{productId}")]
        [Authorize(Roles = "Client,Admin")]
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            // Get current user
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var product = await _dbContext.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound(new { error = "Product not found" });
            }

            // Check ownership (unless admin)
            if (User.IsInRole("Client"))
            {
                var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                if (client == null || product.ClientID != client.ClientID)
                {
                    return Forbid();
                }
            }

            // Delete related SportProducts first
            var sportProducts = await _dbContext.SportProducts
                .Where(sp => sp.ProductID == productId)
                .ToListAsync();
            _dbContext.SportProducts.RemoveRange(sportProducts);

            // Now delete the product
            _dbContext.Products.Remove(product);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Product deleted successfully" });
        }
    }
}