using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Models;

namespace OrderManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly ILogger<OrderController> _logger;

    public OrderController(DatabaseContext context, ILogger<OrderController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetOrders(int pageSize = 10, int pageNumber = 1)
    {
        try
        {
            var orders = _context.Orders
                        .Include(o => o.ShippingStatus)
                        .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            _logger.LogInformation("Orders retrieved successfully.");
            return Ok(orders);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving orders.");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public IActionResult GetOrderById(int id)
    {
        try
        {
            var order = _context.Orders
                .Select(o => new
                {
                    o.Id,
                    o.CustomerId,
                    o.ShippingAddress,
                    o.ShippingStatusId,
                    o.TotalPrice,
                    o.OrderDate,
                    ShippingStatus = o.ShippingStatus.Name,
                    o.RowVersion,
                    ProductOrders = o.ProductOrders.Select(po => new
                    {
                        po.ProductId,
                        ProductName = po.Product.Name,
                        po.Quantity
                    }).ToList()
                })
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Order not found.");
                return NotFound();
            }
            _logger.LogInformation("Order retrieved successfully.");
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving order by ID.");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost]
    [Idempotency]
    public async Task<IActionResult> CreateOrder(Order order)
    {
        try
        {
            //test API idempotency by adding a delay of 5 seconds
            await Task.Delay(5000);
            decimal totalPrice = 0;
            order.TotalPrice = totalPrice;
            var addedOrder = _context.Orders.Add(order);
            if (addedOrder != null)
            {
                foreach (var productOrder in order.ProductOrders)
                {

                    //counting total price
                    var product = _context.Products.FirstOrDefault(p => p.Id == productOrder.ProductId);
                    if (product == null)
                    {
                        _logger.LogWarning($"Product with ID {productOrder.ProductId} not found.");
                        return BadRequest($"Product with ID {productOrder.ProductId} not found.");
                    }
                    if (product.Quantity < productOrder.Quantity)
                    {
                        _logger.LogWarning($"Not enough stock for product {product.Name}. Available: {product.Quantity}, Requested: {productOrder.Quantity}");
                        return BadRequest($"Not enough stock for product {product.Name}. Available: {product.Quantity}, Requested: {productOrder.Quantity}");
                    }
                    totalPrice += product.Price * productOrder.Quantity;


                    var item = _context.Products.FirstOrDefault(p => p.Id == productOrder.ProductId);
                    if (item != null)
                    {
                        item.Quantity -= productOrder.Quantity;
                        _context.Products.Update(item);
                    }
                    productOrder.OrderId = addedOrder.Entity.Id;
                    _context.ProductOrders.Add(productOrder);

                }
            }
            _context.SaveChanges();
            _logger.LogInformation("Order created successfully.");
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, new { id = order.Id, message = "Order created successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the order.");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }

    }

    [HttpPut]
    [Idempotency]
    public async Task<IActionResult> UpdateStatusOrder(int id, int statusId, int rowVersion)
    {
        try
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                _logger.LogWarning("Order not found.");
                return NotFound();
            }
            if (order.RowVersion != rowVersion)
            {
                _logger.LogWarning("Already updated by another user. Please refresh and try again.");
                return Conflict("Already updated by another user. Please refresh and try again.");
            }


            if (order.ShippingStatusId == 1 && (statusId == 2 || statusId == 3))
            {
                if (statusId == 3)
                {
                    RestoreStockForOrder(id);
                }
                _logger.LogInformation("Order status updated successfully.");
                order.ShippingStatusId = statusId;
            }
            else if (order.ShippingStatusId == 2 && (statusId == 3 || statusId == 4))
            {
                if (statusId == 3)
                {
                    RestoreStockForOrder(id);
                }
                _logger.LogInformation("Order status updated successfully.");
                order.ShippingStatusId = statusId;
            }
            else if (order.ShippingStatusId == 4 && statusId == 5)
            {
                _logger.LogInformation("Order status updated successfully.");
                order.ShippingStatusId = statusId;
            }
            else if (order.ShippingStatusId == 3 || order.ShippingStatusId == 5)
            {
                _logger.LogWarning("No further status updates allowed for this order.");
                return BadRequest("No further status updates allowed for this order.");
            }
            else
            {
                _logger.LogWarning("Invalid status transition.");
                return BadRequest("Invalid status transition.");
            }
            order.RowVersion++;
            await _context.SaveChangesAsync();
            return Ok("Order status updated successfully.");
        }

        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict detected during update.");
            return Conflict("Concurrency conflict detected during update.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the order status.");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    private void RestoreStockForOrder(int orderId)
    {
        var productOrders = _context.ProductOrders.Where(po => po.OrderId == orderId).ToList();
        foreach (var productOrder in productOrders)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productOrder.ProductId);
            if (product != null)
            {
                product.Quantity += productOrder.Quantity;
                _context.Products.Update(product);
            }
        }
    }
}
