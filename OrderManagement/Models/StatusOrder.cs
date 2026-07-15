using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Models;

public class StatusOrder
{
    [Key]
    public int Id { get; set; }
    public string? Name { get; set; }
}
