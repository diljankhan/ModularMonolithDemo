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
