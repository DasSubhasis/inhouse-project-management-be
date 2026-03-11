using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using System.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LicenseController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public LicenseController(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
        }

        // Model for Insert
        public class LicenseInsertModel
        {
            public Guid? ProjectId { get; set; }
            public string? ProjectName { get; set; }
            public string? TallySerial { get; set; }
            public DateOnly? InitiationDate { get; set; }
            public DateOnly? ExpiryDate { get; set; }
            public Guid? CreatedBy { get; set; }
        }

        // Model for Get/Update operations
        public class LicenseModel
        {
            public Guid LicenseId { get; set; }
            public Guid? ProjectId { get; set; }
            public string? ProjectName { get; set; }
            public string? TallySerial { get; set; }
            public DateOnly? InitiationDate { get; set; }
            public DateOnly? ExpiryDate { get; set; }
            public DateTime? CreatedDate { get; set; }
            public Guid? CreatedBy { get; set; }
            public DateTime? ModifiedDate { get; set; }
            public Guid? ModifiedBy { get; set; }
            public DateTime? DeleteDate { get; set; }
            public Guid? DeletedBy { get; set; }
        }

        // Model for Validation Response
        public class LicenseValidationResponse
        {
            public string? Status { get; set; } // 'Y' or 'N'
            public string? Message { get; set; }
        }

        // Insert License
        [HttpPost("insert")]
        public async Task<IActionResult> Insert([FromBody] LicenseInsertModel model)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);

                var sql = @"INSERT INTO [dbo].[tbl_License]
                            ([project_id], [project_name], [tally_serial], 
                             [initiation_date], [expiry_date], [created_date], [created_by])
                            VALUES
                            (@ProjectId, @ProjectName, @TallySerial, 
                             @InitiationDate, @ExpiryDate, GETDATE(), @CreatedBy)";

                var parameters = new DynamicParameters();
                parameters.Add("@ProjectId", model.ProjectId);
                parameters.Add("@ProjectName", model.ProjectName);
                parameters.Add("@TallySerial", model.TallySerial);
                parameters.Add("@InitiationDate", model.InitiationDate);
                parameters.Add("@ExpiryDate", model.ExpiryDate);
                parameters.Add("@CreatedBy", model.CreatedBy);

                await conn.ExecuteAsync(sql, parameters);

                return Ok(new { statusCode = 200, message = "License inserted successfully" });
            }
            catch (Exception ex)
            {
                await LogErrorAsync("Insert", ex.Message);
                return StatusCode(500, new { statusCode = 500, message = "Insert failed", error = ex.Message });
            }
        }

        // Get All Licenses
        [HttpGet("getall")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);

                var sql = @"SELECT 
                            [license_id] AS LicenseId,
                            [project_id] AS ProjectId,
                            [project_name] AS ProjectName,
                            [tally_serial] AS TallySerial,
                            [initiation_date] AS InitiationDate,
                            [expiry_date] AS ExpiryDate,
                            [created_date] AS CreatedDate,
                            [created_by] AS CreatedBy,
                            [modified_date] AS ModifiedDate,
                            [modified_by] AS ModifiedBy,
                            [delete_date] AS DeleteDate,
                            [deleted_by] AS DeletedBy
                        FROM [dbo].[tbl_License]
                        WHERE [delete_date] IS NULL
                        ORDER BY [created_date] DESC";

                var result = await conn.QueryAsync<LicenseModel>(sql);

                return Ok(result);
            }
            catch (Exception ex)
            {
                await LogErrorAsync("GetAll", ex.Message);
                return StatusCode(500, new { statusCode = 500, message = "Fetch failed", error = ex.Message });
            }
        }

        // Get License By Id
        [HttpGet("getbyid/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);

                var sql = @"SELECT 
                            [license_id] AS LicenseId,
                            [project_id] AS ProjectId,
                            [project_name] AS ProjectName,
                            [tally_serial] AS TallySerial,
                            [initiation_date] AS InitiationDate,
                            [expiry_date] AS ExpiryDate,
                            [created_date] AS CreatedDate,
                            [created_by] AS CreatedBy,
                            [modified_date] AS ModifiedDate,
                            [modified_by] AS ModifiedBy,
                            [delete_date] AS DeleteDate,
                            [deleted_by] AS DeletedBy
                        FROM [dbo].[tbl_License]
                        WHERE [license_id] = @LicenseId
                        AND [delete_date] IS NULL";

                var parameters = new DynamicParameters();
                parameters.Add("@LicenseId", id);

                var result = await conn.QueryFirstOrDefaultAsync<LicenseModel>(sql, parameters);

                if (result == null)
                    return NotFound(new { statusCode = 404, message = "License not found" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                await LogErrorAsync("GetById", ex.Message);
                return StatusCode(500, new { statusCode = 500, message = "Fetch failed", error = ex.Message });
            }
        }

        // Update License
        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] LicenseModel model)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);

                var sql = @"UPDATE [dbo].[tbl_License]
                            SET 
                                [project_id] = @ProjectId,
                                [project_name] = @ProjectName,
                                [tally_serial] = @TallySerial,
                                [initiation_date] = @InitiationDate,
                                [expiry_date] = @ExpiryDate,
                                [modified_date] = GETDATE(),
                                [modified_by] = @ModifiedBy
                            WHERE [license_id] = @LicenseId
                            AND [delete_date] IS NULL";

                var parameters = new DynamicParameters();
                parameters.Add("@LicenseId", model.LicenseId);
                parameters.Add("@ProjectId", model.ProjectId);
                parameters.Add("@ProjectName", model.ProjectName);
                parameters.Add("@TallySerial", model.TallySerial);
                parameters.Add("@InitiationDate", model.InitiationDate);
                parameters.Add("@ExpiryDate", model.ExpiryDate);
                parameters.Add("@ModifiedBy", model.ModifiedBy);

                await conn.ExecuteAsync(sql, parameters);

                return Ok(new { statusCode = 200, message = "License updated successfully" });
            }
            catch (Exception ex)
            {
                await LogErrorAsync("Update", ex.Message);
                return StatusCode(500, new { statusCode = 500, message = "Update failed", error = ex.Message });
            }
        }

        // Delete License
        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, [FromBody] Guid? deletedBy)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);

                var sql = @"UPDATE [dbo].[tbl_License]
                            SET 
                                [delete_date] = GETDATE(),
                                [deleted_by] = @DeletedBy
                            WHERE [license_id] = @LicenseId";

                var parameters = new DynamicParameters();
                parameters.Add("@LicenseId", id);
                parameters.Add("@DeletedBy", deletedBy);

                await conn.ExecuteAsync(sql, parameters);

                return Ok(new { statusCode = 200, message = "License deleted successfully" });
            }
            catch (Exception ex)
            {
                await LogErrorAsync("Delete", ex.Message);
                return StatusCode(500, new { statusCode = 500, message = "Delete failed", error = ex.Message });
            }
        }

        // Validate License by Tally Serial
        [HttpGet("validate")]
        public async Task<IActionResult> ValidateLicense([FromQuery] string tallySerial)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);

                var sql = @"SELECT TOP 1
                            [license_id] AS LicenseId,
                            [project_id] AS ProjectId,
                            [project_name] AS ProjectName,
                            [tally_serial] AS TallySerial,
                            [initiation_date] AS InitiationDate,
                            [expiry_date] AS ExpiryDate,
                            [created_date] AS CreatedDate,
                            [created_by] AS CreatedBy,
                            [modified_date] AS ModifiedDate,
                            [modified_by] AS ModifiedBy,
                            [delete_date] AS DeleteDate,
                            [deleted_by] AS DeletedBy
                        FROM [dbo].[tbl_License]
                        WHERE [tally_serial] = @TallySerial
                        AND [delete_date] IS NULL
                        ORDER BY [created_date] DESC";

                var parameters = new DynamicParameters();
                parameters.Add("@TallySerial", tallySerial);

                var result = await conn.QueryFirstOrDefaultAsync<LicenseModel>(sql, parameters);

                if (result == null)
                {
                    return Ok(new LicenseValidationResponse 
                    { 
                        Status = "N", 
                        Message = "License not found. For more details, please contact Zicorp Solutions Pvt. Ltd. at +91 7278109799." 
                    });
                }

                // Check if license is expired
                if (result.ExpiryDate.HasValue && result.ExpiryDate.Value >= DateOnly.FromDateTime(DateTime.Now))
                {
                    return Ok(new LicenseValidationResponse 
                    { 
                        Status = "Y", 
                        Message = "License is valid" 
                    });
                }
                else
                {
                    return Ok(new LicenseValidationResponse 
                    { 
                        Status = "N", 
                        Message = "The WhatsApp validity has expired. For more details, please contact Zicorp Solutions Pvt. Ltd. at +91 7278109799." 
                    });
                }
            }
            catch (Exception ex)
            {
                await LogErrorAsync("ValidateLicense", ex.Message);
                return StatusCode(500, new { statusCode = 500, message = "Validation failed", error = ex.Message });
            }
        }

        // Error Logging
        private async Task LogErrorAsync(string action, string message, string source = "SQL")
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var param = new DynamicParameters();
                param.Add("@ControllerName", "LicenseController");
                param.Add("@ActionName", action);
                param.Add("@ErrorMessage", message);
                param.Add("@ErrorSource", source);
                param.Add("@CreatedAt", DateTime.Now);

                await conn.ExecuteAsync("usp_InsertErrorLog", param, commandType: CommandType.StoredProcedure);
            }
            catch
            {
                // Silently fail error logging
            }
        }
    }
}
