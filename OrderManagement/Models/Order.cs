using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Models;

public class Order
{
    [Key]
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string? ShippingAddress { get; set; }
    public int ShippingStatusId { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime OrderDate { get; set; }
    public StatusOrder? ShippingStatus { get; set; }
    public List<ProductOrder>? ProductOrders { get; set; }

    [ConcurrencyCheck]
    public int RowVersion { get; set; } = 1;
}
