# 🚀 Modular Monolith Architecture Demo (.NET 8/9)

A clean, production-ready implementation of a **Modular Monolith** architecture pattern using ASP.NET Core Web API, Entity Framework Core, and SQL Server. This project serves as a showcase for transitioning traditional monolith applications into decoupled, modular components ready for seamless future scaling into independent microservices.

---

## 🏗️ Architectural Core Principles

Unlike traditional "spaghetti" monolithic setups where logical layers can easily blend and create tight coupling, this solution adheres to strict design boundaries:

1. **Database Schema Separation:** Modules leverage a single unified SQL Database engine but operate inside isolated physical database schemas (`Customers`, `Orders`, `Catalog`).
2. **Zero In-Database Joins:** Cross-schema database `JOIN` queries are strictly forbidden. 
3. **Decoupled Data Contexts:** Each module maintains its own dedicated Entity Framework `DbContext`, rendering horizontal context contamination impossible.
4. **Abstract Communication (Shared Kernel):** Cross-module validations utilize asynchronous interface contracts inside a shared kernel to completely avoid direct logical module dependencies.

---

## 🗺️ Project Structure & Solution Anatomy

The solution consists of 5 tightly isolated projects organized inside a clean architectural ecosystem:

```text
ModularMonolithDemo/
│
├── Demo.Host/                      # ASP.NET Core API host (Application entry-point)
├── Demo.Modules.Customers/         # Isolated class library managing Customers
├── Demo.Modules.Orders/            # Isolated class library managing Orders
├── Demo.Modules.Catalog/           # Isolated class library managing Products
└── Demo.SharedKernel/              # Shared contracts for safe inter-module communication


Dependency Reference Matrix
┌───────────────┐
                  │   Demo.Host   │ (Aggregates & Runs All Modules)
                  └───────┬───────┘
          ┌───────────────┼───────────────┐
          ▼               ▼               ▼
┌──────────────────┐ ┌────────────────┐ ┌────────────────┐
│ Modules.Customer │ │ Modules.Orders │ │ Modules.Catalog│
└─────────┬────────┘ └────────┬───────┘ └────────┬───────┘
          │                   │                  │
          └───────────────┐   │   ┌──────────────┘
                          ▼   ▼   ▼
                  ┌────────────────┐
                  │  SharedKernel  │ (Neutral Contract Definitions)
                  └────────────────┘

⚠️ The Golden Rule of References: Demo.Modules.Orders has no direct knowledge of Demo.Modules.Customers or Demo.Modules.Catalog (and vice-versa).
They remain completely sandboxed.

💾 1. Database Initialization
Execute the following setup script in SQL Server Management Studio (SSMS) to provision the database engine schemas and underlying data structures:

CREATE DATABASE ModularDemoDB;
GO
USE ModularDemoDB;
GO

-- Create independent schema boundaries for each module
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
    CustomerId INT NOT NULL, -- Logical link only! No physical Foreign Key.
    ProductId INT NOT NULL DEFAULT 0 -- Logical link only! No physical Foreign Key.
);

-- 3. Catalog Module Table
CREATE TABLE Catalog.Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL
);
GO



🛠️ 2. Module Infrastructure Setup
Project File Configuration Example (.csproj)
To expose controller routes automatically without forcing standalone executables, each module class library is configured with the .Web SDK
while enforcing a target Library output type:

<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Library</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
  </ItemGroup>
</Project>


🔄 3. Clean Cross-Boundary Inter-Module Communication
To allow one module to fetch or validate metadata owned by a completely separate domain layer, communication must go through the Demo.SharedKernel.

Interface Definition (Demo.SharedKernel)
namespace Demo.SharedKernel;

public interface ICustomerModuleApi
{
    Task<bool> CustomerExistsAsync(int customerId);
}

public interface ICatalogModuleApi
{
    Task<decimal?> GetProductPriceAsync(int productId);
}


Contract Execution Pipeline (Demo.Modules.Orders)
When an order is created, the system securely validates the parameters synchronously in-memory across module boundaries without creating compile-time dependency coupling:

[HttpPost]
public async Task<IActionResult> CreateOrder(int customerId, int productId)
{
    // 1. Validate Customer across the module boundary safely via interface contracts
    bool customerExists = await _customerApi.CustomerExistsAsync(customerId);
    if (!customerExists)
    {
        return BadRequest($"Validation Failed: Customer with ID {customerId} does not exist.");
    }

    // 2. Validate Product & extract live matching pricing metrics safely
    decimal? productPrice = await _catalogApi.GetProductPriceAsync(productId);
    if (productPrice == null)
    {
        return BadRequest($"Validation Failed: Product with ID {productId} does not exist.");
    }

    // 3. Save Order metrics safely with isolated domain boundary math
    var order = new Order 
    { 
        CustomerId = customerId, 
        ProductId = productId, 
        TotalAmount = productPrice.Value 
    };
    
    _context.Orders.Add(order);
    await _context.SaveChangesAsync();

    return Ok(order);
}


⚡ 4. Runtime Bootstrapping & Aggregation (Program.cs)
The central host project aggregates dependencies cleanly and triggers assembly scans to discover endpoints across separated binaries dynamically:

using Demo.Modules.Catalog;
using Demo.Modules.Customers;
using Demo.Modules.Orders;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register Isolated Segmented DbContext Containers
builder.Services.AddDbContext<CustomersDbContext>(opt => opt.UseSqlServer(connectionString));
builder.Services.AddDbContext<OrdersDbContext>(opt => opt.UseSqlServer(connectionString));
builder.Services.AddDbContext<CatalogDbContext>(opt => opt.UseSqlServer(connectionString));

// Register Cross-Module Communication Interfaces
builder.Services.AddScoped<Demo.SharedKernel.ICustomerModuleApi, Demo.Modules.Customers.CustomerModuleApi>();
builder.Services.AddScoped<Demo.SharedKernel.ICatalogModuleApi, Demo.Modules.Catalog.CatalogModuleApi>();

// Map Assemblies as explicit Application Parts for Controller discovery
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Demo.Modules.Customers.Controllers.CustomersController).Assembly)
    .AddApplicationPart(typeof(Demo.Modules.Orders.Controllers.OrdersController).Assembly)
    .AddApplicationPart(typeof(Demo.Modules.Catalog.Controllers.CatalogController).Assembly);

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



📈 The Microservices Migration Path
Because this system enforces complete data context isolation and maps communication through neutral abstractions, moving this solution into microservices requires zero structural business logic changes:

Database Splitting: The individual schemas can be broken off into physical independent servers instantly.

Contract Swapping: The concrete implementations of ICustomerModuleApi and ICatalogModuleApi inside Program.cs can be instantly replaced with HttpClient versions making remote HTTP requests across network nodes, requiring zero code rewrites inside the Orders module itself.







