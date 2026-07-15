namespace OrderManagement.Models;

public class IdempotencyRecord
{
    public Guid Id { get; set; }

    public string Key { get; set; } = default!;

    public string Status { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
}
