using AutoMapper;
using RenovoWorkshop.Api.DTOs;
using RenovoWorkshop.Domain.Entities;

namespace RenovoWorkshop.Api.Mapping;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // Customer mappings
        CreateMap<Customer, CustomerDto>()
            .ForMember(dest => dest.VehicleCount, opt => opt.MapFrom(src => src.Vehicles.Count))
            .ForMember(dest => dest.ServiceOrderCount, opt => opt.MapFrom(src => src.ServiceOrders.Count));

        CreateMap<CreateCustomerDto, Customer>();
        CreateMap<UpdateCustomerDto, Customer>();

        // Vehicle mappings
        CreateMap<Vehicle, VehicleDto>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name))
            .ForMember(dest => dest.ServiceOrderCount, opt => opt.MapFrom(src => src.ServiceOrders.Count));

        CreateMap<CreateVehicleDto, Vehicle>();
        CreateMap<UpdateVehicleDto, Vehicle>();

        // ServiceOrder mappings
        CreateMap<ServiceOrder, ServiceOrderDto>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name))
            .ForMember(dest => dest.VehiclePlate, opt => opt.MapFrom(src => src.Vehicle.Plate))
            .ForMember(dest => dest.VehicleBrand, opt => opt.MapFrom(src => src.Vehicle.Brand))
            .ForMember(dest => dest.VehicleModel, opt => opt.MapFrom(src => src.Vehicle.Model))
            .ForMember(dest => dest.History, opt => opt.MapFrom(src => src.History));

        CreateMap<ServiceOrderHistory, ServiceOrderHistoryDto>();
        CreateMap<CreateServiceOrderDto, ServiceOrder>();
        CreateMap<CreateServiceOrderWithCustomerVehicleDto, ServiceOrder>()
            .ForMember(dest => dest.CustomerId, opt => opt.Ignore())
            .ForMember(dest => dest.VehicleId, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Number, opt => opt.Ignore())
            .ForMember(dest => dest.EntryDate, opt => opt.Ignore());
        CreateMap<UpdateServiceOrderStatusDto, ServiceOrder>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // InventoryItem mappings
        CreateMap<InventoryItem, InventoryItemDto>();
        CreateMap<CreateInventoryItemDto, InventoryItem>();
        CreateMap<UpdateInventoryItemDto, InventoryItem>();

        // VehicleCheckList mappings
        CreateMap<VehicleCheckList, VehicleCheckListDto>()
            .ForMember(dest => dest.VehiclePlate, opt => opt.MapFrom(src => src.Vehicle.Plate))
            .ForMember(dest => dest.VehicleBrand, opt => opt.MapFrom(src => src.Vehicle.Brand))
            .ForMember(dest => dest.VehicleModel, opt => opt.MapFrom(src => src.Vehicle.Model));

        CreateMap<CreateCheckListDto, VehicleCheckList>();

        // Supplier mappings
        CreateMap<Supplier, SupplierDto>()
            .ForMember(dest => dest.PurchaseOrderCount, opt => opt.MapFrom(src => src.PurchaseOrders.Count));

        CreateMap<CreateSupplierDto, Supplier>();
        CreateMap<UpdateSupplierDto, Supplier>();

        // PurchaseOrder mappings
        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier.Name))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

        CreateMap<PurchaseOrderItem, PurchaseOrderItemDto>()
            .ForMember(dest => dest.ItemCode, opt => opt.MapFrom(src => src.InventoryItem.Code))
            .ForMember(dest => dest.ItemDescription, opt => opt.MapFrom(src => src.InventoryItem.Description))
            .ForMember(dest => dest.TotalValue, opt => opt.MapFrom(src => src.Quantity * src.UnitValue));

        CreateMap<CreatePurchaseOrderItemDto, PurchaseOrderItem>();
        CreateMap<CreatePurchaseOrderDto, PurchaseOrder>();

        // User mappings
        CreateMap<ApplicationUser, UserDto>();
        CreateMap<CreateUserDto, ApplicationUser>();
        CreateMap<UpdateUserDto, ApplicationUser>();
    }
}
