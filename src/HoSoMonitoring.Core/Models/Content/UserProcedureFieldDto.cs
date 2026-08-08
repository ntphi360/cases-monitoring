using AutoMapper;
using HoSoMonitoring.Core.Content;

namespace HoSoMonitoring.Core.Models.Content;

public class UserProcedureFieldDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ProcedureFieldId { get; set; }

    public string UserFullName { get; set; } = string.Empty;

    public string ProcedureFieldName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public class UserProcedureFieldMappingProfile : Profile
    {
        public UserProcedureFieldMappingProfile()
        {
            CreateMap<UserProcedureField, UserProcedureFieldDto>()
                .ForMember(
                    destination => destination.UserFullName,
                    options => options.MapFrom(source => source.User!.FullName))
                .ForMember(
                    destination => destination.ProcedureFieldName,
                    options => options.MapFrom(source => source.ProcedureField!.Name));
        }
    }
}
