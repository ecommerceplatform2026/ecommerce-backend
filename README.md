# E-commerce Backend API

Backend API for a women’s fashion e-commerce platform built with **ASP.NET Core Web API**, **.NET 8**, **Entity Framework Core**, and **PostgreSQL**.

## Tech Stack

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- JWT Authentication
- Clean Architecture
- Swagger / OpenAPI

## Features

- User registration and login
- JWT authentication and role-based authorization
- User profile and address management
- Category and product management
- Product variants, images, and inventory
- Shopping cart and checkout
- Order management and payment tracking
- Product reviews
- Admin dashboard APIs

## Project Structure

```text
Ecommerce
├── Domain
├── Application
├── Infrastructure
└── Presentation
```

## Getting Started

### 1. Clone repository

```bash
git clone https://github.com/ecommerceplatform2026/ecommerce-backend.git
cd ecommerce-backend
```

### 2. Restore packages

```bash
dotnet restore
```

### 3. Configure database

Update `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ecommerce_db;Username=postgres;Password=CHANGE_ME"
  }
}
```

### 4. Apply migrations

```bash
dotnet ef database update --project Ecommerce/Infrastructure --startup-project Ecommerce/Presentation
```

### 5. Run project

```bash
dotnet run --project Ecommerce/Presentation
```

## Swagger

After running the API, open:

```text
https://localhost:<port>/swagger
```

## Branch Workflow

```text
feature/* -> dev -> main
```

## Status

This project is under active development.
