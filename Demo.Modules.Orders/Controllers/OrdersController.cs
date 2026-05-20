using Microsoft.AspNetCore.Mvc;

namespace Demo.Modules.Orders.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersDbContext _context;

        public OrdersController(OrdersDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(int customerId, decimal amount)
        {
            // Crucial Architect Note: 
            // Right now, Orders module CANNOT check if CustomerId actually exists 
            // because it cannot access the Customers database or code!

            var order = new Order { CustomerId = customerId, TotalAmount = amount };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(order);
        }
    }
}
