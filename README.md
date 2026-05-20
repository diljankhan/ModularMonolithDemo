# ModularMonolithDemo

<b>Step 1: Create the Database, Schemas, and Tables</b>

CREATE DATABASE ModularDemoDB;
GO

USE ModularDemoDB;
GO

-- Create separate schemas (boundaries) for our modules
CREATE SCHEMA Customers;
GO

CREATE SCHEMA Orders;
GO

CREATE SCHEMA Catalog;
GO



-- 1. Customers Module Table
CREATE TABLE Customers.Customers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE
);

-- 2. Orders Module Table
CREATE TABLE Orders.Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    
    -- Logical link only! 
    -- No FOREIGN KEY constraint to Customers.Customers(Id) allowed.
    CustomerId INT NOT NULL,

	-- Logical link only! 
    -- No FOREIGN KEY constraint to Catalog.Products(Id) allowed.
	ProductId INT NOT NULL DEFAULT 0
);


-- 3. Create the Products table
CREATE TABLE Catalog.Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL
);
GO

The Golden Rule of this Database:
Code inside the Customers Module can only query Customers.Customers.
Code inside the Orders Module can only query Orders.Orders.
Strictly Forbidden: Writing a query like SELECT * FROM Orders.Orders o JOIN Customers.Customers c ON o.CustomerId = c.Id.


<b>Step 2: The .NET Solution Setup</b>

Create a new Blank Solution in Visual Studio named ModularMonolithDemo. Inside it, create five projects:

<b>Demo.Host (An ASP.NET Core Web API project)</b>
Purpose: This is the entry point. It contains the Program.cs and hosts the entire application.

<b>Demo.Modules.Customers (.NET Class Library)</b>
Purpose: Contains all the controller endpoints, business logic, and EF Core code for everything related to Customers.

<b>Demo.Modules.Orders (.NET Class Library)</b>
Purpose: Contains all the controller endpoints, business logic, and EF Core code for everything related to Orders.

<b>Demo.Modules.Catalog (.NET Class Library)</b>
Purpose: Contains all the controller endpoints, business logic, and EF Core code for everything related to Products.

<b>Demo.SharedKernel (.NET Class Library)</b>
Purpose: Contains all the controller endpoints, business logic, and EF Core code for everything related to Products.


The Golden Rule of Project References:

Demo.Host references all three Demo.Modules.Customers, Demo.Modules.Orders, Demo.Modules.Catalog and  (so it can run them).
Demo.Modules.Orders MUST NOT reference Demo.Modules.Customers and Demo.Modules.Catalog (and vice versa). They are completely blind to each other.


<b>Step 3: Entity Framework Core Setup (The Separated Contexts)</b>
<b>3.1 Install Entity Framework Core</b>
Install-Package Microsoft.EntityFrameworkCore.SqlServer -ProjectName Demo.Modules.Customers
Install-Package Microsoft.EntityFrameworkCore.SqlServer -ProjectName Demo.Modules.Orders
Install-Package Microsoft.EntityFrameworkCore.SqlServer -ProjectName Demo.Modules.Catalog
Install-Package Microsoft.EntityFrameworkCore.SqlServer -ProjectName Demo.Host

<b>3.2 Create the Customers Domain and DbContext</b>
namespace Demo.Modules.Customers;

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

===========================
using Microsoft.EntityFrameworkCore;

namespace Demo.Modules.Customers;

public class CustomersDbContext : DbContext
{
    public CustomersDbContext(DbContextOptions<CustomersDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tells EF Core that these tables live inside the 'Customers' schema
        modelBuilder.HasDefaultSchema("Customers");
        base.OnModelCreating(modelBuilder);
    }
}

<b>3.2 Create the Orders Domain and DbContext</b>

namespace Demo.Modules.Orders;

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public int CustomerId { get; set; } 
}

using Microsoft.EntityFrameworkCore;

namespace Demo.Modules.Orders;

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tells EF Core that these tables live inside the 'Orders' schema
        modelBuilder.HasDefaultSchema("Orders");
        base.OnModelCreating(modelBuilder);
    }
}


<b>Step 4: Wire Everything up in Demo.Host</b>
Add the Connection String
Open appsettings.json inside your Demo.Host project. Add your connection string at the top. (Replace YOUR_SERVER_NAME with your actual SQL Server name from SSMS).
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=ModularDemoDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}

Register the DbContexts in Program.cs
using Demo.Modules.Customers;
using Demo.Modules.Orders;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Get the connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Register Customers Module DbContext
builder.Services.AddDbContext<CustomersDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Register Orders Module DbContext
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(connectionString));


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();


Step 5: Create the Controllers

1. Customers Controller
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.Modules.Customers.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly CustomersDbContext _context;

    public CustomersController(CustomersDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer(string name, string email)
    {
        var customer = new Customer { FullName = name, Email = email };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        
        return Ok(customer);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();
        
        return Ok(customer);
    }
}

2. Orders Controller
using Microsoft.AspNetCore.Mvc;

namespace Demo.Modules.Orders.Controllers;

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


The Fix: Update the Project Files (.csproj)
In Visual Studio's Solution Explorer, double-click on your Demo.Modules.Customers project name. (This opens its .csproj configuration file as text).

Look at the very first line. Change the SDK from Microsoft.NET.Sdk to Microsoft.NET.Sdk.Web.
Inside the <PropertyGroup> tags, add <OutputType>Library</OutputType>.

<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework> <OutputType>Library</OutputType> <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="..." />
  </ItemGroup>

</Project>


The Fix: Register Module Assemblies in Program.cs

Open Program.cs in the Demo.Host project.
Find the line that says: builder.Services.AddControllers();
Update that line to explicitly look inside your module classes, like this:

// Change this line to discover controllers in external modules
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Demo.Modules.Customers.Controllers.CustomersController).Assembly)
    .AddApplicationPart(typeof(Demo.Modules.Orders.Controllers.OrdersController).Assembly);


    Why this works:
Using .AddApplicationPart() tells ASP.NET Core's MVC engine: "Hey, treat these class libraries as part of the core web application when you scan for endpoints."



Set Up the Project References in the shared project
To allow communication via this shared layer, update your references:
Demo.Modules.Customers should reference Demo.SharedKernel.
Demo.Modules.Orders should reference Demo.SharedKernel.

Step 3: Create the Contract
Inside your new Demo.SharedKernel project, delete Class1.cs and create a new interface file named ICustomerModuleApi.cs:
namespace Demo.SharedKernel;

public interface ICustomerModuleApi
{
    Task<bool> CustomerExistsAsync(int customerId);
}

Step 4: Implement the Contract in the Customers Module
using Demo.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Demo.Modules.Customers;

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

Step 5: Register the Implementation in the Host

// Register the cross-module communication service
builder.Services.AddScoped<Demo.SharedKernel.ICustomerModuleApi, Demo.Modules.Customers.CustomerModuleApi>();

Step 6: Consume the Contract in OrdersController

using Demo.SharedKernel; // Add this using statement
using Microsoft.AspNetCore.Mvc;

namespace Demo.Modules.Orders.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrdersDbContext _context;
    private readonly ICustomerModuleApi _customerApi; // Add private field

    // Inject the shared contract interface here
    public OrdersController(OrdersDbContext context, ICustomerModuleApi customerApi)
    {
        _context = context;
        _customerApi = customerApi;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(int customerId, decimal amount)
    {
        // 1. Call the contract to check if the customer exists across the boundary
        bool customerExists = await _customerApi.CustomerExistsAsync(customerId);
        
        if (!customerExists)
        {
            return BadRequest($"Validation Failed: Customer with ID {customerId} does not exist.");
        }

        // 2. If valid, safely save the order
        var order = new Order { CustomerId = customerId, TotalAmount = amount };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Ok(order);
    }
}



