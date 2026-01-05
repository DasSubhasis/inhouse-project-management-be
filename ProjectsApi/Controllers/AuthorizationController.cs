using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace SWCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorizationController : ControllerBase
    {
        private readonly string _connectionString;

        public AuthorizationController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        // ✅ Log error to ErrorLog table
        private async Task LogErrorAsync(string controller, string action, string errorMessage, string errorSource = "Controller")
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@ControllerName", controller);
                parameters.Add("@ActionName", action);
                parameters.Add("@ErrorMessage", errorMessage);
                parameters.Add("@ErrorSource", errorSource);
                parameters.Add("@CreatedAt", DateTime.Now);

                await conn.ExecuteAsync("usp_InsertErrorLog", parameters, commandType: CommandType.StoredProcedure);
            }
            catch
            {
                // Avoid crash during logging failure
            }
        }
        public class AuthorizationInsertResponse
{
    public int Status { get; set; }
    public string? Message { get; set; }
}


        public class AuthorizationModel
        {
            public Guid RoleId { get; set; }
            public Guid MenuId { get; set; }
            public bool CanView { get; set; }
            public bool CanCreate { get; set; }
            public bool CanEdit { get; set; }
            public bool CanDelete { get; set; }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAuthorization([FromBody] AuthorizationModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Validation failed"
                    });
                }

                using var conn = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@RoleId", model.RoleId);
                parameters.Add("@MenuId", model.MenuId);
                parameters.Add("@CanView", model.CanView);
                parameters.Add("@CanCreate", model.CanCreate);
                parameters.Add("@CanEdit", model.CanEdit);
                parameters.Add("@CanDelete", model.CanDelete);

                // 🔥 IMPORTANT: QueryFirstAsync (NOT ExecuteAsync)
                var result = await conn.QueryFirstAsync<AuthorizationInsertResponse>(
                    "usp_Authorization_Insert",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (result.Status == 0)
                {
                    // Duplicate case
                    return Conflict(new
                    {
                        statusCode = 409,
                        message = result.Message
                    });
                }

                return Ok(new
                {
                    statusCode = 201,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                await LogErrorAsync(
                    "AuthorizationController",
                    "CreateAuthorization",
                    ex.Message,
                    "SQL"
                );

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Insert failed",
                    error = ex.Message
                });
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuthorizationById(Guid id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "usp_Authorization_GetById",
                    new { AuthorizationId = id },
                    commandType: CommandType.StoredProcedure);

                if (result == null)
                {
                    return NotFound(new { statusCode = 404, message = "Authorization not found" });
                }

                return Ok(new { statusCode = 200, data = result });
            }
            catch (Exception ex)
            {
                await LogErrorAsync("AuthorizationController", "GetAuthorizationById", ex.Message, "SQL");
                return StatusCode(500, new { statusCode = 500, message = "Fetch failed", error = ex.Message });
            }
        }
        [HttpGet("role/{roleId}")]
public async Task<IActionResult> GetAuthorizationsByRoleId(Guid roleId)
{
    try
    {
        using var conn = new SqlConnection(_connectionString);
        var result = await conn.QueryAsync<dynamic>(
            "usp_Authorization_GetByRoleIdd",
            new { RoleId = roleId },
            commandType: CommandType.StoredProcedure);

        if (result == null || !result.Any())
        {
            return NotFound(new { statusCode = 404, message = "No authorizations found for the given RoleId" });
        }

        return Ok(new { statusCode = 200, data = result });
    }
    catch (Exception ex)
    {
        await LogErrorAsync("AuthorizationController", "GetAuthorizationsByRoleId", ex.Message, "SQL");
        return StatusCode(500, new { statusCode = 500, message = "Fetch failed", error = ex.Message });
    }
}


        [HttpGet("all")]
        public async Task<IActionResult> GetAllAuthorizations()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryAsync<dynamic>(
                    "usp_Authorization_GetAll",
                    commandType: CommandType.StoredProcedure);

                return Ok(new { statusCode = 200, data = result });
            }
            catch (Exception ex)
            {
                await LogErrorAsync("AuthorizationController", "GetAllAuthorizations", ex.Message, "SQL");
                return StatusCode(500, new { statusCode = 500, message = "Fetch failed", error = ex.Message });
            }
        }
[HttpPut("update")]
public async Task<IActionResult> UpdateAuthorization([FromBody] AuthorizationModel model)
{
    using var conn = new SqlConnection(_connectionString);

    var rows = await conn.QueryFirstAsync<int>(
        "usp_Authorization_Update",
        new
        {
            model.RoleId,
            model.MenuId,
            model.CanView,
            model.CanCreate,
            model.CanEdit,
            model.CanDelete
        },
        commandType: CommandType.StoredProcedure);

    if (rows == 0)
        return NotFound(new { message = "Authorization not found" });

    return Ok(new { statusCode = 200, message = "Authorization updated" });
}



        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteAuthorization(Guid id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@AuthorizationId", id);
                parameters.Add("@OutputMessage", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);

                await conn.ExecuteAsync("usp_Authorization_Delete", parameters, commandType: CommandType.StoredProcedure);

                string result = parameters.Get<string>("@OutputMessage");

                if (result == "SUCCESS")
                {
                    return Ok(new { statusCode = 200, message = "Authorization deleted successfully" });
                }
                else
                {
                    await LogErrorAsync("AuthorizationController", "DeleteAuthorization", "SP returned: " + result, "SQL");
                    return StatusCode(500, new { statusCode = 500, message = result });
                }
            }
            catch (Exception ex)
            {
                await LogErrorAsync("AuthorizationController", "DeleteAuthorization", ex.Message, "SQL");
                return StatusCode(500, new { statusCode = 500, message = "Delete failed", error = ex.Message });
            }
        }
    }
}
