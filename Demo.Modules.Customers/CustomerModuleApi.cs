using Demo.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Demo.Modules.Customers
{
    public class CustomerModuleApi : ICustomerModuleApi
    {
        private readonly CustomersDbContext _context;

        public CustomerModuleApi(CustomersDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CustomerExistsAsync(int customerId)
        {
            // Query the actual database to check if the ID is valid
            return await _context.Customers.AnyAsync(c => c.Id == customerId);
        }

    }
}
