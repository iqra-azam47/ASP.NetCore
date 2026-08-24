using OrderInventory.Api.Data;
using OrderInventory.Api.Models;

namespace OrderInventory.Api.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IGenericRepository<Category> Categories { get; }
        public IGenericRepository<Product> Products { get; }
        public IGenericRepository<Customer> Customers { get; }
        public IGenericRepository<Order> Orders { get; }
        public IGenericRepository<OrderItem> OrderItems { get; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Categories = new GenericRepository<Category>(_context);
            Products = new GenericRepository<Product>(_context);
            Customers = new GenericRepository<Customer>(_context);
            Orders = new GenericRepository<Order>(_context);
            OrderItems = new GenericRepository<OrderItem>(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}