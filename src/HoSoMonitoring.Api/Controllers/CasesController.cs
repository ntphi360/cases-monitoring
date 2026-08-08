using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;
using HoSoMonitoring.Core.Models;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.SeedWorks;
using HoSoMonitoring.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CasesController : ControllerBase
    {
        private const string AssigneeNotAuthorizedMessage =
            "Cán bộ không được phân quyền xử lý lĩnh vực của thủ tục này";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICaseCodeParser _caseCodeParser;
        private readonly ICaseCodeGenerator _caseCodeGenerator;

        public CasesController(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICaseCodeParser caseCodeParser,
            ICaseCodeGenerator caseCodeGenerator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _caseCodeParser = caseCodeParser;
            _caseCodeGenerator = caseCodeGenerator;
        }

        // GET /api/cases/paging?pageIndex=1&pageSize=10
        // GET /api/cases/paging?keyword=HS&departmentId=1&procedureId=1&status=1&pageIndex=1&pageSize=10
        [HttpGet("paging")]
        public async Task<ActionResult<PageResult<CaseInListDto>>> GetCasesPaging(
            [FromQuery] string? keyword,
            [FromQuery] int? departmentId,
            [FromQuery] int? procedureId,
            [FromQuery] CaseStatus? status,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _unitOfWork.Cases
                .GetAllPagingAsync(
                    keyword,
                    departmentId,
                    procedureId,
                    status,
                    pageIndex,
                    pageSize);

            return Ok(result);
        }

        // GET /api/cases/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CaseDto>> GetCaseById(int id)
        {
            var caseEntity = await _unitOfWork.Cases
                .GetByIdAsync(id);

            if (caseEntity == null)
            {
                return NotFound();
            }

            var result = _mapper.Map<CaseDto>(caseEntity);
            ApplyCaseCodeInfo(result);

            return Ok(result);
        }

        // GET /api/cases/overdue?count=10
        [HttpGet("overdue")]
        public async Task<ActionResult<List<CaseInListDto>>> GetOverdueCases(
            [FromQuery] int count = 10)
        {
            var cases = await _unitOfWork.Cases
                .GetOverdueCasesAsync(count);

            var result = _mapper
                .Map<List<CaseInListDto>>(cases);

            return Ok(result);
        }

        // POST /api/cases
        [HttpPost]
        public async Task<ActionResult<CaseDto>> CreateCase(
            [FromBody] CreateUpdateCaseRequest request)
        {
            if (request.CurrentAssigneeId.HasValue
                && !await _unitOfWork.UserProcedureFields
                    .CanUserHandleProcedureAsync(
                        request.CurrentAssigneeId.Value,
                        request.ProcedureId))
            {
                return BadRequest(AssigneeNotAuthorizedMessage);
            }

            var caseEntity = _mapper
                .Map<CreateUpdateCaseRequest, Case>(request);

            var shouldGenerateCode = string.IsNullOrWhiteSpace(
                request.ExternalCaseCode);
            if (shouldGenerateCode)
            {
                caseEntity.ExternalCaseCode = await _caseCodeGenerator
                    .GenerateAsync();
            }

            caseEntity.CreatedAt = DateTime.Now;

            _unitOfWork.Cases.Add(caseEntity);

            const int maxSaveAttempts = 3;
            for (var attempt = 1; attempt <= maxSaveAttempts; attempt++)
            {
                try
                {
                    await _unitOfWork.CompleteAsync();
                    break;
                }
                catch (DbUpdateException) when (
                    shouldGenerateCode && attempt < maxSaveAttempts)
                {
                    if (!await _unitOfWork.Cases.ExternalCaseCodeExistsAsync(
                            caseEntity.ExternalCaseCode))
                    {
                        throw;
                    }

                    caseEntity.ExternalCaseCode = await _caseCodeGenerator
                        .GenerateAsync();
                }
            }

            var result = _mapper.Map<CaseDto>(caseEntity);
            ApplyCaseCodeInfo(result);

            return CreatedAtAction(
                nameof(GetCaseById),
                new { id = caseEntity.Id },
                result);
        }

        // PUT /api/cases/1
        [HttpPut("{id:int}")]
        public async Task<ActionResult<CaseDto>> UpdateCase(
            int id,
            [FromBody] CreateUpdateCaseRequest request)
        {
            var caseEntity = await _unitOfWork.Cases
                .GetByIdAsync(id);

            if (caseEntity == null)
            {
                return NotFound();
            }

            if (request.CurrentAssigneeId.HasValue
                && !await _unitOfWork.UserProcedureFields
                    .CanUserHandleProcedureAsync(
                        request.CurrentAssigneeId.Value,
                        request.ProcedureId))
            {
                return BadRequest(AssigneeNotAuthorizedMessage);
            }

            var currentExternalCaseCode = caseEntity.ExternalCaseCode;
            _mapper.Map(request, caseEntity);
            if (string.IsNullOrWhiteSpace(caseEntity.ExternalCaseCode))
            {
                caseEntity.ExternalCaseCode = currentExternalCaseCode;
            }

            caseEntity.UpdatedAt = DateTime.Now;

            await _unitOfWork.CompleteAsync();

            var result = _mapper.Map<CaseDto>(caseEntity);
            ApplyCaseCodeInfo(result);

            return Ok(result);
        }

        // DELETE /api/cases/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCase(int id)
        {
            var caseEntity = await _unitOfWork.Cases
                .GetByIdAsync(id);

            if (caseEntity == null)
            {
                return NotFound();
            }

            _unitOfWork.Cases.Remove(caseEntity);

            await _unitOfWork.CompleteAsync();

            return NoContent();
        }

        private void ApplyCaseCodeInfo(CaseDto caseDto)
        {
            var caseCodeInfo = _caseCodeParser.Parse(caseDto.ExternalCaseCode);
            if (!caseCodeInfo.IsValid)
            {
                return;
            }

            caseDto.CityCode = caseCodeInfo.CityCode;
            caseDto.CityName = caseCodeInfo.CityName;
            caseDto.WardCode = caseCodeInfo.WardCode;
            caseDto.WardName = caseCodeInfo.WardName;
            caseDto.CaseCodeDate = caseCodeInfo.ReceivedDate;
            caseDto.DailySequence = caseCodeInfo.DailySequence;
        }
    }
}
