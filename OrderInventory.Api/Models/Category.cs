namespace OrderInventory.Api.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property: One Category has many Products
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
