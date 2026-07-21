namespace RenovoWorkshop.Api.DTOs;

public class VehicleDto
{
    public Guid Id { get; set; }
    public string Plate { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public string Fuel { get; set; } = string.Empty;
    public int Mileage { get; set; }
    public string Chassis { get; set; } = string.Empty;
    public string Renavam { get; set; } = string.Empty;
    public string? Photos { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ServiceOrderCount { get; set; }
}

public class CreateVehicleDto
{
    public string Plate { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public string Fuel { get; set; } = string.Empty;
    public int Mileage { get; set; }
    public string Chassis { get; set; } = string.Empty;
    public string Renavam { get; set; } = string.Empty;
    public string? Photos { get; set; }
    public Guid CustomerId { get; set; }
}

public class UpdateVehicleDto
{
    public string Plate { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public string Fuel { get; set; } = string.Empty;
    public int Mileage { get; set; }
    public string Chassis { get; set; } = string.Empty;
    public string Renavam { get; set; } = string.Empty;
    public string? Photos { get; set; }
}