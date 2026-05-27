using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommissionController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public CommissionController(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
        }

        public class CommissionRow
        {
            public Guid? CommissionId { get; set; }
            public Guid LicenseId { get; set; }
            public DateOnly? CommissionDate { get; set; }
            public decimal? Amount { get; set; }
            public DateTime? CreatedDate { get; set; }
            public Guid? CreatedBy { get; set; }
        }

        public class SaveCommissionRequest
        {
            public Guid LicenseId { get; set; }
            public List<CommissionRowInput> Rows { get; set; } = new();
            public Guid? SavedBy { get; set; }
        }

        public class CommissionRowInput
        {
            public string? Date { get; set; }
            public decimal? Amount { get; set; }
        }

        // GET all commission rows for a license
        [HttpGet("getbylicense/{licenseId}")]
        public async Task<IActionResult> GetByLicense(Guid licenseId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);

                var sql = @"SELECT
                            [commission_id] AS CommissionId,
                            [license_id]    AS LicenseId,
                            [commission_date] AS CommissionDate,
                            [amount]        AS Amount,
                            [created_date]  AS CreatedDate,
                            [created_by]    AS CreatedBy
                        FROM [dbo].[tbl_LicenseCommission]
                        WHERE [license_id] = @LicenseId
                        ORDER BY [commission_date] ASC, [created_date] ASC";

                var parameters = new DynamicParameters();
                parameters.Add("@LicenseId", licenseId);

                var result = await conn.QueryAsync<CommissionRow>(sql, parameters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { statusCode = 500, message = "Fetch failed", error = ex.Message });
            }
        }

        // SAVE — replaces all rows for the license in one transaction
        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] SaveCommissionRequest request)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var tx = conn.BeginTransaction();

                // Delete all existing rows for this license
                var deleteSql = "DELETE FROM [dbo].[tbl_LicenseCommission] WHERE [license_id] = @LicenseId";
                await conn.ExecuteAsync(deleteSql, new { LicenseId = request.LicenseId }, tx);

                // Insert new rows (skip completely empty rows)
                if (request.Rows?.Count > 0)
                {
                    var insertSql = @"INSERT INTO [dbo].[tbl_LicenseCommission]
                                        ([license_id], [commission_date], [amount], [created_date], [created_by])
                                     VALUES
                                        (@LicenseId, @CommissionDate, @Amount, GETDATE(), @CreatedBy)";

                    foreach (var row in request.Rows)
                    {
                        if (row.Amount == null && string.IsNullOrWhiteSpace(row.Date)) continue;

                        var p = new DynamicParameters();
                        p.Add("@LicenseId", request.LicenseId);
                        p.Add("@CommissionDate",
                            string.IsNullOrWhiteSpace(row.Date)
                                ? (DateOnly?)null
                                : DateOnly.Parse(row.Date));
                        p.Add("@Amount", row.Amount);
                        p.Add("@CreatedBy", request.SavedBy);

                        await conn.ExecuteAsync(insertSql, p, tx);
                    }
                }

                tx.Commit();
                return Ok(new { statusCode = 200, message = "Commission saved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { statusCode = 500, message = "Save failed", error = ex.Message });
            }
        }
    }
}
