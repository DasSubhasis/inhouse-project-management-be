using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using System.Data;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PreSalesController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public PreSalesController(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
        }

        #region MODELS

        public class ScopeHistoryModel
        {
            public int VersionNo { get; set; }
            public string? Scope { get; set; }
        }

        public class StageHistoryModel
        {
            public string? Stage { get; set; }
        }
 
      
      
       [HttpGet("{projectNo}")]
public async Task<IActionResult> Get(int projectNo)
{
    using var conn = new SqlConnection(_connectionString);
    using var multi = await conn.QueryMultipleAsync(
        "SP_PreSales_GetByProjectNo",
        new { ProjectNo = projectNo },
        commandType: CommandType.StoredProcedure);

    var project = await multi.ReadFirstAsync();
    var scope = await multi.ReadAsync();
    var stage = await multi.ReadAsync();
    var attachmentHistory = await multi.ReadAsync();
    var payments = await multi.ReadAsync();
    var attachments = await multi.ReadAsync<string>();

    return Ok(new
    {
        success = true,
        data = new
        {
            project,
            scopeHistory = scope,
            stageHistory = stage,
            attachmentHistory,
            advancePayments = payments,
            attachmentUrls = attachments
        }
    });
}

public class PreSalesCreateModel
{
    public string PartyName { get; set; } = null!;
    public string ProjectName { get; set; } = null!;
    public string ContactPerson { get; set; } = null!;
    public string MobileNumber { get; set; } = null!;
    public string EmailId { get; set; } = null!;
    public string AgentName { get; set; } = null!;
    public decimal ProjectValue { get; set; }
    public string ScopeOfDevelopment { get; set; } = null!;
    public string CurrentStage { get; set; } = null!;

    // ❌ DO NOT let Dapper auto-map this
    public List<string>? AttachmentUrls { get; set; }

    // Backend / Auth
    public Guid UserId { get; set; }
}
[HttpPost("create")]
public async Task<IActionResult> Create([FromBody] PreSalesCreateModel model)
{
    try
    {
        using var conn = new SqlConnection(_connectionString);

        var param = new DynamicParameters();

        param.Add("@PartyName", model.PartyName);
        param.Add("@ProjectName", model.ProjectName);
        param.Add("@ContactPerson", model.ContactPerson);
        param.Add("@MobileNumber", model.MobileNumber);
        param.Add("@EmailId", model.EmailId);
        param.Add("@AgentName", model.AgentName);
        param.Add("@ProjectValue", model.ProjectValue);
        param.Add("@ScopeOfDevelopment", model.ScopeOfDevelopment);
        param.Add("@CurrentStage", model.CurrentStage);

        // ✅ Serialize list to JSON
        param.Add("@AttachmentUrls",
            model.AttachmentUrls != null && model.AttachmentUrls.Any()
                ? JsonSerializer.Serialize(model.AttachmentUrls)
                : null
        );

        param.Add("@UserId", model.UserId);

        var projectNo = await conn.ExecuteScalarAsync<int>(
            "SP_PreSales_Create",
            param,
            commandType: CommandType.StoredProcedure
        );

        return Ok(new
        {
            success = true,
            projectNo
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = "PreSales creation failed",
            error = ex.Message
        });
    }
}
public class PreSalesUpdateModel
{
    public string PartyName { get; set; } = null!;
    public string ProjectName { get; set; } = null!;
    public string ContactPerson { get; set; } = null!;
    public string MobileNumber { get; set; } = null!;
    public string EmailId { get; set; } = null!;
    public string AgentName { get; set; } = null!;
    public decimal ProjectValue { get; set; }
    public string ScopeOfDevelopment { get; set; } = null!;
    public string CurrentStage { get; set; } = null!;

    public List<string>? AttachmentUrls { get; set; }

    public Guid UserId { get; set; }
}
[HttpPut("update/{projectNo}")]
public async Task<IActionResult> Update(int projectNo, [FromBody] PreSalesUpdateModel model)
{
    try
    {
        using var conn = new SqlConnection(_connectionString);

        var param = new DynamicParameters();

        param.Add("@ProjectNo", projectNo);
        param.Add("@PartyName", model.PartyName);
        param.Add("@ProjectName", model.ProjectName);
        param.Add("@ContactPerson", model.ContactPerson);
        param.Add("@MobileNumber", model.MobileNumber);
        param.Add("@EmailId", model.EmailId);
        param.Add("@AgentName", model.AgentName);
        param.Add("@ProjectValue", model.ProjectValue);
        param.Add("@ScopeOfDevelopment", model.ScopeOfDevelopment);
        param.Add("@CurrentStage", model.CurrentStage);

        param.Add("@AttachmentUrls",
            model.AttachmentUrls != null && model.AttachmentUrls.Any()
                ? JsonSerializer.Serialize(model.AttachmentUrls)
                : null
        );

        param.Add("@UserId", model.UserId);

        await conn.ExecuteAsync(
            "SP_PreSales_Update",
            param,
            commandType: CommandType.StoredProcedure
        );

        return Ok(new { success = true });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            error = ex.Message
        });
    }
}

[HttpGet("getall")]
public async Task<IActionResult> GetAll()
{
    try
    {
        using var conn = new SqlConnection(_connectionString);

        var result = await conn.QueryAsync(
            "SP_PreSales_GetAll",
            commandType: CommandType.StoredProcedure
        );

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
            error = ex.Message
        });
    }
}
public class AdvancePaymentModel
{
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string TallyEntryNumber { get; set; } = null!;
    public Guid UserId { get; set; }
}

[HttpPost("{projectNo}/advance-payment")]
public async Task<IActionResult> AddAdvancePayment(
    int projectNo,
    [FromBody] AdvancePaymentModel model)
{
    try
    {
        using var conn = new SqlConnection(_connectionString);

        var param = new DynamicParameters();
        param.Add("@ProjectNo", projectNo);
        param.Add("@Amount", model.Amount);
        param.Add("@PaymentDate", model.PaymentDate);
        param.Add("@TallyEntryNumber", model.TallyEntryNumber);
        param.Add("@UserId", model.UserId);

        await conn.ExecuteAsync(
            "SP_PreSales_AddAdvancePayment",
            param,
            commandType: CommandType.StoredProcedure
        );

        return Ok(new { success = true });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            error = ex.Message
        });
    }
}
[HttpDelete("delete/{projectNo}")]
public async Task<IActionResult> Delete(int projectNo, [FromQuery] Guid userId)
{
    try
    {
        using var conn = new SqlConnection(_connectionString);

        await conn.ExecuteAsync(
            "SP_PreSales_Delete",
            new { ProjectNo = projectNo, UserId = userId },
            commandType: CommandType.StoredProcedure
        );

        return Ok(new { success = true });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            error = ex.Message
        });
    }
}

        #endregion
       
    }
}
