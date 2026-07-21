using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RenovoWorkshop.Infrastructure.Migrations
{
    public partial class SeedTestData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed 10 Users with different roles
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Permissions", "Role", "UserName" },
                values: new object[]
                {
                    new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@renovo.com.br", "Administrador Master", true, "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2", "dashboard.view,customers.read,customers.write,vehicles.read,vehicles.write,orders.read,orders.write,inventory.read,inventory.write,users.manage", "Administrador", "admin"
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Permissions", "Role", "UserName" },
                values: new object[]
                {
                    new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc), "joao.gerente@renovo.com.br", "João Silva", true, "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2", "dashboard.view,customers.read,customers.write,vehicles.read,vehicles.write,orders.read,orders.write,inventory.read,inventory.write", "Gerente", "joao.silva"
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Permissions", "Role", "UserName" },
                values: new object[]
                {
                    new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "maria.recepcao@renovo.com.br", "Maria Santos", true, "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2", "dashboard.view,customers.read,customers.write,vehicles.read,vehicles.write,orders.read,orders.write", "Recepção", "maria.santos"
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Permissions", "Role", "UserName" },
                values: new object[]
                {
                    new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2026, 1, 4, 0, 0, 0, 0, DateTimeKind.Utc), "pedro.mecanico@renovo.com.br", "Pedro Oliveira", true, "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2", "dashboard.view,vehicles.read,orders.read,orders.write", "Mecânico", "pedro.oliveira"
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Permissions", "Role", "UserName" },
                values: new object[]
                {
                    new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), "carlos.mecanico@renovo.com.br", "Carlos Ferreira", true, "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2", "dashboard.view,vehicles.read,orders.read,orders.write", "Mecânico", "carlos.ferreira"
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Permissions", "Role", "UserName" },
                values: new object[]
                {
                    new Guid("66666666-6666-6666-6666-666666666666"), new DateTime(2026, 1, 6, 0, 0, 0, 0, DateTimeKind.Utc), "ana.almoxarifado@renovo.com.br", "Ana Costa", true, "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2", "inventory.read,inventory.write,orders.read", "Almoxarifado", "ana.costa"
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Permissions", "Role", "UserName" },
                values: new object[]
                {
                    new Guid("77777777-7777-7777-7777-777777777777"), new DateTime(2026, 1, 7, 0, 0, 0, 0, DateTimeKind.Utc), "lucas.recepcao@renovo.com.br", "Lucas Pereira", true, "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2", "dashboard.view,customers.read,customers.write,vehicles.read,vehicles.write,orders.read,orders.write", "Recepção", "lucas.pereira"
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Permissions", "Role", "UserName" },
                values: new object[]
                {
                    new Guid("88888888-8888-8888-8888-888888888888"), new DateTime(2026, 1, 8, 0, 0, 0, 0, DateTimeKind.Utc), "juliana.mecanico@renovo.com.br", "Juliana Lima", true, "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2", "dashboard.view,vehicles.read,orders.read,orders.write", "Mecânico", "juliana.lima"
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Permissions", "Role", "UserName" },
                values: new object[]
                {
                    new Guid("99999999-9999-9999-9999-999999999999"), new DateTime(2026, 1, 9, 0, 0, 0, 0, DateTimeKind.Utc), "fernanda.gerente@renovo.com.br", "Fernanda Souza", true, "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2", "dashboard.view,customers.read,customers.write,vehicles.read,vehicles.write,orders.read,orders.write,inventory.read,inventory.write", "Gerente", "fernanda.souza"
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Permissions", "Role", "UserName" },
                values: new object[]
                {
                    new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "roberto.almoxarifado@renovo.com.br", "Roberto Almeida", true, "$2a$11$DE/XOjuMgCwZHSgyv9STWOwUNW9nI/Rds4nAwA2gWmfB1zZ8bKgq2", "inventory.read,inventory.write,orders.read", "Almoxarifado", "roberto.almeida"
                });

            // Seed 10 Customers
            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Name", "Document", "Phone", "WhatsApp", "Email", "Address", "Notes", "CreatedAt" },
                values: new object[] { new Guid("10000001-0000-0000-0000-000000000001"), "Carlos Eduardo Mendes", "12345678901", "(11) 98765-4321", "(11) 98765-4321", "carlos.mendes@email.com", "Rua das Flores, 123, São Paulo, SP", "Cliente preferencial", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Name", "Document", "Phone", "WhatsApp", "Email", "Address", "Notes", "CreatedAt" },
                values: new object[] { new Guid("10000002-0000-0000-0000-000000000002"), "Ana Paula Rodrigues", "23456789012", "(11) 97654-3210", "(11) 97654-3210", "ana.rodrigues@email.com", "Av. Paulista, 456, São Paulo, SP", "Cliente desde 2024", new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Name", "Document", "Phone", "WhatsApp", "Email", "Address", "Notes", "CreatedAt" },
                values: new object[] { new Guid("10000003-0000-0000-0000-000000000003"), "José Carlos Almeida", "34567890123", "(11) 96543-2109", "(11) 96543-2109", "jose.almeida@email.com", "Rua Augusta, 789, São Paulo, SP", "Empresário", new DateTime(2026, 2, 3, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Name", "Document", "Phone", "WhatsApp", "Email", "Address", "Notes", "CreatedAt" },
                values: new object[] { new Guid("10000004-0000-0000-0000-000000000004"), "Mariana Costa Silva", "45678901234", "(11) 95432-1098", "(11) 95432-1098", "mariana.silva@email.com", "Alameda Santos, 321, São Paulo, SP", "Cliente VIP", new DateTime(2026, 2, 4, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Name", "Document", "Phone", "WhatsApp", "Email", "Address", "Notes", "CreatedAt" },
                values: new object[] { new Guid("10000005-0000-0000-0000-000000000005"), "Roberto Fernandes Lima", "56789012345", "(11) 94321-0987", "(11) 94321-0987", "roberto.lima@email.com", "Rua Oscar Freire, 654, São Paulo, SP", "Prefere atendimento pela manhã", new DateTime(2026, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Name", "Document", "Phone", "WhatsApp", "Email", "Address", "Notes", "CreatedAt" },
                values: new object[] { new Guid("10000006-0000-0000-0000-000000000006"), "Patricia Oliveira Souza", "67890123456", "(11) 93210-9876", "(11) 93210-9876", "patricia.souza@email.com", "Av. Brigadeiro Faria Lima, 987, São Paulo, SP", "Cliente nova", new DateTime(2026, 2, 6, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Name", "Document", "Phone", "WhatsApp", "Email", "Address", "Notes", "CreatedAt" },
                values: new object[] { new Guid("10000007-0000-0000-0000-000000000007"), "Fernando Costa Pereira", "78901234567", "(11) 92109-8765", "(11) 92109-8765", "fernando.pereira@email.com", "Rua Haddock Lobo, 147, São Paulo, SP", "Frota de veículos", new DateTime(2026, 2, 7, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Name", "Document", "Phone", "WhatsApp", "Email", "Address", "Notes", "CreatedAt" },
                values: new object[] { new Guid("10000008-0000-0000-0000-000000000008"), "Camila Rodrigues Santos", "89012345678", "(11) 91098-7654", "(11) 91098-7654", "camila.santos@email.com", "Av. Rebouças, 258, São Paulo, SP", "Cliente fiel", new DateTime(2026, 2, 8, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Name", "Document", "Phone", "WhatsApp", "Email", "Address", "Notes", "CreatedAt" },
                values: new object[] { new Guid("10000009-0000-0000-0000-000000000009"), "Ricardo Almeida Costa", "90123456789", "(11) 90987-6543", "(11) 90987-6543", "ricardo.costa@email.com", "Rua Pamplona, 369, São Paulo, SP", "Empresa", new DateTime(2026, 2, 9, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Name", "Document", "Phone", "WhatsApp", "Email", "Address", "Notes", "CreatedAt" },
                values: new object[] { new Guid("10000010-0000-0000-0000-000000000010"), "Juliana Ferreira Lima", "01234567890", "(11) 89876-5432", "(11) 89876-5432", "juliana.lima@email.com", "Av. Moema, 741, São Paulo, SP", "Cliente desde 2023", new DateTime(2026, 2, 10, 0, 0, 0, 0, DateTimeKind.Utc) });

            // Seed 10 Vehicles
            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "Plate", "Brand", "Model", "Year", "Color", "Engine", "Fuel", "Mileage", "Chassis", "Renavam", "Photos", "CreatedAt", "CustomerId" },
                values: new object[] { new Guid("20000001-0000-0000-0000-000000000001"), "ABC1234", "Toyota", "Corolla", 2022, "Prata", "2.0 Flex", "Flex", 45000, "9BR12345678901234", "01234567890", null, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("10000001-0000-0000-0000-000000000001") });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "Plate", "Brand", "Model", "Year", "Color", "Engine", "Fuel", "Mileage", "Chassis", "Renavam", "Photos", "CreatedAt", "CustomerId" },
                values: new object[] { new Guid("20000002-0000-0000-0000-000000000002"), "DEF5678", "Honda", "Civic", 2023, "Preto", "1.5 Turbo", "Flex", 28000, "9BR23456789012345", "12345678901", null, new DateTime(2026, 3, 2, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("10000002-0000-0000-0000-000000000002") });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "Plate", "Brand", "Model", "Year", "Color", "Engine", "Fuel", "Mileage", "Chassis", "Renavam", "Photos", "CreatedAt", "CustomerId" },
                values: new object[] { new Guid("20000003-0000-0000-0000-000000000003"), "GHI9012", "Volkswagen", "Golf", 2021, "Branco", "1.4 TSI", "Flex", 35000, "9BW12345678901234", "23456789012", null, new DateTime(2026, 3, 3, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("10000003-0000-0000-0000-000000000003") });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "Plate", "Brand", "Model", "Year", "Color", "Engine", "Fuel", "Mileage", "Chassis", "Renavam", "Photos", "CreatedAt", "CustomerId" },
                values: new object[] { new Guid("20000004-0000-0000-0000-000000000004"), "JKL3456", "Hyundai", "HB20", 2023, "Vermelho", "1.0 Turbo", "Flex", 15000, "9BH12345678901234", "34567890123", null, new DateTime(2026, 3, 4, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("10000004-0000-0000-0000-000000000004") });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "Plate", "Brand", "Model", "Year", "Color", "Engine", "Fuel", "Mileage", "Chassis", "Renavam", "Photos", "CreatedAt", "CustomerId" },
                values: new object[] { new Guid("20000005-0000-0000-0000-000000000005"), "MNO7890", "Fiat", "Argo", 2022, "Azul", "1.3 Firefly", "Flex", 22000, "9BD12345678901234", "45678901234", null, new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("10000005-0000-0000-0000-000000000005") });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "Plate", "Brand", "Model", "Year", "Color", "Engine", "Fuel", "Mileage", "Chassis", "Renavam", "Photos", "CreatedAt", "CustomerId" },
                values: new object[] { new Guid("20000006-0000-0000-0000-000000000006"), "PQR1234", "Chevrolet", "Onix", 2023, "Cinza", "1.0 Turbo", "Flex", 12000, "9BG12345678901234", "56789012345", null, new DateTime(2026, 3, 6, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("10000006-0000-0000-0000-000000000006") });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "Plate", "Brand", "Model", "Year", "Color", "Engine", "Fuel", "Mileage", "Chassis", "Renavam", "Photos", "CreatedAt", "CustomerId" },
                values: new object[] { new Guid("20000007-0000-0000-0000-000000000007"), "STU5678", "Ford", "Ka", 2021, "Prata", "1.0 Ti-VCT", "Flex", 38000, "9BF12345678901234", "67890123456", null, new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("10000007-0000-0000-0000-000000000007") });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "Plate", "Brand", "Model", "Year", "Color", "Engine", "Fuel", "Mileage", "Chassis", "Renavam", "Photos", "CreatedAt", "CustomerId" },
                values: new object[] { new Guid("20000008-0000-0000-0000-000000000008"), "VWX9012", "Renault", "Kwid", 2022, "Branco", "1.0 SCe", "Flex", 25000, "9BR34567890123456", "78901234567", null, new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("10000008-0000-0000-0000-000000000008") });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "Plate", "Brand", "Model", "Year", "Color", "Engine", "Fuel", "Mileage", "Chassis", "Renavam", "Photos", "CreatedAt", "CustomerId" },
                values: new object[] { new Guid("20000009-0000-0000-0000-000000000009"), "YZA3456", "Nissan", "Kicks", 2023, "Preto", "1.6", "Flex", 18000, "9BR45678901234567", "89012345678", null, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("10000009-0000-0000-0000-000000000009") });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "Plate", "Brand", "Model", "Year", "Color", "Engine", "Fuel", "Mileage", "Chassis", "Renavam", "Photos", "CreatedAt", "CustomerId" },
                values: new object[] { new Guid("20000010-0000-0000-0000-000000000010"), "BCD7890", "Jeep", "Compass", 2024, "Cinza", "2.0 Turbo", "Diesel", 8000, "9BR56789012345678", "90123456789", null, new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("10000010-0000-0000-0000-000000000010") });

            // Seed 10 Service Orders with different statuses
            migrationBuilder.InsertData(
                table: "ServiceOrders",
                columns: new[] { "Id", "Number", "ProblemReported", "Diagnosis", "Services", "Parts", "Oils", "Filters", "EstimatedTime", "Value", "Notes", "EntryDate", "EstimatedDate", "FinalDate", "Status", "ResponsibleUser", "HasChecklist", "ChecklistId", "ApprovalLink", "CustomerId", "VehicleId" },
                values: new object[] { new Guid("30000001-0000-0000-0000-000000000001"), "OS-2026-001", "Motor não liga", "Problema no sistema de ignição", "Troca de velas e cabos", "Velas de ignição, cabos", "Óleo motor 5W30", "Filtro de ar", 3.5m, 850.00m, "Cliente relatou problema ao dar partida", new DateTime(2026, 4, 1, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 1, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 1, 11, 30, 0, 0, DateTimeKind.Utc), "Concluído", "Pedro Oliveira", true, null, null, new Guid("10000001-0000-0000-0000-000000000001"), new Guid("20000001-0000-0000-0000-000000000001") });

            migrationBuilder.InsertData(
                table: "ServiceOrders",
                columns: new[] { "Id", "Number", "ProblemReported", "Diagnosis", "Services", "Parts", "Oils", "Filters", "EstimatedTime", "Value", "Notes", "EntryDate", "EstimatedDate", "FinalDate", "Status", "ResponsibleUser", "HasChecklist", "ChecklistId", "ApprovalLink", "CustomerId", "VehicleId" },
                values: new object[] { new Guid("30000002-0000-0000-0000-000000000002"), "OS-2026-002", "Barulho na suspensão", "Amortecedor dianteiro direito danificado", "Troca de amortecedores", "Amortecedor dianteiro direito", "Óleo suspensão", "Filtro de óleo", 4.0m, 1200.00m, "Verificar também Buchas", new DateTime(2026, 4, 2, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 2, 14, 0, 0, 0, DateTimeKind.Utc), null, "Em andamento", "Carlos Ferreira", true, null, null, new Guid("10000002-0000-0000-0000-000000000002"), new Guid("20000002-0000-0000-0000-000000000002") });

            migrationBuilder.InsertData(
                table: "ServiceOrders",
                columns: new[] { "Id", "Number", "ProblemReported", "Diagnosis", "Services", "Parts", "Oils", "Filters", "EstimatedTime", "Value", "Notes", "EntryDate", "EstimatedDate", "FinalDate", "Status", "ResponsibleUser", "HasChecklist", "ChecklistId", "ApprovalLink", "CustomerId", "VehicleId" },
                values: new object[] { new Guid("30000003-0000-0000-0000-000000000003"), "OS-2026-003", "Ar condicionado não gela", "Compressor com defeito", "Troca de compressor", "Compressor de ar", "Óleo compressor", "Filtro de cabine", 5.0m, 2500.00m, "Aguardando aprovação do cliente", new DateTime(2026, 4, 3, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 3, 16, 0, 0, 0, DateTimeKind.Utc), null, "Aguardando aprovação", "Pedro Oliveira", true, null, "https://renovo.approve/OS-2026-003", new Guid("10000003-0000-0000-0000-000000000003"), new Guid("20000003-0000-0000-0000-000000000003") });

            migrationBuilder.InsertData(
                table: "ServiceOrders",
                columns: new[] { "Id", "Number", "ProblemReported", "Diagnosis", "Services", "Parts", "Oils", "Filters", "EstimatedTime", "Value", "Notes", "EntryDate", "EstimatedDate", "FinalDate", "Status", "ResponsibleUser", "HasChecklist", "ChecklistId", "ApprovalLink", "CustomerId", "VehicleId" },
                values: new object[] { new Guid("30000004-0000-0000-0000-000000000004"), "OS-2026-004", "Troca de óleo e revisão", "Revisão preventiva 30.000km", "Troca de óleo, filtros e revisão geral", "Filtros, óleo, fluido de freio", "Óleo motor 5W30", "Filtro de óleo, filtro de ar, filtro de combustível", 2.0m, 650.00m, "Revisão programada", new DateTime(2026, 4, 4, 7, 30, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 4, 10, 0, 0, 0, DateTimeKind.Utc), null, "Na oficina", "Juliana Lima", false, null, null, new Guid("10000004-0000-0000-0000-000000000004"), new Guid("20000004-0000-0000-0000-000000000004") });

            migrationBuilder.InsertData(
                table: "ServiceOrders",
                columns: new[] { "Id", "Number", "ProblemReported", "Diagnosis", "Services", "Parts", "Oils", "Filters", "EstimatedTime", "Value", "Notes", "EntryDate", "EstimatedDate", "FinalDate", "Status", "ResponsibleUser", "HasChecklist", "ChecklistId", "ApprovalLink", "CustomerId", "VehicleId" },
                values: new object[] { new Guid("30000005-0000-0000-0000-000000000005"), "OS-2026-005", "Freios com ruído", "Pastilhas de freio desgastadas", "Troca de pastilhas e discos", "Pastilhas de freio, discos", "Fluido de freio", "Filtro de ar", 3.0m, 980.00m, "Verificar fluido de freio", new DateTime(2026, 4, 5, 8, 30, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 12, 30, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 12, 0, 0, 0, DateTimeKind.Utc), "Concluído", "Carlos Ferreira", true, null, null, new Guid("10000005-0000-0000-0000-000000000005"), new Guid("20000005-0000-0000-0000-000000000005") });

            migrationBuilder.InsertData(
                table: "ServiceOrders",
                columns: new[] { "Id", "Number", "ProblemReported", "Diagnosis", "Services", "Parts", "Oils", "Filters", "EstimatedTime", "Value", "Notes", "EntryDate", "EstimatedDate", "FinalDate", "Status", "ResponsibleUser", "HasChecklist", "ChecklistId", "ApprovalLink", "CustomerId", "VehicleId" },
                values: new object[] { new Guid("30000006-0000-0000-0000-000000000006"), "OS-2026-006", "Problema na transmissão", "Marcha engatando com dificuldade", "Revisão da caixa de câmbio", "Kit de reparo, fluido de câmbio", "Óleo de câmbio", "Filtro de câmbio", 6.0m, 3200.00m, "Serviço especializado", new DateTime(2026, 4, 6, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 7, 9, 0, 0, 0, DateTimeKind.Utc), null, "Em andamento", "Pedro Oliveira", true, null, null, new Guid("10000006-0000-0000-0000-000000000006"), new Guid("20000006-0000-0000-0000-000000000006") });

            migrationBuilder.InsertData(
                table: "ServiceOrders",
                columns: new[] { "Id", "Number", "ProblemReported", "Diagnosis", "Services", "Parts", "Oils", "Filters", "EstimatedTime", "Value", "Notes", "EntryDate", "EstimatedDate", "FinalDate", "Status", "ResponsibleUser", "HasChecklist", "ChecklistId", "ApprovalLink", "CustomerId", "VehicleId" },
                values: new object[] { new Guid("30000007-0000-0000-0000-000000000007"), "OS-2026-007", "Troca de bateria", "Bateria com baixa performance", "Troca de bateria", "Bateria 60Ah", "N/A", "N/A", 0.5m, 450.00m, "Bateria original", new DateTime(2026, 4, 7, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 7, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 7, 11, 45, 0, 0, DateTimeKind.Utc), "Concluído", "Juliana Lima", true, null, null, new Guid("10000007-0000-0000-0000-000000000007"), new Guid("20000007-0000-0000-0000-000000000007") });

            migrationBuilder.InsertData(
                table: "ServiceOrders",
                columns: new[] { "Id", "Number", "ProblemReported", "Diagnosis", "Services", "Parts", "Oils", "Filters", "EstimatedTime", "Value", "Notes", "EntryDate", "EstimatedDate", "FinalDate", "Status", "ResponsibleUser", "HasChecklist", "ChecklistId", "ApprovalLink", "CustomerId", "VehicleId" },
                values: new object[] { new Guid("30000008-0000-0000-0000-000000000008"), "OS-2026-008", "Alinhamento e balanceamento", "Desgaste irregular dos pneus", "Alinhamento, balanceamento e rodízio", "N/A", "N/A", "N/A", 1.5m, 280.00m, "Verificar suspensão", new DateTime(2026, 4, 8, 13, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 8, 15, 0, 0, 0, DateTimeKind.Utc), null, "Aguardando peças", "Carlos Ferreira", false, null, null, new Guid("10000008-0000-0000-0000-000000000008"), new Guid("20000008-0000-0000-0000-000000000008") });

            migrationBuilder.InsertData(
                table: "ServiceOrders",
                columns: new[] { "Id", "Number", "ProblemReported", "Diagnosis", "Services", "Parts", "Oils", "Filters", "EstimatedTime", "Value", "Notes", "EntryDate", "EstimatedDate", "FinalDate", "Status", "ResponsibleUser", "HasChecklist", "ChecklistId", "ApprovalLink", "CustomerId", "VehicleId" },
                values: new object[] { new Guid("30000009-0000-0000-0000-000000000009"), "OS-2026-009", "Instalação de som", "Cliente deseja instalar sistema de som", "Instalação de som automotivo", "Central multimídia, alto-falantes", "N/A", "N/A", 4.0m, 1800.00m, "Serviço de instalação especializada", new DateTime(2026, 4, 9, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 9, 19, 0, 0, 0, DateTimeKind.Utc), null, "Em andamento", "Juliana Lima", true, null, null, new Guid("10000009-0000-0000-0000-000000000009"), new Guid("20000009-0000-0000-0000-000000000009") });

            migrationBuilder.InsertData(
                table: "ServiceOrders",
                columns: new[] { "Id", "Number", "ProblemReported", "Diagnosis", "Services", "Parts", "Oils", "Filters", "EstimatedTime", "Value", "Notes", "EntryDate", "EstimatedDate", "FinalDate", "Status", "ResponsibleUser", "HasChecklist", "ChecklistId", "ApprovalLink", "CustomerId", "VehicleId" },
                values: new object[] { new Guid("30000010-0000-0000-0000-000000000010"), "OS-2026-010", "Revisão completa", "Revisão preventiva 60.000km", "Revisão completa conforme manual", "Todos os filtros, velas, correias", "Óleo motor, óleo câmbio, fluido freio", "Filtro de óleo, ar, combustível, cabine", 8.0m, 4500.00m, "Revisão completa - maior serviço", new DateTime(2026, 4, 10, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 11, 8, 0, 0, 0, DateTimeKind.Utc), null, "Recebido", "Pedro Oliveira", false, null, null, new Guid("10000010-0000-0000-0000-000000000010"), new Guid("20000010-0000-0000-0000-000000000010") });

            // Seed 10 Service Order Histories
            migrationBuilder.InsertData(
                table: "ServiceOrderHistories",
                columns: new[] { "Id", "ServiceOrderId", "Status", "ChangedAt", "ChangedBy", "Notes" },
                values: new object[] { new Guid("40000001-0000-0000-0000-000000000001"), new Guid("30000001-0000-0000-0000-000000000001"), "Recebido", new DateTime(2026, 4, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Maria Santos", "Ordem de serviço criada" });

            migrationBuilder.InsertData(
                table: "ServiceOrderHistories",
                columns: new[] { "Id", "ServiceOrderId", "Status", "ChangedAt", "ChangedBy", "Notes" },
                values: new object[] { new Guid("40000002-0000-0000-0000-000000000002"), new Guid("30000001-0000-0000-0000-000000000001"), "Em andamento", new DateTime(2026, 4, 1, 8, 30, 0, 0, DateTimeKind.Utc), "Pedro Oliveira", "Iniciado atendimento" });

            migrationBuilder.InsertData(
                table: "ServiceOrderHistories",
                columns: new[] { "Id", "ServiceOrderId", "Status", "ChangedAt", "ChangedBy", "Notes" },
                values: new object[] { new Guid("40000003-0000-0000-0000-000000000003"), new Guid("30000001-0000-0000-0000-000000000001"), "Concluído", new DateTime(2026, 4, 1, 11, 30, 0, 0, DateTimeKind.Utc), "Pedro Oliveira", "Serviço finalizado com sucesso" });

            migrationBuilder.InsertData(
                table: "ServiceOrderHistories",
                columns: new[] { "Id", "ServiceOrderId", "Status", "ChangedAt", "ChangedBy", "Notes" },
                values: new object[] { new Guid("40000004-0000-0000-0000-000000000004"), new Guid("30000002-0000-0000-0000-000000000002"), "Recebido", new DateTime(2026, 4, 2, 9, 0, 0, 0, DateTimeKind.Utc), "Lucas Pereira", "Ordem de serviço criada" });

            migrationBuilder.InsertData(
                table: "ServiceOrderHistories",
                columns: new[] { "Id", "ServiceOrderId", "Status", "ChangedAt", "ChangedBy", "Notes" },
                values: new object[] { new Guid("40000005-0000-0000-0000-000000000005"), new Guid("30000002-0000-0000-0000-000000000002"), "Em andamento", new DateTime(2026, 4, 2, 9, 30, 0, 0, DateTimeKind.Utc), "Carlos Ferreira", "Iniciado atendimento" });

            migrationBuilder.InsertData(
                table: "ServiceOrderHistories",
                columns: new[] { "Id", "ServiceOrderId", "Status", "ChangedAt", "ChangedBy", "Notes" },
                values: new object[] { new Guid("40000006-0000-0000-0000-000000000006"), new Guid("30000003-0000-0000-0000-000000000003"), "Recebido", new DateTime(2026, 4, 3, 10, 0, 0, 0, DateTimeKind.Utc), "Maria Santos", "Ordem de serviço criada" });

            migrationBuilder.InsertData(
                table: "ServiceOrderHistories",
                columns: new[] { "Id", "ServiceOrderId", "Status", "ChangedAt", "ChangedBy", "Notes" },
                values: new object[] { new Guid("40000007-0000-0000-0000-000000000007"), new Guid("30000003-0000-0000-0000-000000000003"), "Aguardando aprovação", new DateTime(2026, 4, 3, 10, 30, 0, 0, DateTimeKind.Utc), "Pedro Oliveira", "Aguardando aprovação do orçamento" });

            migrationBuilder.InsertData(
                table: "ServiceOrderHistories",
                columns: new[] { "Id", "ServiceOrderId", "Status", "ChangedAt", "ChangedBy", "Notes" },
                values: new object[] { new Guid("40000008-0000-0000-0000-000000000008"), new Guid("30000004-0000-0000-0000-000000000004"), "Recebido", new DateTime(2026, 4, 4, 7, 30, 0, 0, DateTimeKind.Utc), "Lucas Pereira", "Ordem de serviço criada" });

            migrationBuilder.InsertData(
                table: "ServiceOrderHistories",
                columns: new[] { "Id", "ServiceOrderId", "Status", "ChangedAt", "ChangedBy", "Notes" },
                values: new object[] { new Guid("40000009-0000-0000-0000-000000000009"), new Guid("30000004-0000-0000-0000-000000000004"), "Na oficina", new DateTime(2026, 4, 4, 8, 0, 0, 0, DateTimeKind.Utc), "Juliana Lima", "Veículo na oficina" });

            migrationBuilder.InsertData(
                table: "ServiceOrderHistories",
                columns: new[] { "Id", "ServiceOrderId", "Status", "ChangedAt", "ChangedBy", "Notes" },
                values: new object[] { new Guid("40000010-0000-0000-0000-000000000010"), new Guid("30000005-0000-0000-0000-000000000005"), "Recebido", new DateTime(2026, 4, 5, 8, 30, 0, 0, 0, DateTimeKind.Utc), "Maria Santos", "Ordem de serviço criada" });

            // Seed 10 Vehicle Checklists
            migrationBuilder.InsertData(
                table: "VehicleCheckLists",
                columns: new[] { "Id", "VehicleId", "ServiceOrderId", "Mileage", "FuelLevel", "TireCondition", "CoolingLevel", "OilLevel", "TirePressure", "SpareTire", "Rims", "Headlights", "Taillights", "Mirrors", "Windows", "Windshield", "Wipers", "Seats", "Dashboard", "Multimedia", "AirConditioning", "Jack", "Triangle", "SpareKey", "Documents", "GeneralState", "Observations", "VisualDamage", "Photos", "CheckedAt", "ResponsibleUser", "GpsLocation" },
                values: new object[] { new Guid("50000001-0000-0000-0000-000000000001"), new Guid("20000001-0000-0000-0000-000000000001"), new Guid("30000001-0000-0000-0000-000000000001"), 45000, "3/4", "Bom", "Normal", "Normal", "32 psi", true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, "Bom estado geral", "Veículo bem conservado", "Sem danos visíveis", null, new DateTime(2026, 4, 1, 8, 15, 0, 0, DateTimeKind.Utc), "Pedro Oliveira", "-23.550520, -46.633308" });

            migrationBuilder.InsertData(
                table: "VehicleCheckLists",
                columns: new[] { "Id", "VehicleId", "ServiceOrderId", "Mileage", "FuelLevel", "TireCondition", "CoolingLevel", "OilLevel", "TirePressure", "SpareTire", "Rims", "Headlights", "Taillights", "Mirrors", "Windows", "Windshield", "Wipers", "Seats", "Dashboard", "Multimedia", "AirConditioning", "Jack", "Triangle", "SpareKey", "Documents", "GeneralState", "Observations", "VisualDamage", "Photos", "CheckedAt", "ResponsibleUser", "GpsLocation" },
                values: new object[] { new Guid("50000002-0000-0000-0000-000000000002"), new Guid("20000002-0000-0000-0000-000000000002"), new Guid("30000002-0000-0000-0000-000000000002"), 28000, "1/2", "Regular", "Baixo", "Baixo", "28 psi", true, true, true, true, true, true, false, true, true, true, true, false, true, true, true, true, "Necessita reparos", "Pneus carecas, nível de água baixo", "Arranhão na porta esquerda", null, new DateTime(2026, 4, 2, 9, 15, 0, 0, DateTimeKind.Utc), "Carlos Ferreira", "-23.561430, -46.655980" });

            migrationBuilder.InsertData(
                table: "VehicleCheckLists",
                columns: new[] { "Id", "VehicleId", "ServiceOrderId", "Mileage", "FuelLevel", "TireCondition", "CoolingLevel", "OilLevel", "TirePressure", "SpareTire", "Rims", "Headlights", "Taillights", "Mirrors", "Windows", "Windshield", "Wipers", "Seats", "Dashboard", "Multimedia", "AirConditioning", "Jack", "Triangle", "SpareKey", "Documents", "GeneralState", "Observations", "VisualDamage", "Photos", "CheckedAt", "ResponsibleUser", "GpsLocation" },
                values: new object[] { new Guid("50000003-0000-0000-0000-000000000003"), new Guid("20000003-0000-0000-0000-000000000003"), new Guid("30000003-0000-0000-0000-000000000003"), 35000, "Cheio", "Bom", "Normal", "Normal", "35 psi", true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, "Bom estado", "Cliente informou problema apenas no ar", "Sem danos", null, new DateTime(2026, 4, 3, 10, 15, 0, 0, DateTimeKind.Utc), "Pedro Oliveira", "-23.587400, -46.657200" });

            migrationBuilder.InsertData(
                table: "VehicleCheckLists",
                columns: new[] { "Id", "VehicleId", "ServiceOrderId", "Mileage", "FuelLevel", "TireCondition", "CoolingLevel", "OilLevel", "TirePressure", "SpareTire", "Rims", "Headlights", "Taillights", "Mirrors", "Windows", "Windshield", "Wipers", "Seats", "Dashboard", "Multimedia", "AirConditioning", "Jack", "Triangle", "SpareKey", "Documents", "GeneralState", "Observations", "VisualDamage", "Photos", "CheckedAt", "ResponsibleUser", "GpsLocation" },
                values: new object[] { new Guid("50000004-0000-0000-0000-000000000004"), new Guid("20000004-0000-0000-0000-000000000004"), new Guid("30000004-0000-0000-0000-000000000004"), 15000, "3/4", "Bom", "Normal", "Normal", "33 psi", true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, "Excelente estado", "Veículo novo, apenas revisão", "Sem danos", null, new DateTime(2026, 4, 4, 7, 45, 0, 0, DateTimeKind.Utc), "Juliana Lima", "-23.593400, -46.689400" });

            migrationBuilder.InsertData(
                table: "VehicleCheckLists",
                columns: new[] { "Id", "VehicleId", "ServiceOrderId", "Mileage", "FuelLevel", "TireCondition", "CoolingLevel", "OilLevel", "TirePressure", "SpareTire", "Rims", "Headlights", "Taillights", "Mirrors", "Windows", "Windshield", "Wipers", "Seats", "Dashboard", "Multimedia", "AirConditioning", "Jack", "Triangle", "SpareKey", "Documents", "GeneralState", "Observations", "VisualDamage", "Photos", "CheckedAt", "ResponsibleUser", "GpsLocation" },
                values: new object[] { new Guid("50000005-0000-0000-0000-000000000005"), new Guid("20000005-0000-0000-0000-000000000005"), new Guid("30000005-0000-0000-0000-000000000005"), 22000, "1/4", "Ruim", "Normal", "Baixo", "25 psi", false, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, "Ruim estado", "Pneus necessitam troca urgente", "Amassado no para-choque dianteiro", null, new DateTime(2026, 4, 5, 8, 45, 0, 0, DateTimeKind.Utc), "Carlos Ferreira", "-23.548900, -46.638800" });

            migrationBuilder.InsertData(
                table: "VehicleCheckLists",
                columns: new[] { "Id", "VehicleId", "ServiceOrderId", "Mileage", "FuelLevel", "TireCondition", "CoolingLevel", "OilLevel", "TirePressure", "SpareTire", "Rims", "Headlights", "Taillights", "Mirrors", "Windows", "Windshield", "Wipers", "Seats", "Dashboard", "Multimedia", "AirConditioning", "Jack", "Triangle", "SpareKey", "Documents", "GeneralState", "Observations", "VisualDamage", "Photos", "CheckedAt", "ResponsibleUser", "GpsLocation" },
                values: new object[] { new Guid("50000006-0000-0000-0000-000000000006"), new Guid("20000006-0000-0000-0000-000000000006"), new Guid("30000006-0000-0000-0000-000000000006"), 12000, "Cheio", "Bom", "Normal", "Normal", "34 psi", true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, "Bom estado", "Veículo bem cuidado", "Sem danos", null, new DateTime(2026, 4, 6, 9, 15, 0, 0, DateTimeKind.Utc), "Pedro Oliveira", "-23.564200, -46.653400" });

            migrationBuilder.InsertData(
                table: "VehicleCheckLists",
                columns: new[] { "Id", "VehicleId", "ServiceOrderId", "Mileage", "FuelLevel", "TireCondition", "CoolingLevel", "OilLevel", "TirePressure", "SpareTire", "Rims", "Headlights", "Taillights", "Mirrors", "Windows", "Windshield", "Wipers", "Seats", "Dashboard", "Multimedia", "AirConditioning", "Jack", "Triangle", "SpareKey", "Documents", "GeneralState", "Observations", "VisualDamage", "Photos", "CheckedAt", "ResponsibleUser", "GpsLocation" },
                values: new object[] { new Guid("50000007-0000-0000-0000-000000000007"), new Guid("20000007-0000-0000-0000-000000000007"), new Guid("30000007-0000-0000-0000-000000000007"), 38000, "1/2", "Regular", "Baixo", "Baixo", "29 psi", true, true, true, true, true, true, false, true, true, true, true, false, true, true, true, true, "Regular", "Necessita revisão geral", "Risco no vidro traseiro", null, new DateTime(2026, 4, 7, 11, 15, 0, 0, DateTimeKind.Utc), "Juliana Lima", "-23.527600, -46.668100" });

            migrationBuilder.InsertData(
                table: "VehicleCheckLists",
                columns: new[] { "Id", "VehicleId", "ServiceOrderId", "Mileage", "FuelLevel", "TireCondition", "CoolingLevel", "OilLevel", "TirePressure", "SpareTire", "Rims", "Headlights", "Taillights", "Mirrors", "Windows", "Windshield", "Wipers", "Seats", "Dashboard", "Multimedia", "AirConditioning", "Jack", "Triangle", "SpareKey", "Documents", "GeneralState", "Observations", "VisualDamage", "Photos", "CheckedAt", "ResponsibleUser", "GpsLocation" },
                values: new object[] { new Guid("50000008-0000-0000-0000-000000000008"), new Guid("20000008-0000-0000-0000-000000000008"), new Guid("30000008-0000-0000-0000-000000000008"), 25000, "3/4", "Bom", "Normal", "Normal", "31 psi", true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, "Bom estado", "Apenas alinhamento", "Sem danos", null, new DateTime(2026, 4, 8, 13, 15, 0, 0, DateTimeKind.Utc), "Carlos Ferreira", "-23.615600, -46.690700" });

            migrationBuilder.InsertData(
                table: "VehicleCheckLists",
                columns: new[] { "Id", "VehicleId", "ServiceOrderId", "Mileage", "FuelLevel", "TireCondition", "CoolingLevel", "OilLevel", "TirePressure", "SpareTire", "Rims", "Headlights", "Taillights", "Mirrors", "Windows", "Windshield", "Wipers", "Seats", "Dashboard", "Multimedia", "AirConditioning", "Jack", "Triangle", "SpareKey", "Documents", "GeneralState", "Observations", "VisualDamage", "Photos", "CheckedAt", "ResponsibleUser", "GpsLocation" },
                values: new object[] { new Guid("50000009-0000-0000-0000-000000000009"), new Guid("20000009-0000-0000-0000-000000000009"), new Guid("30000009-0000-0000-0000-000000000009"), 18000, "Cheio", "Bom", "Normal", "Normal", "33 psi", true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, "Bom estado", "Veículo para instalação de som", "Sem danos", null, new DateTime(2026, 4, 9, 14, 15, 0, 0, DateTimeKind.Utc), "Juliana Lima", "-23.588900, -46.658600" });

            migrationBuilder.InsertData(
                table: "VehicleCheckLists",
                columns: new[] { "Id", "VehicleId", "ServiceOrderId", "Mileage", "FuelLevel", "TireCondition", "CoolingLevel", "OilLevel", "TirePressure", "SpareTire", "Rims", "Headlights", "Taillights", "Mirrors", "Windows", "Windshield", "Wipers", "Seats", "Dashboard", "Multimedia", "AirConditioning", "Jack", "Triangle", "SpareKey", "Documents", "GeneralState", "Observations", "VisualDamage", "Photos", "CheckedAt", "ResponsibleUser", "GpsLocation" },
                values: new object[] { new Guid("50000010-0000-0000-0000-000000000010"), new Guid("20000010-0000-0000-0000-000000000010"), new Guid("30000010-0000-0000-0000-000000000010"), 8000, "Cheio", "Bom", "Normal", "Normal", "35 psi", true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, "Excelente estado", "Veículo zero km, apenas revisão", "Sem danos", null, new DateTime(2026, 4, 10, 8, 15, 0, 0, DateTimeKind.Utc), "Pedro Oliveira", "-23.549900, -46.634900" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ServiceOrderHistories",
                keyColumn: "Id",
                keyValues: new object[] { new Guid("40000001-0000-0000-0000-000000000001"), new Guid("40000002-0000-0000-0000-000000000002"), new Guid("40000003-0000-0000-0000-000000000003"), new Guid("40000004-0000-0000-0000-000000000004"), new Guid("40000005-0000-0000-0000-000000000005"), new Guid("40000006-0000-0000-0000-000000000006"), new Guid("40000007-0000-0000-0000-000000000007"), new Guid("40000008-0000-0000-0000-000000000008"), new Guid("40000009-0000-0000-0000-000000000009"), new Guid("40000010-0000-0000-0000-000000000010") });

            migrationBuilder.DeleteData(
                table: "VehicleCheckLists",
                keyColumn: "Id",
                keyValues: new object[] { new Guid("50000001-0000-0000-0000-000000000001"), new Guid("50000002-0000-0000-0000-000000000002"), new Guid("50000003-0000-0000-0000-000000000003"), new Guid("50000004-0000-0000-0000-000000000004"), new Guid("50000005-0000-0000-0000-000000000005"), new Guid("50000006-0000-0000-0000-000000000006"), new Guid("50000007-0000-0000-0000-000000000007"), new Guid("50000008-0000-0000-0000-000000000008"), new Guid("50000009-0000-0000-0000-000000000009"), new Guid("50000010-0000-0000-0000-000000000010") });

            migrationBuilder.DeleteData(
                table: "ServiceOrders",
                keyColumn: "Id",
                keyValues: new object[] { new Guid("30000001-0000-0000-0000-000000000001"), new Guid("30000002-0000-0000-0000-000000000002"), new Guid("30000003-0000-0000-0000-000000000003"), new Guid("30000004-0000-0000-0000-000000000004"), new Guid("30000005-0000-0000-0000-000000000005"), new Guid("30000006-0000-0000-0000-000000000006"), new Guid("30000007-0000-0000-0000-000000000007"), new Guid("30000008-0000-0000-0000-000000000008"), new Guid("30000009-0000-0000-0000-000000000009"), new Guid("30000010-0000-0000-0000-000000000010") });

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValues: new object[] { new Guid("20000001-0000-0000-0000-000000000001"), new Guid("20000002-0000-0000-0000-000000000002"), new Guid("20000003-0000-0000-0000-000000000003"), new Guid("20000004-0000-0000-0000-000000000004"), new Guid("20000005-0000-0000-0000-000000000005"), new Guid("20000006-0000-0000-0000-000000000006"), new Guid("20000007-0000-0000-0000-000000000007"), new Guid("20000008-0000-0000-0000-000000000008"), new Guid("20000009-0000-0000-0000-000000000009"), new Guid("20000010-0000-0000-0000-000000000010") });

            migrationBuilder.DeleteData(
                table: "Customers",
                keyColumn: "Id",
                keyValues: new object[] { new Guid("10000001-0000-0000-0000-000000000001"), new Guid("10000002-0000-0000-0000-000000000002"), new Guid("10000003-0000-0000-0000-000000000003"), new Guid("10000004-0000-0000-0000-000000000004"), new Guid("10000005-0000-0000-0000-000000000005"), new Guid("10000006-0000-0000-0000-000000000006"), new Guid("10000007-0000-0000-0000-000000000007"), new Guid("10000008-0000-0000-0000-000000000008"), new Guid("10000009-0000-0000-0000-000000000009"), new Guid("10000010-0000-0000-0000-000000000010") });

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValues: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new Guid("22222222-2222-2222-2222-222222222222"), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("55555555-5555-5555-5555-555555555555"), new Guid("66666666-6666-6666-6666-666666666666"), new Guid("77777777-7777-7777-7777-777777777777"), new Guid("88888888-8888-8888-8888-888888888888"), new Guid("99999999-9999-9999-9999-999999999999"), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") });
        }
    }
}