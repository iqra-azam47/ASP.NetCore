using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderInventory.Api.DTOs;
using OrderInventory.Api.Models;
using OrderInventory.Api.Repositories;

namespace OrderInventory.Api.Controllers
{
    [Route("api/categories")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoriesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _unitOfWork.Categories.Query()
                .AsNoTracking()
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory(CreateCategoryDto dto)
        {
            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                IsActive = true
            };

            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            var responseDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive
            };

            return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, responseDto);
        }

       
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, CreateCategoryDto dto)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null) return NotFound(new { message = "Category not found." });

            // Agar user ne naya name diya hai to update karein, warna purana rehne dein
            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                category.Name = dto.Name;
            }

            // Agar description di hai to update karein
            if (dto.Description != null)
            {
                category.Description = dto.Description;
            }

            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
    }
}