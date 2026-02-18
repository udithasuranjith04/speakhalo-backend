using AutoMapper;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Domain.Entities;

namespace SpeakHalo.Application.Mappings;

public class CallMappingProfile : Profile
{
    public CallMappingProfile()
    {
        CreateMap<Call, CallResponse>()
            .ForMember(dest => dest.FromNumber, opt => opt.MapFrom(src => src.CallerPhone))
            .ForMember(dest => dest.ToNumber, opt => opt.MapFrom(src => src.RecipientPhone))
            .ForMember(dest => dest.TranscriptText, opt => opt.MapFrom(src => src.TranscriptText ?? string.Empty));

        CreateMap<CreateCallRequest, Call>()
            .ForMember(dest => dest.CallerPhone, opt => opt.MapFrom(src => src.FromNumber))
            .ForMember(dest => dest.RecipientPhone, opt => opt.MapFrom(src => src.ToNumber))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateCallRequest, Call>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<TranscriptEntry, TranscriptEntryResponse>();
    }
}
