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
[HttpGet("getall-confirmed/{userId}")]
public async Task<IActionResult> GetAllConfirmed(Guid userId)
{
    try
    {
        using var conn = new SqlConnection(_connectionString);

        using var multi = await conn.QueryMultipleAsync(
            "SP_PreSales_GetAll_Confirmed",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure
        );

        var projects = (await multi.ReadAsync()).ToList();
        var serials = (await multi.ReadAsync()).ToList();

        var result = projects.Select(p => new
        {
            p.ProjectNo,
            p.PartyName,
            p.ProjectName,
            p.ContactPerson,
            p.MobileNumber,
            p.EmailId,
            p.AgentName,
            p.ProjectValue,
            p.ScopeOfDevelopment,
            p.CurrentStage,
            p.CreatedBy,
            p.CreatedDate,
            p.ModifiedBy,
            p.ModifiedDate,
            p.AssignedTo,
            p.LatestAttachmentUrl,

            serialNumbers = serials
                .Where(s => s.ProjectNo == p.ProjectNo)
                .ToList()
        });

        return Ok(new { success = true, data = result });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = ex.Message
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

[HttpPost("{projectNo}/project-log")]
public async Task<IActionResult> AddLog(int projectNo, [FromBody] Guid assignedBy)
{
    try
    {
        using var conn = new SqlConnection(_connectionString);

        await conn.ExecuteAsync(
            "SP_Work_ProjectLog_Insert",
            new { ProjectNo = projectNo, AssignedBy = assignedBy },
            commandType: CommandType.StoredProcedure);

        return Ok(new
        {
            success = true,
            message = "Log added successfully"
        });
    }
    catch (SqlException ex) when (ex.Number >= 60000)
    {
        return UnprocessableEntity(new { success = false, message = ex.Message });
    }
}



       
    }
}
