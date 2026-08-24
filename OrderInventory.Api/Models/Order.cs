namespace OrderInventory.Api.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending";
        public decimal TotalAmount { get; set; }

        // Navigation Property
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
}
