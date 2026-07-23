using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RenovoWorkshop.Api.Auth;
using RenovoWorkshop.Api.Hubs;
using RenovoWorkshop.Api.Mapping;
using RenovoWorkshop.Application.Interfaces;
using RenovoWorkshop.Infrastructure.Persistence;
using RenovoWorkshop.Infrastructure.Repositories;
using RenovoWorkshop.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
builder.Services.AddSignalR();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Renovo Workshop API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<RenovoWorkshopDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        options.UseNpgsql(connectionString);
    else
        options.UseSqlite(connectionString);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:8080", "http://127.0.0.1:3000", "http://192.168.*.*", "http://10.0.*.*")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    
    options.AddPolicy("AllowProduction", policy =>
    {
        var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? Array.Empty<string>();
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "RenovoWorkshop.Api",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "RenovoWorkshop.Client",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "RenovoWorkshop-Development-Key-123456"))
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageCustomers", policy => policy.Requirements.Add(new PermissionRequirement("customers.write")));
    options.AddPolicy("CanManageVehicles", policy => policy.Requirements.Add(new PermissionRequirement("vehicles.write")));
    options.AddPolicy("CanManageOrders", policy => policy.Requirements.Add(new PermissionRequirement("orders.write")));
    options.AddPolicy("CanManageInventory", policy => policy.Requirements.Add(new PermissionRequirement("inventory.write")));
    options.AddPolicy("CanManageUsers", policy => policy.Requirements.Add(new PermissionRequirement("users.manage")));
});

var app = builder.Build();

// Apply database migrations and seed test data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RenovoWorkshopDbContext>();
    context.Database.Migrate();

    // Seed test data if database is empty
    if (!context.Users.Any())
    {
        SeedTestData(context);
    }

    // Independent from the users-based seed above: runs whenever these
    // tables are empty, even on a database that was already seeded before
    // this feature existed.
    if (!context.InventoryItems.Any())
    {
        SeedInventory(context);
    }
    if (!context.WorkshopSettings.Any())
    {
        SeedWorkshopSettings(context);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(app.Environment.IsDevelopment() ? "AllowLocalhost" : "AllowProduction");

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        context.RequestServices.GetRequiredService<ILogger<Program>>()
            .LogError(exception, "Unhandled exception on {Path}", context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { message = "Erro interno no servidor." });
    });
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<WorkshopHub>("/hubs/workshop");

app.Run();

static void SeedTestData(RenovoWorkshopDbContext context)
{
    // Seed 10 Users
    var users = new List<RenovoWorkshop.Domain.Entities.ApplicationUser>
    {
            new RenovoWorkshop.Domain.Entities.ApplicationUser
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserName = "admin",
                Email = "admin@renovo.com.br",
                PasswordHash = "$2a$11$GAh79wVuhlFp1ZKM6HmGEebXjYe.tk.HwhkOu1ahmtF1yCinSB37S",
                FullName = "Administrador Master",
                Role = "Administrador",
                Permissions = "dashboard.view,customers.read,customers.write,vehicles.read,vehicles.write,orders.read,orders.write,inventory.read,inventory.write,users.manage",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
        new RenovoWorkshop.Domain.Entities.ApplicationUser
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            UserName = "joao.silva",
            Email = "joao.gerente@renovo.com.br",
            PasswordHash = "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2",
            FullName = "João Silva",
            Role = "Gerente",
            Permissions = "dashboard.view,customers.read,customers.write,vehicles.read,vehicles.write,orders.read,orders.write,inventory.read,inventory.write",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        },
        new RenovoWorkshop.Domain.Entities.ApplicationUser
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            UserName = "maria.santos",
            Email = "maria.recepcao@renovo.com.br",
            PasswordHash = "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2",
            FullName = "Maria Santos",
            Role = "Recepção",
            Permissions = "dashboard.view,customers.read,customers.write,vehicles.read,vehicles.write,orders.read,orders.write",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
        },
        new RenovoWorkshop.Domain.Entities.ApplicationUser
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            UserName = "pedro.oliveira",
            Email = "pedro.mecanico@renovo.com.br",
            PasswordHash = "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2",
            FullName = "Pedro Oliveira",
            Role = "Mecânico",
            Permissions = "dashboard.view,vehicles.read,orders.read,orders.write",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc)
        },
        new RenovoWorkshop.Domain.Entities.ApplicationUser
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            UserName = "carlos.ferreira",
            Email = "carlos.mecanico@renovo.com.br",
            PasswordHash = "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2",
            FullName = "Carlos Ferreira",
            Role = "Mecânico",
            Permissions = "dashboard.view,vehicles.read,orders.read,orders.write",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)
        },
        new RenovoWorkshop.Domain.Entities.ApplicationUser
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            UserName = "ana.costa",
            Email = "ana.almoxarifado@renovo.com.br",
            PasswordHash = "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2",
            FullName = "Ana Costa",
            Role = "Almoxarifado",
            Permissions = "inventory.read,inventory.write,orders.read",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc)
        },
        new RenovoWorkshop.Domain.Entities.ApplicationUser
        {
            Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
            UserName = "lucas.pereira",
            Email = "lucas.recepcao@renovo.com.br",
            PasswordHash = "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2",
            FullName = "Lucas Pereira",
            Role = "Recepção",
            Permissions = "dashboard.view,customers.read,customers.write,vehicles.read,vehicles.write,orders.read,orders.write",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 7, 0, 0, 0, DateTimeKind.Utc)
        },
        new RenovoWorkshop.Domain.Entities.ApplicationUser
        {
            Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
            UserName = "juliana.lima",
            Email = "juliana.mecanico@renovo.com.br",
            PasswordHash = "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2",
            FullName = "Juliana Lima",
            Role = "Mecânico",
            Permissions = "dashboard.view,vehicles.read,orders.read,orders.write",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc)
        },
        new RenovoWorkshop.Domain.Entities.ApplicationUser
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            UserName = "fernanda.souza",
            Email = "fernanda.gerente@renovo.com.br",
            PasswordHash = "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2",
            FullName = "Fernanda Souza",
            Role = "Gerente",
            Permissions = "dashboard.view,customers.read,customers.write,vehicles.read,vehicles.write,orders.read,orders.write,inventory.read,inventory.write",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 9, 0, 0, 0, DateTimeKind.Utc)
        },
        new RenovoWorkshop.Domain.Entities.ApplicationUser
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            UserName = "roberto.almeida",
            Email = "roberto.almoxarifado@renovo.com.br",
            PasswordHash = "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2",
            FullName = "Roberto Almeida",
            Role = "Almoxarifado",
            Permissions = "inventory.read,inventory.write,orders.read",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)
        }
    };

    context.Users.AddRange(users);

    // Seed 10 Customers
    var customers = new List<RenovoWorkshop.Domain.Entities.Customer>
    {
        new RenovoWorkshop.Domain.Entities.Customer { Id = Guid.Parse("10000001-0000-0000-0000-000000000001"), Name = "Carlos Eduardo Mendes", Document = "12345678901", Phone = "(11) 98765-4321", WhatsApp = "(11) 98765-4321", Email = "carlos.mendes@email.com", Address = "Rua das Flores, 123, São Paulo, SP", Notes = "Cliente preferencial", CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc) },
        new RenovoWorkshop.Domain.Entities.Customer { Id = Guid.Parse("10000002-0000-0000-0000-000000000002"), Name = "Ana Paula Rodrigues", Document = "23456789012", Phone = "(11) 97654-3210", WhatsApp = "(11) 97654-3210", Email = "ana.rodrigues@email.com", Address = "Av. Paulista, 456, São Paulo, SP", Notes = "Cliente desde 2024", CreatedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc) },
        new RenovoWorkshop.Domain.Entities.Customer { Id = Guid.Parse("10000003-0000-0000-0000-000000000003"), Name = "José Carlos Almeida", Document = "34567890123", Phone = "(11) 96543-2109", WhatsApp = "(11) 96543-2109", Email = "jose.almeida@email.com", Address = "Rua Augusta, 789, São Paulo, SP", Notes = "Empresário", CreatedAt = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc) },
        new RenovoWorkshop.Domain.Entities.Customer { Id = Guid.Parse("10000004-0000-0000-0000-000000000004"), Name = "Mariana Costa Silva", Document = "45678901234", Phone = "(11) 95432-1098", WhatsApp = "(11) 95432-1098", Email = "mariana.silva@email.com", Address = "Alameda Santos, 321, São Paulo, SP", Notes = "Cliente VIP", CreatedAt = new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc) },
        new RenovoWorkshop.Domain.Entities.Customer { Id = Guid.Parse("10000005-0000-0000-0000-000000000005"), Name = "Roberto Fernandes Lima", Document = "56789012345", Phone = "(11) 94321-0987", WhatsApp = "(11) 94321-0987", Email = "roberto.lima@email.com", Address = "Rua Oscar Freire, 654, São Paulo, SP", Notes = "Prefere atendimento pela manhã", CreatedAt = new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc) },
        new RenovoWorkshop.Domain.Entities.Customer { Id = Guid.Parse("10000006-0000-0000-0000-000000000006"), Name = "Patricia Oliveira Souza", Document = "67890123456", Phone = "(11) 93210-9876", WhatsApp = "(11) 93210-9876", Email = "patricia.souza@email.com", Address = "Av. Brigadeiro Faria Lima, 987, São Paulo, SP", Notes = "Cliente nova", CreatedAt = new DateTime(2026, 2, 6, 0, 0, 0, DateTimeKind.Utc) },
        new RenovoWorkshop.Domain.Entities.Customer { Id = Guid.Parse("10000007-0000-0000-0000-000000000007"), Name = "Fernando Costa Pereira", Document = "78901234567", Phone = "(11) 92109-8765", WhatsApp = "(11) 92109-8765", Email = "fernando.pereira@email.com", Address = "Rua Haddock Lobo, 147, São Paulo, SP", Notes = "Frota de veículos", CreatedAt = new DateTime(2026, 2, 7, 0, 0, 0, DateTimeKind.Utc) },
        new RenovoWorkshop.Domain.Entities.Customer { Id = Guid.Parse("10000008-0000-0000-0000-000000000008"), Name = "Camila Rodrigues Santos", Document = "89012345678", Phone = "(11) 91098-7654", WhatsApp = "(11) 91098-7654", Email = "camila.santos@email.com", Address = "Av. Rebouças, 258, São Paulo, SP", Notes = "Cliente fiel", CreatedAt = new DateTime(2026, 2, 8, 0, 0, 0, DateTimeKind.Utc) },
        new RenovoWorkshop.Domain.Entities.Customer { Id = Guid.Parse("10000009-0000-0000-0000-000000000009"), Name = "Ricardo Almeida Costa", Document = "90123456789", Phone = "(11) 90987-6543", WhatsApp = "(11) 90987-6543", Email = "ricardo.costa@email.com", Address = "Rua Pamplona, 369, São Paulo, SP", Notes = "Empresa", CreatedAt = new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc) },
        new RenovoWorkshop.Domain.Entities.Customer { Id = Guid.Parse("10000010-0000-0000-0000-000000000010"), Name = "Juliana Ferreira Lima", Document = "01234567890", Phone = "(11) 89876-5432", WhatsApp = "(11) 89876-5432", Email = "juliana.lima@email.com", Address = "Av. Moema, 741, São Paulo, SP", Notes = "Cliente desde 2023", CreatedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc) }
    };

    context.Customers.AddRange(customers);

    // Seed 10 Vehicles
    var vehicles = new List<RenovoWorkshop.Domain.Entities.Vehicle>
    {
        new RenovoWorkshop.Domain.Entities.Vehicle { Id = Guid.Parse("20000001-0000-0000-0000-000000000001"), Plate = "ABC1234", Brand = "Toyota", Model = "Corolla", Year = 2022, Color = "Prata", Engine = "2.0 Flex", Fuel = "Flex", Mileage = 45000, Chassis = "9BR12345678901234", Renavam = "01234567890", CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), CustomerId = Guid.Parse("10000001-0000-0000-0000-000000000001") },
        new RenovoWorkshop.Domain.Entities.Vehicle { Id = Guid.Parse("20000002-0000-0000-0000-000000000002"), Plate = "DEF5678", Brand = "Honda", Model = "Civic", Year = 2023, Color = "Preto", Engine = "1.5 Turbo", Fuel = "Flex", Mileage = 28000, Chassis = "9BR23456789012345", Renavam = "12345678901", CreatedAt = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc), CustomerId = Guid.Parse("10000002-0000-0000-0000-000000000002") },
        new RenovoWorkshop.Domain.Entities.Vehicle { Id = Guid.Parse("20000003-0000-0000-0000-000000000003"), Plate = "GHI9012", Brand = "Volkswagen", Model = "Golf", Year = 2021, Color = "Branco", Engine = "1.4 TSI", Fuel = "Flex", Mileage = 35000, Chassis = "9BW12345678901234", Renavam = "23456789012", CreatedAt = new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc), CustomerId = Guid.Parse("10000003-0000-0000-0000-000000000003") },
        new RenovoWorkshop.Domain.Entities.Vehicle { Id = Guid.Parse("20000004-0000-0000-0000-000000000004"), Plate = "JKL3456", Brand = "Hyundai", Model = "HB20", Year = 2023, Color = "Vermelho", Engine = "1.0 Turbo", Fuel = "Flex", Mileage = 15000, Chassis = "9BH12345678901234", Renavam = "34567890123", CreatedAt = new DateTime(2026, 3, 4, 0, 0, 0, DateTimeKind.Utc), CustomerId = Guid.Parse("10000004-0000-0000-0000-000000000004") },
        new RenovoWorkshop.Domain.Entities.Vehicle { Id = Guid.Parse("20000005-0000-0000-0000-000000000005"), Plate = "MNO7890", Brand = "Fiat", Model = "Argo", Year = 2022, Color = "Azul", Engine = "1.3 Firefly", Fuel = "Flex", Mileage = 22000, Chassis = "9BD12345678901234", Renavam = "45678901234", CreatedAt = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc), CustomerId = Guid.Parse("10000005-0000-0000-0000-000000000005") },
        new RenovoWorkshop.Domain.Entities.Vehicle { Id = Guid.Parse("20000006-0000-0000-0000-000000000006"), Plate = "PQR1234", Brand = "Chevrolet", Model = "Onix", Year = 2023, Color = "Cinza", Engine = "1.0 Turbo", Fuel = "Flex", Mileage = 12000, Chassis = "9BG12345678901234", Renavam = "56789012345", CreatedAt = new DateTime(2026, 3, 6, 0, 0, 0, DateTimeKind.Utc), CustomerId = Guid.Parse("10000006-0000-0000-0000-000000000006") },
        new RenovoWorkshop.Domain.Entities.Vehicle { Id = Guid.Parse("20000007-0000-0000-0000-000000000007"), Plate = "STU5678", Brand = "Ford", Model = "Ka", Year = 2021, Color = "Prata", Engine = "1.0 Ti-VCT", Fuel = "Flex", Mileage = 38000, Chassis = "9BF12345678901234", Renavam = "67890123456", CreatedAt = new DateTime(2026, 3, 7, 0, 0, 0, DateTimeKind.Utc), CustomerId = Guid.Parse("10000007-0000-0000-0000-000000000007") },
        new RenovoWorkshop.Domain.Entities.Vehicle { Id = Guid.Parse("20000008-0000-0000-0000-000000000008"), Plate = "VWX9012", Brand = "Renault", Model = "Kwid", Year = 2022, Color = "Branco", Engine = "1.0 SCe", Fuel = "Flex", Mileage = 25000, Chassis = "9BR34567890123456", Renavam = "78901234567", CreatedAt = new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc), CustomerId = Guid.Parse("10000008-0000-0000-0000-000000000008") },
        new RenovoWorkshop.Domain.Entities.Vehicle { Id = Guid.Parse("20000009-0000-0000-0000-000000000009"), Plate = "YZA3456", Brand = "Nissan", Model = "Kicks", Year = 2023, Color = "Preto", Engine = "1.6", Fuel = "Flex", Mileage = 18000, Chassis = "9BR45678901234567", Renavam = "89012345678", CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc), CustomerId = Guid.Parse("10000009-0000-0000-0000-000000000009") },
        new RenovoWorkshop.Domain.Entities.Vehicle { Id = Guid.Parse("20000010-0000-0000-0000-000000000010"), Plate = "BCD7890", Brand = "Jeep", Model = "Compass", Year = 2024, Color = "Cinza", Engine = "2.0 Turbo", Fuel = "Diesel", Mileage = 8000, Chassis = "9BR56789012345678", Renavam = "90123456789", CreatedAt = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc), CustomerId = Guid.Parse("10000010-0000-0000-0000-000000000010") }
    };

    context.Vehicles.AddRange(vehicles);

    // Seed 10 Service Orders with different statuses
    var serviceOrders = new List<RenovoWorkshop.Domain.Entities.ServiceOrder>
    {
        new RenovoWorkshop.Domain.Entities.ServiceOrder { Id = Guid.Parse("30000001-0000-0000-0000-000000000001"), Number = "OS-2026-001", ProblemReported = "Motor não liga", Diagnosis = "Problema no sistema de ignição", Services = "Troca de velas e cabos", Parts = "Velas de ignição, cabos", Oils = "Óleo motor 5W30", Filters = "Filtro de ar", EstimatedTime = 3.5m, Value = 850.00m, Notes = "Cliente relatou problema ao dar partida", EntryDate = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc), EstimatedDate = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc), FinalDate = new DateTime(2026, 4, 1, 11, 30, 0, DateTimeKind.Utc), Status = "Entregue", ResponsibleUser = "Pedro Oliveira", HasChecklist = true, CustomerId = Guid.Parse("10000001-0000-0000-0000-000000000001"), VehicleId = Guid.Parse("20000001-0000-0000-0000-000000000001") },
        new RenovoWorkshop.Domain.Entities.ServiceOrder { Id = Guid.Parse("30000002-0000-0000-0000-000000000002"), Number = "OS-2026-002", ProblemReported = "Barulho na suspensão", Diagnosis = "Amortecedor dianteiro direito danificado", Services = "Troca de amortecedores", Parts = "Amortecedor dianteiro direito", Oils = "Óleo suspensão", Filters = "Filtro de óleo", EstimatedTime = 4.0m, Value = 1200.00m, Notes = "Verificar também Buchas", EntryDate = new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc), EstimatedDate = new DateTime(2026, 4, 2, 14, 0, 0, DateTimeKind.Utc), Status = "Em andamento", ResponsibleUser = "Carlos Ferreira", HasChecklist = true, CustomerId = Guid.Parse("10000002-0000-0000-0000-000000000002"), VehicleId = Guid.Parse("20000002-0000-0000-0000-000000000002") },
        new RenovoWorkshop.Domain.Entities.ServiceOrder { Id = Guid.Parse("30000003-0000-0000-0000-000000000003"), Number = "OS-2026-003", ProblemReported = "Ar condicionado não gela", Diagnosis = "Compressor com defeito", Services = "Troca de compressor", Parts = "Compressor de ar", Oils = "Óleo compressor", Filters = "Filtro de cabine", EstimatedTime = 5.0m, Value = 2500.00m, Notes = "Aguardando aprovação do cliente", EntryDate = new DateTime(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc), EstimatedDate = new DateTime(2026, 4, 3, 16, 0, 0, DateTimeKind.Utc), Status = "Aguardando aprovação", ResponsibleUser = "Pedro Oliveira", HasChecklist = true, ApprovalLink = "https://renovo.approve/OS-2026-003", CustomerId = Guid.Parse("10000003-0000-0000-0000-000000000003"), VehicleId = Guid.Parse("20000003-0000-0000-0000-000000000003") },
        new RenovoWorkshop.Domain.Entities.ServiceOrder { Id = Guid.Parse("30000004-0000-0000-0000-000000000004"), Number = "OS-2026-004", ProblemReported = "Troca de óleo e revisão", Diagnosis = "Revisão preventiva 30.000km", Services = "Troca de óleo, filtros e revisão geral", Parts = "Filtros, óleo, fluido de freio", Oils = "Óleo motor 5W30", Filters = "Filtro de óleo, filtro de ar, filtro de combustível", EstimatedTime = 2.0m, Value = 650.00m, Notes = "Revisão programada", EntryDate = new DateTime(2026, 4, 4, 7, 30, 0, DateTimeKind.Utc), EstimatedDate = new DateTime(2026, 4, 4, 10, 0, 0, DateTimeKind.Utc), Status = "Na oficina", ResponsibleUser = "Juliana Lima", HasChecklist = false, CustomerId = Guid.Parse("10000004-0000-0000-0000-000000000004"), VehicleId = Guid.Parse("20000004-0000-0000-0000-000000000004") },
        new RenovoWorkshop.Domain.Entities.ServiceOrder { Id = Guid.Parse("30000005-0000-0000-0000-000000000005"), Number = "OS-2026-005", ProblemReported = "Freios com ruído", Diagnosis = "Pastilhas de freio desgastadas", Services = "Troca de pastilhas e discos", Parts = "Pastilhas de freio, discos", Oils = "Fluido de freio", Filters = "Filtro de ar", EstimatedTime = 3.0m, Value = 980.00m, Notes = "Verificar fluido de freio", EntryDate = new DateTime(2026, 4, 5, 8, 30, 0, DateTimeKind.Utc), EstimatedDate = new DateTime(2026, 4, 5, 12, 30, 0, DateTimeKind.Utc), FinalDate = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc), Status = "Entregue", ResponsibleUser = "Carlos Ferreira", HasChecklist = true, CustomerId = Guid.Parse("10000005-0000-0000-0000-000000000005"), VehicleId = Guid.Parse("20000005-0000-0000-0000-000000000005") },
        new RenovoWorkshop.Domain.Entities.ServiceOrder { Id = Guid.Parse("30000006-0000-0000-0000-000000000006"), Number = "OS-2026-006", ProblemReported = "Problema na transmissão", Diagnosis = "Marcha engatando com dificuldade", Services = "Revisão da caixa de câmbio", Parts = "Kit de reparo, fluido de câmbio", Oils = "Óleo de câmbio", Filters = "Filtro de câmbio", EstimatedTime = 6.0m, Value = 3200.00m, Notes = "Serviço especializado", EntryDate = new DateTime(2026, 4, 6, 9, 0, 0, DateTimeKind.Utc), EstimatedDate = new DateTime(2026, 4, 7, 9, 0, 0, DateTimeKind.Utc), Status = "Em andamento", ResponsibleUser = "Pedro Oliveira", HasChecklist = true, CustomerId = Guid.Parse("10000006-0000-0000-0000-000000000006"), VehicleId = Guid.Parse("20000006-0000-0000-0000-000000000006") },
        new RenovoWorkshop.Domain.Entities.ServiceOrder { Id = Guid.Parse("30000007-0000-0000-0000-000000000007"), Number = "OS-2026-007", ProblemReported = "Troca de bateria", Diagnosis = "Bateria com baixa performance", Services = "Troca de bateria", Parts = "Bateria 60Ah", Oils = "N/A", Filters = "N/A", EstimatedTime = 0.5m, Value = 450.00m, Notes = "Bateria original", EntryDate = new DateTime(2026, 4, 7, 11, 0, 0, DateTimeKind.Utc), EstimatedDate = new DateTime(2026, 4, 7, 12, 0, 0, DateTimeKind.Utc), FinalDate = new DateTime(2026, 4, 7, 11, 45, 0, DateTimeKind.Utc), Status = "Entregue", ResponsibleUser = "Juliana Lima", HasChecklist = true, CustomerId = Guid.Parse("10000007-0000-0000-0000-000000000007"), VehicleId = Guid.Parse("20000007-0000-0000-0000-000000000007") },
        new RenovoWorkshop.Domain.Entities.ServiceOrder { Id = Guid.Parse("30000008-0000-0000-0000-000000000008"), Number = "OS-2026-008", ProblemReported = "Alinhamento e balanceamento", Diagnosis = "Desgaste irregular dos pneus", Services = "Alinhamento, balanceamento e rodízio", Parts = "N/A", Oils = "N/A", Filters = "N/A", EstimatedTime = 1.5m, Value = 280.00m, Notes = "Verificar suspensão", EntryDate = new DateTime(2026, 4, 8, 13, 0, 0, DateTimeKind.Utc), EstimatedDate = new DateTime(2026, 4, 8, 15, 0, 0, DateTimeKind.Utc), Status = "Aguardando peças", ResponsibleUser = "Carlos Ferreira", HasChecklist = false, CustomerId = Guid.Parse("10000008-0000-0000-0000-000000000008"), VehicleId = Guid.Parse("20000008-0000-0000-0000-000000000008") },
        new RenovoWorkshop.Domain.Entities.ServiceOrder { Id = Guid.Parse("30000009-0000-0000-0000-000000000009"), Number = "OS-2026-009", ProblemReported = "Instalação de som", Diagnosis = "Cliente deseja instalar sistema de som", Services = "Instalação de som automotivo", Parts = "Central multimídia, alto-falantes", Oils = "N/A", Filters = "N/A", EstimatedTime = 4.0m, Value = 1800.00m, Notes = "Serviço de instalação especializada", EntryDate = new DateTime(2026, 4, 9, 14, 0, 0, DateTimeKind.Utc), EstimatedDate = new DateTime(2026, 4, 9, 19, 0, 0, DateTimeKind.Utc), Status = "Em andamento", ResponsibleUser = "Juliana Lima", HasChecklist = true, CustomerId = Guid.Parse("10000009-0000-0000-0000-000000000009"), VehicleId = Guid.Parse("20000009-0000-0000-0000-000000000009") },
        new RenovoWorkshop.Domain.Entities.ServiceOrder { Id = Guid.Parse("30000010-0000-0000-0000-000000000010"), Number = "OS-2026-010", ProblemReported = "Revisão completa", Diagnosis = "Revisão preventiva 60.000km", Services = "Revisão completa conforme manual", Parts = "Todos os filtros, velas, correias", Oils = "Óleo motor, óleo câmbio, fluido freio", Filters = "Filtro de óleo, ar, combustível, cabine", EstimatedTime = 8.0m, Value = 4500.00m, Notes = "Revisão completa - maior serviço", EntryDate = new DateTime(2026, 4, 10, 8, 0, 0, DateTimeKind.Utc), EstimatedDate = new DateTime(2026, 4, 11, 8, 0, 0, DateTimeKind.Utc), Status = "Recebido", ResponsibleUser = "Pedro Oliveira", HasChecklist = false, CustomerId = Guid.Parse("10000010-0000-0000-0000-000000000010"), VehicleId = Guid.Parse("20000010-0000-0000-0000-000000000010") }
    };

    context.ServiceOrders.AddRange(serviceOrders);

    // Seed 10 Service Order Histories
    var histories = new List<RenovoWorkshop.Domain.Entities.ServiceOrderHistory>
    {
        new RenovoWorkshop.Domain.Entities.ServiceOrderHistory { Id = Guid.Parse("40000001-0000-0000-0000-000000000001"), ServiceOrderId = Guid.Parse("30000001-0000-0000-0000-000000000001"), Status = "Recebido", ChangedAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc), ChangedBy = "Maria Santos", Notes = "Ordem de serviço criada" },
        new RenovoWorkshop.Domain.Entities.ServiceOrderHistory { Id = Guid.Parse("40000002-0000-0000-0000-000000000002"), ServiceOrderId = Guid.Parse("30000001-0000-0000-0000-000000000001"), Status = "Em andamento", ChangedAt = new DateTime(2026, 4, 1, 8, 30, 0, DateTimeKind.Utc), ChangedBy = "Pedro Oliveira", Notes = "Iniciado atendimento" },
        new RenovoWorkshop.Domain.Entities.ServiceOrderHistory { Id = Guid.Parse("40000003-0000-0000-0000-000000000003"), ServiceOrderId = Guid.Parse("30000001-0000-0000-0000-000000000001"), Status = "Entregue", ChangedAt = new DateTime(2026, 4, 1, 11, 30, 0, DateTimeKind.Utc), ChangedBy = "Pedro Oliveira", Notes = "Serviço finalizado com sucesso" },
        new RenovoWorkshop.Domain.Entities.ServiceOrderHistory { Id = Guid.Parse("40000004-0000-0000-0000-000000000004"), ServiceOrderId = Guid.Parse("30000002-0000-0000-0000-000000000002"), Status = "Recebido", ChangedAt = new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc), ChangedBy = "Lucas Pereira", Notes = "Ordem de serviço criada" },
        new RenovoWorkshop.Domain.Entities.ServiceOrderHistory { Id = Guid.Parse("40000005-0000-0000-0000-000000000005"), ServiceOrderId = Guid.Parse("30000002-0000-0000-0000-000000000002"), Status = "Em andamento", ChangedAt = new DateTime(2026, 4, 2, 9, 30, 0, DateTimeKind.Utc), ChangedBy = "Carlos Ferreira", Notes = "Iniciado atendimento" },
        new RenovoWorkshop.Domain.Entities.ServiceOrderHistory { Id = Guid.Parse("40000006-0000-0000-0000-000000000006"), ServiceOrderId = Guid.Parse("30000003-0000-0000-0000-000000000003"), Status = "Recebido", ChangedAt = new DateTime(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc), ChangedBy = "Maria Santos", Notes = "Ordem de serviço criada" },
        new RenovoWorkshop.Domain.Entities.ServiceOrderHistory { Id = Guid.Parse("40000007-0000-0000-0000-000000000007"), ServiceOrderId = Guid.Parse("30000003-0000-0000-0000-000000000003"), Status = "Aguardando aprovação", ChangedAt = new DateTime(2026, 4, 3, 10, 30, 0, DateTimeKind.Utc), ChangedBy = "Pedro Oliveira", Notes = "Aguardando aprovação do orçamento" },
        new RenovoWorkshop.Domain.Entities.ServiceOrderHistory { Id = Guid.Parse("40000008-0000-0000-0000-000000000008"), ServiceOrderId = Guid.Parse("30000004-0000-0000-0000-000000000004"), Status = "Recebido", ChangedAt = new DateTime(2026, 4, 4, 7, 30, 0, DateTimeKind.Utc), ChangedBy = "Lucas Pereira", Notes = "Ordem de serviço criada" },
        new RenovoWorkshop.Domain.Entities.ServiceOrderHistory { Id = Guid.Parse("40000009-0000-0000-0000-000000000009"), ServiceOrderId = Guid.Parse("30000004-0000-0000-0000-000000000004"), Status = "Na oficina", ChangedAt = new DateTime(2026, 4, 4, 8, 0, 0, DateTimeKind.Utc), ChangedBy = "Juliana Lima", Notes = "Veículo na oficina" },
        new RenovoWorkshop.Domain.Entities.ServiceOrderHistory { Id = Guid.Parse("40000010-0000-0000-0000-000000000010"), ServiceOrderId = Guid.Parse("30000005-0000-0000-0000-000000000005"), Status = "Recebido", ChangedAt = new DateTime(2026, 4, 5, 8, 30, 0, DateTimeKind.Utc), ChangedBy = "Maria Santos", Notes = "Ordem de serviço criada" }
    };

    context.ServiceOrderHistories.AddRange(histories);

    // Seed 10 Vehicle Checklists
    var checklists = new List<RenovoWorkshop.Domain.Entities.VehicleCheckList>
    {
        new RenovoWorkshop.Domain.Entities.VehicleCheckList { Id = Guid.Parse("50000001-0000-0000-0000-000000000001"), VehicleId = Guid.Parse("20000001-0000-0000-0000-000000000001"), ServiceOrderId = Guid.Parse("30000001-0000-0000-0000-000000000001"), Mileage = 45000, FuelLevel = "3/4", TireCondition = "Bom", CoolingLevel = "Normal", OilLevel = "Normal", TirePressure = "32 psi", SpareTire = true, Rims = true, Headlights = true, Taillights = true, Mirrors = true, Windows = true, Windshield = true, Wipers = true, Seats = true, Dashboard = true, Multimedia = true, AirConditioning = true, Jack = true, Triangle = true, SpareKey = true, Documents = true, GeneralState = "Bom estado geral", Observations = "Veículo bem conservado", VisualDamage = "Sem danos visíveis", CheckedAt = new DateTime(2026, 4, 1, 8, 15, 0, DateTimeKind.Utc), ResponsibleUser = "Pedro Oliveira", GpsLocation = "-23.550520, -46.633308" },
        new RenovoWorkshop.Domain.Entities.VehicleCheckList { Id = Guid.Parse("50000002-0000-0000-0000-000000000002"), VehicleId = Guid.Parse("20000002-0000-0000-0000-000000000002"), ServiceOrderId = Guid.Parse("30000002-0000-0000-0000-000000000002"), Mileage = 28000, FuelLevel = "1/2", TireCondition = "Regular", CoolingLevel = "Baixo", OilLevel = "Baixo", TirePressure = "28 psi", SpareTire = true, Rims = true, Headlights = true, Taillights = true, Mirrors = true, Windows = true, Windshield = false, Wipers = true, Seats = true, Dashboard = true, Multimedia = true, AirConditioning = false, Jack = true, Triangle = true, SpareKey = true, Documents = true, GeneralState = "Necessita reparos", Observations = "Pneus carecas, nível de água baixo", VisualDamage = "Arranhão na porta esquerda", CheckedAt = new DateTime(2026, 4, 2, 9, 15, 0, DateTimeKind.Utc), ResponsibleUser = "Carlos Ferreira", GpsLocation = "-23.561430, -46.655980" },
        new RenovoWorkshop.Domain.Entities.VehicleCheckList { Id = Guid.Parse("50000003-0000-0000-0000-000000000003"), VehicleId = Guid.Parse("20000003-0000-0000-0000-000000000003"), ServiceOrderId = Guid.Parse("30000003-0000-0000-0000-000000000003"), Mileage = 35000, FuelLevel = "Cheio", TireCondition = "Bom", CoolingLevel = "Normal", OilLevel = "Normal", TirePressure = "35 psi", SpareTire = true, Rims = true, Headlights = true, Taillights = true, Mirrors = true, Windows = true, Windshield = true, Wipers = true, Seats = true, Dashboard = true, Multimedia = true, AirConditioning = true, Jack = true, Triangle = true, SpareKey = true, Documents = true, GeneralState = "Bom estado", Observations = "Cliente informou problema apenas no ar", VisualDamage = "Sem danos", CheckedAt = new DateTime(2026, 4, 3, 10, 15, 0, DateTimeKind.Utc), ResponsibleUser = "Pedro Oliveira", GpsLocation = "-23.587400, -46.657200" },
        new RenovoWorkshop.Domain.Entities.VehicleCheckList { Id = Guid.Parse("50000004-0000-0000-0000-000000000004"), VehicleId = Guid.Parse("20000004-0000-0000-0000-000000000004"), ServiceOrderId = Guid.Parse("30000004-0000-0000-0000-000000000004"), Mileage = 15000, FuelLevel = "3/4", TireCondition = "Bom", CoolingLevel = "Normal", OilLevel = "Normal", TirePressure = "33 psi", SpareTire = true, Rims = true, Headlights = true, Taillights = true, Mirrors = true, Windows = true, Windshield = true, Wipers = true, Seats = true, Dashboard = true, Multimedia = true, AirConditioning = true, Jack = true, Triangle = true, SpareKey = true, Documents = true, GeneralState = "Excelente estado", Observations = "Veículo novo, apenas revisão", VisualDamage = "Sem danos", CheckedAt = new DateTime(2026, 4, 4, 7, 45, 0, DateTimeKind.Utc), ResponsibleUser = "Juliana Lima", GpsLocation = "-23.593400, -46.689400" },
        new RenovoWorkshop.Domain.Entities.VehicleCheckList { Id = Guid.Parse("50000005-0000-0000-0000-000000000005"), VehicleId = Guid.Parse("20000005-0000-0000-0000-000000000005"), ServiceOrderId = Guid.Parse("30000005-0000-0000-0000-000000000005"), Mileage = 22000, FuelLevel = "1/4", TireCondition = "Ruim", CoolingLevel = "Normal", OilLevel = "Baixo", TirePressure = "25 psi", SpareTire = false, Rims = true, Headlights = true, Taillights = true, Mirrors = true, Windows = true, Windshield = true, Wipers = false, Seats = true, Dashboard = true, Multimedia = true, AirConditioning = true, Jack = true, Triangle = true, SpareKey = true, Documents = true, GeneralState = "Ruim estado", Observations = "Pneus necessitam troca urgente", VisualDamage = "Amassado no para-choque dianteiro", CheckedAt = new DateTime(2026, 4, 5, 8, 45, 0, DateTimeKind.Utc), ResponsibleUser = "Carlos Ferreira", GpsLocation = "-23.548900, -46.638800" },
        new RenovoWorkshop.Domain.Entities.VehicleCheckList { Id = Guid.Parse("50000006-0000-0000-0000-000000000006"), VehicleId = Guid.Parse("20000006-0000-0000-0000-000000000006"), ServiceOrderId = Guid.Parse("30000006-0000-0000-0000-000000000006"), Mileage = 12000, FuelLevel = "Cheio", TireCondition = "Bom", CoolingLevel = "Normal", OilLevel = "Normal", TirePressure = "34 psi", SpareTire = true, Rims = true, Headlights = true, Taillights = true, Mirrors = true, Windows = true, Windshield = true, Wipers = true, Seats = true, Dashboard = true, Multimedia = true, AirConditioning = true, Jack = true, Triangle = true, SpareKey = true, Documents = true, GeneralState = "Bom estado", Observations = "Veículo bem cuidado", VisualDamage = "Sem danos", CheckedAt = new DateTime(2026, 4, 6, 9, 15, 0, DateTimeKind.Utc), ResponsibleUser = "Pedro Oliveira", GpsLocation = "-23.564200, -46.653400" },
        new RenovoWorkshop.Domain.Entities.VehicleCheckList { Id = Guid.Parse("50000007-0000-0000-0000-000000000007"), VehicleId = Guid.Parse("20000007-0000-0000-0000-000000000007"), ServiceOrderId = Guid.Parse("30000007-0000-0000-0000-000000000007"), Mileage = 38000, FuelLevel = "1/2", TireCondition = "Regular", CoolingLevel = "Baixo", OilLevel = "Baixo", TirePressure = "29 psi", SpareTire = true, Rims = true, Headlights = true, Taillights = true, Mirrors = true, Windows = true, Windshield = false, Wipers = true, Seats = true, Dashboard = true, Multimedia = true, AirConditioning = false, Jack = true, Triangle = true, SpareKey = true, Documents = true, GeneralState = "Regular", Observations = "Necessita revisão geral", VisualDamage = "Risco no vidro traseiro", CheckedAt = new DateTime(2026, 4, 7, 11, 15, 0, DateTimeKind.Utc), ResponsibleUser = "Juliana Lima", GpsLocation = "-23.527600, -46.668100" },
        new RenovoWorkshop.Domain.Entities.VehicleCheckList { Id = Guid.Parse("50000008-0000-0000-0000-000000000008"), VehicleId = Guid.Parse("20000008-0000-0000-0000-000000000008"), ServiceOrderId = Guid.Parse("30000008-0000-0000-0000-000000000008"), Mileage = 25000, FuelLevel = "3/4", TireCondition = "Bom", CoolingLevel = "Normal", OilLevel = "Normal", TirePressure = "31 psi", SpareTire = true, Rims = true, Headlights = true, Taillights = true, Mirrors = true, Windows = true, Windshield = true, Wipers = true, Seats = true, Dashboard = true, Multimedia = true, AirConditioning = true, Jack = true, Triangle = true, SpareKey = true, Documents = true, GeneralState = "Bom estado", Observations = "Apenas alinhamento", VisualDamage = "Sem danos", CheckedAt = new DateTime(2026, 4, 8, 13, 15, 0, DateTimeKind.Utc), ResponsibleUser = "Carlos Ferreira", GpsLocation = "-23.615600, -46.690700" },
        new RenovoWorkshop.Domain.Entities.VehicleCheckList { Id = Guid.Parse("50000009-0000-0000-0000-000000000009"), VehicleId = Guid.Parse("20000009-0000-0000-0000-000000000009"), ServiceOrderId = Guid.Parse("30000009-0000-0000-0000-000000000009"), Mileage = 18000, FuelLevel = "Cheio", TireCondition = "Bom", CoolingLevel = "Normal", OilLevel = "Normal", TirePressure = "33 psi", SpareTire = true, Rims = true, Headlights = true, Taillights = true, Mirrors = true, Windows = true, Windshield = true, Wipers = true, Seats = true, Dashboard = true, Multimedia = true, AirConditioning = true, Jack = true, Triangle = true, SpareKey = true, Documents = true, GeneralState = "Bom estado", Observations = "Veículo para instalação de som", VisualDamage = "Sem danos", CheckedAt = new DateTime(2026, 4, 9, 14, 15, 0, DateTimeKind.Utc), ResponsibleUser = "Juliana Lima", GpsLocation = "-23.588900, -46.658600" },
        new RenovoWorkshop.Domain.Entities.VehicleCheckList { Id = Guid.Parse("50000010-0000-0000-0000-000000000010"), VehicleId = Guid.Parse("20000010-0000-0000-0000-000000000010"), ServiceOrderId = Guid.Parse("30000010-0000-0000-0000-000000000010"), Mileage = 8000, FuelLevel = "Cheio", TireCondition = "Bom", CoolingLevel = "Normal", OilLevel = "Normal", TirePressure = "35 psi", SpareTire = true, Rims = true, Headlights = true, Taillights = true, Mirrors = true, Windows = true, Windshield = true, Wipers = true, Seats = true, Dashboard = true, Multimedia = true, AirConditioning = true, Jack = true, Triangle = true, SpareKey = true, Documents = true, GeneralState = "Excelente estado", Observations = "Veículo zero km, apenas revisão", VisualDamage = "Sem danos", CheckedAt = new DateTime(2026, 4, 10, 8, 15, 0, DateTimeKind.Utc), ResponsibleUser = "Pedro Oliveira", GpsLocation = "-23.549900, -46.634900" }
    };

    context.VehicleCheckLists.AddRange(checklists);

    context.SaveChanges();
}

static void SeedInventory(RenovoWorkshopDbContext context)
{
    // Seed 20 Inventory Items (peças reais de oficina, valores em R$)
    var inventoryItems = new List<RenovoWorkshop.Domain.Entities.InventoryItem>
    {
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000001-0000-0000-0000-000000000001"), Code = "OL-5W30", Description = "Óleo Motor 5W30 Sintético (1L)", Supplier = "Distribuidora AutoPeças SP", Brand = "Mobil", Category = "Óleo", Quantity = 60, MinimumQuantity = 20, Location = "Prateleira A1", PurchaseValue = 28.00m, AverageValue = 28.00m, SaleValue = 45.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000002-0000-0000-0000-000000000002"), Code = "OL-20W50", Description = "Óleo Motor 20W50 Mineral (1L)", Supplier = "Distribuidora AutoPeças SP", Brand = "Lubrax", Category = "Óleo", Quantity = 40, MinimumQuantity = 15, Location = "Prateleira A1", PurchaseValue = 18.00m, AverageValue = 18.00m, SaleValue = 32.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000003-0000-0000-0000-000000000003"), Code = "OL-ATF", Description = "Óleo de Câmbio ATF", Supplier = "Distribuidora AutoPeças SP", Brand = "Mobil", Category = "Óleo", Quantity = 20, MinimumQuantity = 8, Location = "Prateleira A2", PurchaseValue = 35.00m, AverageValue = 35.00m, SaleValue = 55.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000004-0000-0000-0000-000000000004"), Code = "FL-OLEO", Description = "Filtro de Óleo", Supplier = "Mahle Distribuidora", Brand = "Mahle", Category = "Filtros", Quantity = 50, MinimumQuantity = 15, Location = "Prateleira B1", PurchaseValue = 15.00m, AverageValue = 15.00m, SaleValue = 32.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000005-0000-0000-0000-000000000005"), Code = "FL-AR", Description = "Filtro de Ar", Supplier = "Mahle Distribuidora", Brand = "Fram", Category = "Filtros", Quantity = 40, MinimumQuantity = 12, Location = "Prateleira B1", PurchaseValue = 22.00m, AverageValue = 22.00m, SaleValue = 45.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000006-0000-0000-0000-000000000006"), Code = "FL-COMB", Description = "Filtro de Combustível", Supplier = "Mahle Distribuidora", Brand = "Tecfil", Category = "Filtros", Quantity = 30, MinimumQuantity = 10, Location = "Prateleira B2", PurchaseValue = 25.00m, AverageValue = 25.00m, SaleValue = 48.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000007-0000-0000-0000-000000000007"), Code = "FL-CABINE", Description = "Filtro de Cabine (Ar-condicionado)", Supplier = "Mahle Distribuidora", Brand = "Fram", Category = "Filtros", Quantity = 35, MinimumQuantity = 10, Location = "Prateleira B2", PurchaseValue = 30.00m, AverageValue = 30.00m, SaleValue = 58.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000008-0000-0000-0000-000000000008"), Code = "FR-PAST-D", Description = "Pastilha de Freio Dianteira (jogo)", Supplier = "Cofap Peças", Brand = "Cofap", Category = "Freios", Quantity = 25, MinimumQuantity = 8, Location = "Prateleira C1", PurchaseValue = 90.00m, AverageValue = 90.00m, SaleValue = 180.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000009-0000-0000-0000-000000000009"), Code = "FR-PAST-T", Description = "Pastilha de Freio Traseira (jogo)", Supplier = "Cofap Peças", Brand = "Cofap", Category = "Freios", Quantity = 25, MinimumQuantity = 8, Location = "Prateleira C1", PurchaseValue = 70.00m, AverageValue = 70.00m, SaleValue = 150.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000010-0000-0000-0000-000000000010"), Code = "FR-DISCO-D", Description = "Disco de Freio Dianteiro (par)", Supplier = "Cofap Peças", Brand = "Fremax", Category = "Freios", Quantity = 15, MinimumQuantity = 5, Location = "Prateleira C2", PurchaseValue = 180.00m, AverageValue = 180.00m, SaleValue = 350.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000011-0000-0000-0000-000000000011"), Code = "FR-FLUIDO", Description = "Fluido de Freio DOT4 (500ml)", Supplier = "Cofap Peças", Brand = "Bosch", Category = "Freios", Quantity = 30, MinimumQuantity = 10, Location = "Prateleira C2", PurchaseValue = 18.00m, AverageValue = 18.00m, SaleValue = 32.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000012-0000-0000-0000-000000000012"), Code = "MOT-CORR-DENT", Description = "Correia Dentada", Supplier = "Gates Distribuidora", Brand = "Gates", Category = "Motor", Quantity = 12, MinimumQuantity = 5, Location = "Prateleira D1", PurchaseValue = 85.00m, AverageValue = 85.00m, SaleValue = 160.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000013-0000-0000-0000-000000000013"), Code = "MOT-CORR-POLYV", Description = "Correia Poly-V (Alternador)", Supplier = "Gates Distribuidora", Brand = "Gates", Category = "Motor", Quantity = 15, MinimumQuantity = 5, Location = "Prateleira D1", PurchaseValue = 45.00m, AverageValue = 45.00m, SaleValue = 90.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000014-0000-0000-0000-000000000014"), Code = "MOT-VELA", Description = "Vela de Ignição (unidade)", Supplier = "NGK Distribuidora", Brand = "NGK", Category = "Motor", Quantity = 80, MinimumQuantity = 20, Location = "Prateleira D2", PurchaseValue = 18.00m, AverageValue = 18.00m, SaleValue = 35.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000015-0000-0000-0000-000000000015"), Code = "ELE-BAT-60AH", Description = "Bateria 60Ah", Supplier = "Moura Baterias", Brand = "Moura", Category = "Elétrica", Quantity = 10, MinimumQuantity = 3, Location = "Prateleira E1", PurchaseValue = 320.00m, AverageValue = 320.00m, SaleValue = 480.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000016-0000-0000-0000-000000000016"), Code = "SUS-AMORT-D", Description = "Amortecedor Dianteiro (unidade)", Supplier = "Cofap Peças", Brand = "Cofap", Category = "Suspensão", Quantity = 16, MinimumQuantity = 4, Location = "Prateleira F1", PurchaseValue = 150.00m, AverageValue = 150.00m, SaleValue = 280.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000017-0000-0000-0000-000000000017"), Code = "SUS-AMORT-T", Description = "Amortecedor Traseiro (unidade)", Supplier = "Cofap Peças", Brand = "Cofap", Category = "Suspensão", Quantity = 16, MinimumQuantity = 4, Location = "Prateleira F1", PurchaseValue = 130.00m, AverageValue = 130.00m, SaleValue = 250.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000018-0000-0000-0000-000000000018"), Code = "ACE-PALHETA", Description = "Palheta Limpador de Para-brisa (par)", Supplier = "Bosch Distribuidora", Brand = "Bosch", Category = "Acessórios", Quantity = 30, MinimumQuantity = 10, Location = "Prateleira G1", PurchaseValue = 25.00m, AverageValue = 25.00m, SaleValue = 48.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000019-0000-0000-0000-000000000019"), Code = "ELE-LAMP-H4", Description = "Lâmpada de Farol H4", Supplier = "Bosch Distribuidora", Brand = "Osram", Category = "Elétrica", Quantity = 40, MinimumQuantity = 12, Location = "Prateleira E2", PurchaseValue = 20.00m, AverageValue = 20.00m, SaleValue = 38.00m },
        new RenovoWorkshop.Domain.Entities.InventoryItem { Id = Guid.Parse("60000020-0000-0000-0000-000000000020"), Code = "ARR-ADITIVO", Description = "Aditivo Radiador (1L)", Supplier = "Distribuidora AutoPeças SP", Brand = "Paraflu", Category = "Arrefecimento", Quantity = 25, MinimumQuantity = 8, Location = "Prateleira A2", PurchaseValue = 22.00m, AverageValue = 22.00m, SaleValue = 40.00m },
    };

    context.InventoryItems.AddRange(inventoryItems);
    context.SaveChanges();
}

static void SeedWorkshopSettings(RenovoWorkshopDbContext context)
{
    context.WorkshopSettings.Add(new RenovoWorkshop.Domain.Entities.WorkshopSettings
    {
        Id = Guid.Parse("70000001-0000-0000-0000-000000000001"),
        Name = "Renovo Centro Automotivo",
        Phone = "(11) 4002-8922",
        Email = "contato@renovoautomotivo.com.br",
        Address = "Av. das Nações, 1500 - São Paulo, SP",
    });
    context.SaveChanges();
}