
using Demo.Modules.Customers;
using Demo.Modules.Orders;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. Get the connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Register Customers Module DbContext
builder.Services.AddDbContext<CustomersDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Register Orders Module DbContext
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register the cross-module communication service
builder.Services.AddScoped<Demo.SharedKernel.ICustomerModuleApi, Demo.Modules.Customers.CustomerModuleApi>();

//builder.Services.AddControllers();
// Change this line to discover controllers in external modules

//Using .AddApplicationPart() tells ASP.NET Core's MVC engine: "Hey, treat these class
//libraries as part of the core web application when you scan for endpoints."
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Demo.Modules.Customers.Controllers.CustomersController).Assembly)
    .AddApplicationPart(typeof(Demo.Modules.Orders.Controllers.OrdersController).Assembly);


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
