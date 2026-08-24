# 📦 Order Inventory Management System (ASP.NET Core Web API)

A robust, enterprise-grade Inventory and Order Management Web API built using **ASP.NET Core**, **Entity Framework Core**, and an **N-tier Clean Architecture** pattern. This project implements advanced features like pagination, filtering, sorting, stock validation, automatic low-stock reorder warnings, and transaction safety via Unit of Work.



##  Key Features & Architecture
* **N-Tier Architecture:** Clear separation of concerns into Controllers, DTOs, Models, Repositories, and Data layers.
* **Generic Repository & Unit of Work Pattern:** Centralizes data access logic and ensures single-transaction database commits across multiple repositories.
* **Advanced Querying:** Supports full server-side pagination, sorting, search by name/SKU, and multi-parameter filtering (by category, price range, stock, status).
* **Inventory Control & Reorder Warning System:** Built-in `ReorderLevel` logic that automatically triggers low-stock warnings during order processing when product inventory drops below safe levels.
* **Database Safety & Constraints:** Unique indices on SKUs and Emails, with strict foreign key delete restrictions (`Restricted/NoAction`) to protect relational data integrity.



##  Database Schema & Relationship Diagram (ERD)


  +------------------+         1 : N         +--------------------+
  |     Category     |-----------------------|      Product       |
  +------------------+                       +--------------------+
  | - Id (PK)        |                       | - Id (PK)          |
  | - Name           |                       | - Name, SKU, Price |
  +------------------+                       | - StockQuantity    |
                                             | - ReorderLevel (5) |
                                             +--------------------+
                                                       |
                                                       | 1 : N
                                                       v
  +------------------+         1 : N         +--------------------+
  |     Customer     |-----------------------|     OrderItem      |
  +------------------+                       +--------------------+
  | - Id (PK)        |                       | - Id (PK)          |
  | - FullName       |                       | - Quantity         |
  | - Email (Unique) |                       | - UnitPrice        |
  +------------------+                       | - LineTotal        |
           |                                 +--------------------+
           |                                           |
           | 1 : N                                     | N : 1
           v                                           v
  +-------------------------------------------------------+
  |                         Order                         |
  +-------------------------------------------------------+
  | - Id (PK), CustomerId (FK), OrderDate, Status, Total  |
  +-------------------------------------------------------+


  
📝 EF Core Migrations
The project includes meaningful database migrations tracking schema evolutions:

InitialCreate: Sets up core tables (Categories, Products, Customers, Orders, OrderItems) with primary keys, unique indices, and foreign key constraints.

AddProductReorderLevelWithDefault: Introduces the ReorderLevel property to the Product entity with a fluent API database default constraint (HasDefaultValue(5)) to power inventory warning mechanisms.



🛠️ Setup and Installation Instructions
Prerequisites
.NET 8.0 SDK or later installed.

SQL Server (LocalDB or SQL Server Management Studio).

Step-by-Step Run Guide
Clone the Repository:

Bash
git clone [https://github.com/iqra-azam47/ASP.NetCore.git](https://github.com/iqra-azam47/ASP.NetCore.git)
cd ASP.NetCore/OrderInventory.Api
Configure Connection String:
Open appsettings.json and update your SQL Server connection string under ConnectionStrings:DefaultConnection.


Apply Database Migrations:
Run the following command in the terminal to update/create the database:


Bash
dotnet ef database update
Run the Application:

Bash
dotnet run
Test Endpoints:
Open your browser and navigate to the Scalar or Swagger UI URL provided in the console output to test APIs, pagination, filters, and order placement workflows.



🧠 Architectural Notes: Generic Repository & Unit of Work
To satisfy professional software engineering standards, this solution decouples data access logic from business controllers using two core design patterns:

Generic Repository (IRepository<T>):

Encapsulates standard data access operations (e.g., GetByIdAsync, AddAsync, Update, Remove) so that queries and database logic are reusable across all entities without code duplication.

Unit of Work (IUnitOfWork):

Acts as a single coordinator for multiple repository transactions.

In operations like CreateOrder (where stocks are deducted, order items are mapped, and the order record is added), _unitOfWork.SaveChangesAsync() ensures that all changes are committed in a single atomized database transaction, preventing partial data corruption if any step fails.

