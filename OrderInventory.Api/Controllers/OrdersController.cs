using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderInventory.Api.DTOs;
using OrderInventory.Api.Models;
using OrderInventory.Api.Repositories;

namespace OrderInventory.Api.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrdersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

      
        [HttpGet]
        public async Task<ActionResult<PagedResult<OrderDto>>> GetOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? customerId = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string sortBy = "orderDate",
            [FromQuery] string sortDirection = "desc")
        {
            if (pageSize > 100) pageSize = 100;
            if (pageSize < 1) pageSize = 10;
            if (pageNumber < 1) pageNumber = 1;

            var query = _unitOfWork.Orders.Query()
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsNoTracking();

            // Filters
            if (customerId.HasValue)
                query = query.Where(o => o.CustomerId == customerId.Value);
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status.ToLower() == status.ToLower());
            if (dateFrom.HasValue)
                query = query.Where(o => o.OrderDate >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(o => o.OrderDate <= dateTo.Value);

            // Sorting Whitelist
            query = (sortBy.ToLower(), sortDirection.ToLower()) switch
            {
                ("totalamount", "asc") => query.OrderBy(o => o.TotalAmount),
                ("totalamount", "desc") => query.OrderByDescending(o => o.TotalAmount),
                ("status", "asc") => query.OrderBy(o => o.Status),
                ("status", "desc") => query.OrderByDescending(o => o.Status),
                _ => query.OrderByDescending(o => o.OrderDate)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer.FullName,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        LineTotal = oi.LineTotal
                    }).ToList()
                })
                .ToListAsync();

            var result = new PagedResult<OrderDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            return Ok(result);
        }

        // GET: api/orders/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrderById(int id)
        {
            var order = await _unitOfWork.Orders.Query()
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound(new { message = "Order not found." });

            var dto = new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer.FullName,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    LineTotal = oi.LineTotal
                }).ToList()
            };

            return Ok(dto);
        }

       
        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto dto)
        {
            // 1. Validate Customer exists
            var customerExists = await _unitOfWork.Customers.ExistsAsync(dto.CustomerId);
            if (!customerExists)
                return BadRequest(new { message = "Customer does not exist." });

            if (dto.Items == null || !dto.Items.Any())
                return BadRequest(new { message = "Order must contain at least one item." });

            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;

            foreach (var itemDto in dto.Items)
            {
                // 2. Load product and check if active & exists
                var product = await _unitOfWork.Products.GetByIdAsync(itemDto.ProductId);
                if (product == null || !product.IsActive)
                    return BadRequest(new { message = $"Product with ID {itemDto.ProductId} is not found or inactive." });

                // 3. Validate stock availability
                if (product.StockQuantity < itemDto.Quantity)
                    return BadRequest(new { message = $"Insufficient stock for product '{product.Name}'. Available: {product.StockQuantity}, Requested: {itemDto.Quantity}" });

                // 4. Decrease product stock quantity
                product.StockQuantity -= itemDto.Quantity;
                _unitOfWork.Products.Update(product);

                // 5. Calculate LineTotal and copy current Product.Price into OrderItem.UnitPrice (Requirement)
                decimal lineTotal = product.Price * itemDto.Quantity;
                totalAmount += lineTotal;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price, 
                    LineTotal = lineTotal
                });
            }

            // 6. Create Order record
            var order = new Order
            {
                CustomerId = dto.CustomerId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                TotalAmount = totalAmount,
                OrderItems = orderItems
            };

            await _unitOfWork.Orders.AddAsync(order);

            // 7. Persist all changes using a single Unit of Work commit (Requirement)
            await _unitOfWork.SaveChangesAsync();

            // Return response DTO
            var responseDto = new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                CustomerName = (await _unitOfWork.Customers.GetByIdAsync(order.CustomerId))?.FullName ?? "",
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                OrderItems = orderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = _unitOfWork.Products.GetByIdAsync(oi.ProductId).Result?.Name ?? "",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    LineTotal = oi.LineTotal
                }).ToList()
            };

            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, responseDto);
        }

        
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, UpdateOrderStatusDto dto)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(id);
            if (order == null) return NotFound(new { message = "Order not found." });

            order.Status = dto.Status;
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
    }
}