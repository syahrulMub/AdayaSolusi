using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Models;

public class Product
{
    [Key]
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public List<ProductOrder>? ProductOrders { get; set; }
}
