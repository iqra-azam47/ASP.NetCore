using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderInventory.Api.DTOs;
using OrderInventory.Api.Models;
using OrderInventory.Api.Repositories;

namespace OrderInventory.Api.Controllers
{
    [Route("api/customers")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CustomersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        
        [HttpGet]
        public async Task<ActionResult<PagedResult<CustomerDto>>> GetCustomers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageSize > 100) pageSize = 100;
            if (pageSize < 1) pageSize = 10;
            if (pageNumber < 1) pageNumber = 1;

            var query = _unitOfWork.Customers.Query()
                .AsNoTracking();

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Email = c.Email,
                    Phone = c.Phone
                })
                .ToListAsync();

            var result = new PagedResult<CustomerDto>
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
        public async Task<ActionResult<CustomerDto>> GetCustomerById(int id)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(id);

            if (customer == null)
            {
                return NotFound(new { message = "Customer not found." });
            }

            var dto = new CustomerDto
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone
            };

            return Ok(dto);
        }

        [HttpGet("{id}/orders")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetCustomerOrders(int id)
        {
            var customerExists = await _unitOfWork.Customers.ExistsAsync(id);
            if (!customerExists) return NotFound(new { message = "Customer not found." });

            var orders = await _unitOfWork.Orders.Query()
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerId == id)
                .AsNoTracking()
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

            return Ok(orders);
        }

        [HttpPost]
        public async Task<ActionResult<CustomerDto>> CreateCustomer(CreateCustomerDto dto)
        {
            var customer = new Customer
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone
            };

            try
            {
                await _unitOfWork.Customers.AddAsync(customer);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { message = "A customer with this email already exists." });
            }

            var responseDto = new CustomerDto
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone
            };

            return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, responseDto);
        }
    }
}