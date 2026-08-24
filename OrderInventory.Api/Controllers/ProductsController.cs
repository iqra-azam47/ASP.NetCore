using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderInventory.Api.DTOs;
using OrderInventory.Api.Models;
using OrderInventory.Api.Repositories;

namespace OrderInventory.Api.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int? minStock = null,
            [FromQuery] string sortBy = "createdAt",
            [FromQuery] string sortDirection = "desc")
        {
            if (pageSize > 100) pageSize = 100;
            if (pageSize < 1) pageSize = 10;
            if (pageNumber < 1) pageNumber = 1;

            var query = _unitOfWork.Products.Query()
                .Include(p => p.Category)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.SKU.Contains(search));
            }
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }
            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }
            if (minStock.HasValue)
            {
                query = query.Where(p => p.StockQuantity >= minStock.Value);
            }

            
            query = (sortBy.ToLower(), sortDirection.ToLower()) switch
            {
                ("id", "asc") => query.OrderBy(p => p.Id),
                ("id", "desc") => query.OrderByDescending(p => p.Id),
                ("name", "asc") => query.OrderBy(p => p.Name),
                ("name", "desc") => query.OrderByDescending(p => p.Name),
                ("price", "asc") => query.OrderBy(p => p.Price),
                ("price", "desc") => query.OrderByDescending(p => p.Price),
                ("stockquantity", "asc") => query.OrderBy(p => p.StockQuantity),
                ("stockquantity", "desc") => query.OrderByDescending(p => p.StockQuantity),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    SKU = p.SKU,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    IsActive = p.IsActive,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name
                })
                .ToListAsync();

            var result = new PagedResult<ProductDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            return Ok(result);
        }

        
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProductById(int id)
        {
            var product = await _unitOfWork.Products.Query()
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound(new { message = "Product not found." });

            var dto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                SKU = dto.SKU,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.CategoryId,
                IsActive = true
            };

            try
            {
                await _unitOfWork.Products.AddAsync(product);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { message = "A product with this SKU already exists." });
            }

            var responseDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId,
                CategoryName = (await _unitOfWork.Categories.GetByIdAsync(product.CategoryId))?.Name ?? ""
            };

            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, responseDto);
        }

        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, CreateProductDto dto)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null) return NotFound(new { message = "Product not found." });

            
            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                product.Name = dto.Name;
            }

            if (!string.IsNullOrWhiteSpace(dto.SKU))
            {
                product.SKU = dto.SKU;
            }

            if (dto.Price > 0)
            {
                product.Price = dto.Price;
            }

            if (dto.StockQuantity >= 0)
            {
                product.StockQuantity = dto.StockQuantity;
            }

            if (dto.CategoryId > 0)
            {
                product.CategoryId = dto.CategoryId;
            }

            try
            {
                _unitOfWork.Products.Update(product);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { message = "A product with this SKU already exists." });
            }

            return NoContent();
        }

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null) return NotFound(new { message = "Product not found." });

            product.IsActive = false;
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
    }
}