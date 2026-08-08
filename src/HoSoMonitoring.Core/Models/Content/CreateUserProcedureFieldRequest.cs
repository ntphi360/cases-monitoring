using AutoMapper;
using HoSoMonitoring.Core.Content;
using System.ComponentModel.DataAnnotations;

namespace HoSoMonitoring.Core.Models.Content;

public class CreateUserProcedureFieldRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Range(1, int.MaxValue)]
    public int ProcedureFieldId { get; set; }

    public class CreateUserProcedureFieldMappingProfile : Profile
    {
        public CreateUserProcedureFieldMappingProfile()
        {
            CreateMap<CreateUserProcedureFieldRequest, UserProcedureField>();
        }
    }
}
