using AutoMapper;
using Bo.Models;
using Dto;
using Dto.Event;
using Dto.User;

namespace Tests.Mappings
{
    public class TestMappingProfile : Profile
    {
        public TestMappingProfile()
        {
            CreateMap<UserBo, UserDto>().ReverseMap();

            CreateMap<UserCreateDto, UserBo>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

            CreateMap<PropositionBo, PropositionDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
                .ReverseMap();

            CreateMap<EventBo, EventDto>().ReverseMap();
        }
    }
}
