using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SWCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttacmentController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString; 

        public AttacmentController(IConfiguration config)
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

       
    }
}
