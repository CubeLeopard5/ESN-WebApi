using AutoMapper;
using Bo.Constants;
using Dto;
using Dto.Attendance;
using Dto.Calendar;
using Dto.Event;
using Dto.EventTemplate;
using Dto.User;
using Bo.Models;

namespace Web.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<UserBo, UserDto>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : null));
            CreateMap<UserDto, UserBo>();

            CreateMap<UserCreateDto, UserBo>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

            CreateMap<UserUpdateDto, UserBo>().ReverseMap();

            CreateMap<PropositionBo, PropositionDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
                .ReverseMap();

            CreateMap<EventBo, EventDto>()
                .ForMember(dest => dest.IsCurrentUserRegistered, opt => opt.Ignore()) // Calculé dans le service
                .ReverseMap();

            CreateMap<CreateEventDto, EventBo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.EventRegistrations, opt => opt.Ignore())
                .ForMember(dest => dest.Calendars, opt => opt.Ignore());

            // EventRegistration mappings
            CreateMap<EventRegistrationBo, EventRegistrationDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));

            // Event with registrations mapping
            CreateMap<EventBo, EventWithRegistrationsDto>()
                .ForMember(dest => dest.Organizer, opt => opt.MapFrom(src => src.User))
                .ForMember(dest => dest.Registrations, opt => opt.MapFrom(src =>
                    src.EventRegistrations.Where(r => r.Status == RegistrationStatus.Registered)))
                .ForMember(dest => dest.TotalRegistered, opt => opt.MapFrom(src =>
                    src.EventRegistrations.Count(r => r.Status == RegistrationStatus.Registered)));

            // Calendar mappings
            CreateMap<CalendarBo, CalendarDto>()
                .ForMember(dest => dest.SubOrganizers, opt => opt.MapFrom(src =>
                    src.CalendarSubOrganizers.Select(cso => cso.User)));

            CreateMap<CalendarCreateDto, CalendarBo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Event, opt => opt.Ignore())
                .ForMember(dest => dest.MainOrganizer, opt => opt.Ignore())
                .ForMember(dest => dest.EventManager, opt => opt.Ignore())
                .ForMember(dest => dest.ResponsableCom, opt => opt.Ignore())
                .ForMember(dest => dest.CalendarSubOrganizers, opt => opt.Ignore());

            CreateMap<CalendarUpdateDto, CalendarBo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Event, opt => opt.Ignore())
                .ForMember(dest => dest.MainOrganizer, opt => opt.Ignore())
                .ForMember(dest => dest.EventManager, opt => opt.Ignore())
                .ForMember(dest => dest.ResponsableCom, opt => opt.Ignore())
                .ForMember(dest => dest.CalendarSubOrganizers, opt => opt.Ignore());

            // EventTemplate mappings
            CreateMap<EventTemplateBo, EventTemplateDto>().ReverseMap();

            CreateMap<CreateEventTemplateDto, EventTemplateBo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // Attendance mappings
            CreateMap<EventRegistrationBo, AttendanceDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
                .ForMember(dest => dest.AttendanceValidatedBy, opt => opt.MapFrom(src => src.AttendanceValidatedBy));
        }
    }
}
