namespace RenovoWorkshop.Api.DTOs;

public class VehicleCheckListDto
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public Guid ServiceOrderId { get; set; }
    public int Mileage { get; set; }
    public string FuelLevel { get; set; } = string.Empty;
    public string TireCondition { get; set; } = string.Empty;
    public string CoolingLevel { get; set; } = string.Empty;
    public string OilLevel { get; set; } = string.Empty;
    public string TirePressure { get; set; } = string.Empty;
    public bool SpareTire { get; set; }
    public bool Rims { get; set; }
    public bool Headlights { get; set; }
    public bool Taillights { get; set; }
    public bool Mirrors { get; set; }
    public bool Windows { get; set; }
    public bool Windshield { get; set; }
    public bool Wipers { get; set; }
    public bool Seats { get; set; }
    public bool Dashboard { get; set; }
    public bool Multimedia { get; set; }
    public bool AirConditioning { get; set; }
    public bool Jack { get; set; }
    public bool Triangle { get; set; }
    public bool SpareKey { get; set; }
    public bool Documents { get; set; }
    public string GeneralState { get; set; } = string.Empty;
    public string Observations { get; set; } = string.Empty;
    public string VisualDamage { get; set; } = string.Empty;
    public string? Photos { get; set; }
    public DateTime CheckedAt { get; set; }
    public string ResponsibleUser { get; set; } = string.Empty;
    public string? GpsLocation { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrand { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
}

public class CreateCheckListDto
{
    public Guid VehicleId { get; set; }
    public Guid ServiceOrderId { get; set; }
    public int Mileage { get; set; }
    public string FuelLevel { get; set; } = string.Empty;
    public string TireCondition { get; set; } = string.Empty;
    public string CoolingLevel { get; set; } = string.Empty;
    public string OilLevel { get; set; } = string.Empty;
    public string TirePressure { get; set; } = string.Empty;
    public bool SpareTire { get; set; }
    public bool Rims { get; set; }
    public bool Headlights { get; set; }
    public bool Taillights { get; set; }
    public bool Mirrors { get; set; }
    public bool Windows { get; set; }
    public bool Windshield { get; set; }
    public bool Wipers { get; set; }
    public bool Seats { get; set; }
    public bool Dashboard { get; set; }
    public bool Multimedia { get; set; }
    public bool AirConditioning { get; set; }
    public bool Jack { get; set; }
    public bool Triangle { get; set; }
    public bool SpareKey { get; set; }
    public bool Documents { get; set; }
    public string GeneralState { get; set; } = string.Empty;
    public string Observations { get; set; } = string.Empty;
    public string VisualDamage { get; set; } = string.Empty;
    public string Photos { get; set; } = string.Empty;
    public string ResponsibleUser { get; set; } = string.Empty;
    public string? GpsLocation { get; set; }
}