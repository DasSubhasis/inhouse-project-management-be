using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using System;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SWCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevlopmentController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString; 

        public DevlopmentController(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
        }

[HttpGet("{projectNo}/serial-numbers")]
public async Task<IActionResult> GetSerialNumbers(int projectNo)
{
    if (projectNo <= 0)
    {
        return BadRequest(new
        {
            success = false,
            message = "Invalid project number"
        });
    }

    try
    {
        using var conn = new SqlConnection(_connectionString);

        var result = await conn.QueryAsync(
            "SP_PreSales_GetSerialNumbersByProjectNo",
            new { ProjectNo = projectNo },
            commandType: CommandType.StoredProcedure
        );

        return Ok(new
        {
            success = true,
            data = result
        });
    }
    catch (SqlException ex) when (ex.Number >= 50000)
    {
        // Business validation errors from THROW
        return UnprocessableEntity(new
        {
            success = false,
            message = ex.Message
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = "Unexpected server error",
            error = ex.Message
        });
    }
}
[HttpGet("getall-confirmed")]
public async Task<IActionResult> GetAllConfirmed()
{
    try
    {
        using var conn = new SqlConnection(_connectionString);

        using var multi = await conn.QueryMultipleAsync(
            "SP_PreSales_GetAll_Confirmed",
            commandType: CommandType.StoredProcedure
        );

        var projects = (await multi.ReadAsync<dynamic>()).ToList();
        var serials = (await multi.ReadAsync<dynamic>()).ToList();

        var result = projects.Select(p => new
        {
            projectNo = p.ProjectNo,
            partyName = p.PartyName,
            projectName = p.ProjectName,
            contactPerson = p.ContactPerson,
            mobileNumber = p.MobileNumber,
            emailId = p.EmailId,
            agentName = p.AgentName,
            projectValue = p.ProjectValue,
            scopeOfDevelopment = p.ScopeOfDevelopment,
            currentStage = p.CurrentStage,

            createdBy = p.CreatedBy,
            createdDate = p.CreatedDate,
            modifiedBy = p.ModifiedBy,
            modifiedDate = p.ModifiedDate,

            latestAttachmentUrl = p.LatestAttachmentUrl,

            serialNumbers = serials
                .Where(s => s.ProjectNo == p.ProjectNo)
                .Select(s => new
                {
                    serialNumber = s.SerialNumber,
                    version = s.Version,
                    recordedById = s.RecordedById,
                    recordedByName = s.RecordedByName,
                    recordedDate = s.RecordedDate
                })
                .ToList()
        });

        return Ok(new
        {
            success = true,
            data = result
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = "Failed to fetch confirmed projects",
            error = ex.Message
        });
    }
}
public class WorkStatusCreateModel
{
    public string? Notes { get; set; }
    public string Status { get; set; } = null!;
    public List<string>? AttachmentUrls { get; set; }
    public Guid CreatedBy { get; set; }
}
[HttpPost("{projectNo}/status")]
public async Task<IActionResult> AddStatusUpdate(
    int projectNo,
    [FromBody] WorkStatusCreateModel model)
{
    if (projectNo <= 0)
    {
        return BadRequest(new
        {
            success = false,
            message = "Invalid project number"
        });
    }

    if (model == null)
    {
        return BadRequest(new
        {
            success = false,
            message = "Request body is required"
        });
    }

    try
    {
        using var conn = new SqlConnection(_connectionString);

        var param = new DynamicParameters();
        param.Add("@ProjectNo", projectNo);
        param.Add("@Notes", model.Notes);
        param.Add("@Status", model.Status);
        param.Add("@AttachmentUrls",
    model.AttachmentUrls == null
        ? null
        : JsonSerializer.Serialize(model.AttachmentUrls));

        param.Add("@CreatedBy", model.CreatedBy);

        var result = await conn.QuerySingleAsync(
            "SP_Work_StatusUpdate_Insert",
            param,
            commandType: CommandType.StoredProcedure
        );

        return Ok(new
        {
            success = true,
            message = "Status updated successfully",
            data = result
        });
    }
    catch (SqlException ex) when (ex.Number >= 50000)
    {
        return UnprocessableEntity(new
        {
            success = false,
            message = ex.Message
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = "Unexpected server error",
            error = ex.Message
        });
    }
}

[HttpGet("work-status/{projectNo}")]
public async Task<IActionResult> GetWorkStatus(int projectNo)
{
    if (projectNo <= 0)
    {
        return BadRequest(new
        {
            success = false,
            message = "Invalid project number"
        });
    }

    try
    {
        using var conn = new SqlConnection(_connectionString);

        using var multi = await conn.QueryMultipleAsync(
            "SP_WorkStatus_GetByProjectNo",
            new { ProjectNo = projectNo },
            commandType: CommandType.StoredProcedure
        );

        // ✅ MUST materialize immediately
        var statuses = (await multi.ReadAsync<dynamic>()).ToList();
        var attachments = (await multi.ReadAsync<dynamic>()).ToList();

        var data = statuses.Select(s => new
        {
            s.StatusUpdateId,
            s.ProjectId,
            s.Notes,
            s.StatusCode,
            s.StatusText,
            s.CreatedDate,
            s.CreatedById,
            s.CreatedByName,

            attachments = attachments
                .Where(a => a.StatusUpdateId == s.StatusUpdateId)
                .Select(a => new
                {
                    a.FileUrl,
                    a.UploadedDate,
                    a.UploadedById,
                    a.UploadedByName
                })
                .ToList()
        }).ToList();

        return Ok(new
        {
            success = true,
            data
        });
    }
    catch (SqlException ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = ex.Message
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = "Unexpected server error",
            error = ex.Message
        });
    }
}



[HttpGet("status-master")]
public async Task<IActionResult> GetAllStatusMaster()
{
    try
    {
        using var conn = new SqlConnection(_connectionString);

        var data = await conn.QueryAsync(
            "SP_Work_StatusMaster_GetAll",
            commandType: CommandType.StoredProcedure
        );

        return Ok(new
        {
            success = true,
            data
        });
    }
    catch (SqlException ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = ex.Message
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = "Unexpected server error",
            error = ex.Message
        });
    }
}

       
    }
}
