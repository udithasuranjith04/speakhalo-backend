using AutoMapper;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Domain.Entities;

#pragma warning disable CS0618

namespace SpeakHalo.Application.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserResponse>()
            .ForMember(dest => dest.Industry, opt => opt.MapFrom(src => src.Industry.ToString()))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => (string?)null))
            .ForMember(dest => dest.Website, opt => opt.MapFrom(src => (string?)null))
            .ForMember(dest => dest.EmailVerified, opt => opt.MapFrom(src => false));

        CreateMap<UpdateUserRequest, User>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}

#pragma warning restore CS0618
