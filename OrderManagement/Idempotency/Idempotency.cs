using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Models;

namespace OrderManagement;

public class IdempotencyAttribute : TypeFilterAttribute
{
    public IdempotencyAttribute()
        : base(typeof(IdempotencyFilter))
    {
    }
}

public class IdempotencyFilter : IAsyncActionFilter
{
    private readonly DatabaseContext _context;

    public IdempotencyFilter(DatabaseContext context)
    {
        _context = context;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var key = context.HttpContext.Request.Headers["Idempotency-Key"]
         .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(key))
        {
            context.Result = new BadRequestObjectResult(
                "Idempotency-Key is required.");

            return;
        }

        var record = new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Key = key,
            Status = "Processing",
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.IdempotencyRecords.Add(record);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            context.Result = new ConflictObjectResult(
                "Duplicate Idempotency-Key.");

            return;
        }

        try
        {
            await next();

            record.Status = "Completed";

            await _context.SaveChangesAsync();
        }
        catch
        {
            _context.IdempotencyRecords.Remove(record);

            await _context.SaveChangesAsync();

            throw;
        }
    }
}