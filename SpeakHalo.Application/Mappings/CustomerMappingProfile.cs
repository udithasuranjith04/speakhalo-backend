using AutoMapper;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Domain.Entities;

namespace SpeakHalo.Application.Mappings;

public class CustomerMappingProfile : Profile
{
    public CustomerMappingProfile()
    {
        CreateMap<Customer, CustomerResponse>()
            .ForMember(dest => dest.TotalRevenue, opt => opt.MapFrom(src => src.TotalSpend))
            .ForMember(dest => dest.TotalAppointments, opt => opt.MapFrom(src => src.Appointments.Count));

        CreateMap<CreateCustomerRequest, Customer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Appointments, opt => opt.Ignore());

        CreateMap<UpdateCustomerRequest, Customer>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
