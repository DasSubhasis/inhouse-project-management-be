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
            "SP_PreSales_GetByProjectNo",
            new { ProjectNo = projectNo },
            commandType: CommandType.StoredProcedure
        );

        /* 1️⃣ PROJECT */
        var project = await multi.ReadFirstOrDefaultAsync<dynamic>();
        if (project == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Project not found"
            });
        }

        /* 2️⃣ SCOPE HISTORY */
        var scopes = (await multi.ReadAsync<dynamic>()).ToList();

        /* 3️⃣ ATTACHMENTS BY SCOPE */
        var attachments = (await multi.ReadAsync<dynamic>()).ToList();

        /* 4️⃣ STATUS UPDATES - ONLY statusName and latestFileUrl ⭐ */
        var statusUpdates = (await multi.ReadAsync<dynamic>())
            .Select(s => new
            {
                statusName = s.StatusName,
                latestFileUrl = s.LatestFileUrl
            })
            .ToList();

        /* 5️⃣ STAGE HISTORY (ORIGINAL - NO CHANGES) */
        var stageHistory = (await multi.ReadAsync<dynamic>()).ToList();

        /* 6️⃣ PAYMENTS */
        var advancePayments = (await multi.ReadAsync<dynamic>()).ToList();

        /* 7️⃣ SERIAL NUMBERS */
        var serialNumbers = (await multi.ReadAsync<dynamic>()).ToList();


        /* =====================================================
           ⭐ GROUP ATTACHMENTS UNDER SCOPE (SAFE VERSION)
        ===================================================== */
        var scopeHistory = scopes.Select(s => new
        {
            version = s.version,
            scope = s.Scope,
            modifiedById = s.ModifiedById,
            modifiedByName = s.ModifiedByName,
            modifiedDate = s.ModifiedDate,

            attachments = attachments
                .Where(a => a.ScopeHistoryId?.ToString() == s.ScopeHistoryId?.ToString())
                .Select(a => new
                {
                    fileUrl = a.FileUrl,
                    uploadedById = a.UploadedById,
                    uploadedByName = a.UploadedByName,
                    uploadedDate = a.UploadedDate
                })
                .ToList()
        }).ToList();


        /* =====================================================
           ✅ FINAL RESPONSE
        ===================================================== */
        return Ok(new
        {
            success = true,
            data = new
            {
                project.ProjectNo,
                project.PartyName,
                project.ProjectName,
                project.ContactPerson,
                project.MobileNumber,
                project.EmailId,
                project.AgentName,
                project.ProjectValue,
                project.ScopeOfDevelopment,
                project.CurrentStage,

                project.CreatedById,
                project.CreatedByName,
                project.CreatedDate,
                project.ModifiedById,
                project.ModifiedByName,
                project.ModifiedDate,

                scopeHistory,
                statusUpdates,      // ⭐ NEW: Only statusName + latestFileUrl
                stageHistory,       // ✅ ORIGINAL: Unchanged
                advancePayments,
                serialNumbers
            }
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


        public class FileUploadModel
        {
            public List<IFormFile>? Files { get; set; } // Change to List<IFormFile> to handle multiple files
        }

        public class FileUploadModel1
        {
            public IFormFile? File { get; set; }
        }

        [HttpPost("logo"), DisableRequestSizeLimit]
        public async Task<IActionResult> UploadLogo([FromForm] FileUploadModel1 model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                return BadRequest("Invalid File");
            }

            var folderName = Path.Combine("Docs", "uploads", "AllFiles");
            var pathtoSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
            if (!Directory.Exists(pathtoSave))
            {
                Directory.CreateDirectory(pathtoSave);
            }

            var filename = $"{Path.GetFileNameWithoutExtension(model.File.FileName)}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss_fff}{Path.GetExtension(model.File.FileName)}";
            var fullPath = Path.Combine(pathtoSave, filename);
            var dbPath = Path.Combine(folderName, filename).Replace("\\", "/"); // Replace backslashes with forward slashes

            if (System.IO.File.Exists(fullPath))
            {
                return BadRequest("File already exists");
            }

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            // Get the base URL of your application
            var baseUrl = $"{this.Request.Scheme}://{this.Request.Host}";

            // Generate the full URL path
            var fullUrlPath = $"{baseUrl}/{dbPath}";

            // Automatically fetch filename and content type
            var contentType = model.File.ContentType;

            return Ok(new { fullUrlPath, filename, contentType });
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
    public List<string>? AttachmentUrls { get; set; }

    // Backend / Auth
    public Guid UserId { get; set; }
}
[HttpPost("create")]
public async Task<IActionResult> Create([FromBody] PreSalesCreateModel model)
{
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

        var projectNo = await conn.ExecuteScalarAsync<int>(
            "SP_PreSales_Create",
            param,
            commandType: CommandType.StoredProcedure
        );

        return Ok(new
        {
            success = true,
            message = "Pre-sales project created successfully",
            projectNo
        });
    }
    catch (SqlException ex)
    {
        // 🔹 Business / validation errors from SQL
        if (ex.Number >= 50000)
        {
            return UnprocessableEntity(new
            {
                success = false,
                message = ex.Message
            });
        }

        // 🔹 Actual SQL crash
        return StatusCode(500, new
        {
            success = false,
            message = "Database error occurred",
            error = ex.Message
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

        return Ok(new
        {
            success = true,
            message = "Pre-sales project updated successfully"
        });
    }
    catch (SqlException ex)
    {
        // Business / validation errors from SQL
        if (ex.Number >= 50000)
        {
            return UnprocessableEntity(new
            {
                success = false,
                message = ex.Message
            });
        }

        return StatusCode(500, new
        {
            success = false,
            message = "Database error occurred",
            error = ex.Message
        });
    }
    //
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

[HttpGet("getall")]
public async Task<IActionResult> GetAll()
{
    try
    {
        using var conn = new SqlConnection(_connectionString);

        using var multi = await conn.QueryMultipleAsync(
            "SP_PreSales_GetAll",
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

            /* ⭐ NEW STATUS BLOCK */
           status = p.Status,


            /* 🔹 serial numbers */
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
            message = "Failed to fetch projects",
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

[HttpDelete("delete/{projectNo}")]
public async Task<IActionResult> Delete(int projectNo, [FromQuery] Guid userId)
{
    if (projectNo <= 0)
        return BadRequest(new { success = false, message = "Invalid project number" });

    try
    {
        using var conn = new SqlConnection(_connectionString);

        await conn.ExecuteAsync(
            "SP_PreSales_Delete",
            new { ProjectNo = projectNo, UserId = userId },
            commandType: CommandType.StoredProcedure
        );

        return Ok(new
        {
            success = true,
            message = "Pre-sales project deleted successfully"
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
[HttpPost("{projectNo}/advance-payment")]
public async Task<IActionResult> AddAdvancePayment(
    int projectNo,
    [FromBody] AdvancePaymentModel model)
{
    if (projectNo <= 0)
        return BadRequest(new { success = false, message = "Invalid project number" });

    if (model == null)
        return BadRequest(new { success = false, message = "Request body is required" });

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

        return Ok(new
        {
            success = true,
            message = "Advance payment added successfully"
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
[HttpGet("{projectNo}/advance-payments")]
public async Task<IActionResult> GetAdvancePayments(int projectNo)
{
    if (projectNo <= 0)
        return BadRequest(new { success = false, message = "Invalid project number" });

    try
    {
        using var conn = new SqlConnection(_connectionString);

        var payments = await conn.QueryAsync(
            "SP_PreSales_GetAdvancePaymentsByProjectNo",
            new { ProjectNo = projectNo },
            commandType: CommandType.StoredProcedure);

        return Ok(new
        {
            success = true,
            data = payments
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
[HttpPost("{projectNo}/quotation")]
public async Task<IActionResult> AddQuotation(
    int projectNo,
    [FromBody] PreSalesQuotationCreateModel model)
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
        param.Add("@SerialNumber", model.SerialNumber);
        param.Add("@Version", model.Version);
        param.Add("@RecordedBy", model.RecordedById);
        param.Add("@RecordedDate", model.RecordedDate);

        await conn.ExecuteAsync(
            "SP_PreSales_AddQuotation",
            param,
            commandType: CommandType.StoredProcedure
        );

        return Ok(new
        {
            success = true,
            message = "Serial number added successfully"
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
public class PreSalesQuotationCreateModel
{
    public string? SerialNumber { get; set; } 
    public string? Version { get; set; } 
    public Guid RecordedById { get; set; }
    public string? RecordedDate { get; set; }
}



        #endregion
       
    }
}
