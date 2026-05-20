using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.SharedKernel
{
    public interface ICustomerModuleApi
    {
        Task<bool> CustomerExistsAsync(int customerId);
    }
}
