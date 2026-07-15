using Microsoft.AspNetCore.Mvc;
using OrderManagement.Models;

namespace OrderManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly ILogger<ProductController> _logger;

    public ProductController(DatabaseContext context, ILogger<ProductController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public IActionResult GetProductById(int id)
    {
        var product = _context.Products.Find(id);
        if (product == null)
        {
            _logger.LogWarning("Product not found.");
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    [Idempotency]
    public IActionResult CreateProduct(Product product)
    {
        _context.Products.Add(product);
        _context.SaveChanges();
        _logger.LogInformation("Product created successfully.");
        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
    }
}

