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
    public class UserRoleController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString; 

        public UserRoleController(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
        }


        public class UserRoleViewModel
        {
            public Guid RoleId { get; set; }
            public string? RoleName { get; set; }
            public string? IsActive { get; set; }
        }

        
        public class UserRoleInsertModel
        {
            public string? RoleName { get; set; }
        }

        [HttpPost("insert")]
        public async Task<IActionResult> Insert([FromBody] UserRoleInsertModel model)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@RoleId", Guid.NewGuid());
                parameters.Add("@RoleName", model.RoleName);

                await conn.ExecuteAsync("sp_InsertUserRole", parameters, commandType: CommandType.StoredProcedure);

                return Ok(new { statusCode = 200, message = "Role inserted successfully" });
            }
            catch (Exception ex)
            {
                await LogErrorAsync("Insert", ex.Message);
                return StatusCode(500, new { statusCode = 500, message = "Insert failed", error = ex.Message });
            }
        }

        [HttpGet("getall")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);

                var roles = await conn.QueryAsync<UserRoleViewModel>(
                    "sp_GetAllUserRoles", commandType: CommandType.StoredProcedure);

                return Ok(roles);
            }
            catch (Exception ex)
            {
                await LogErrorAsync("GetAll", ex.Message);
                return StatusCode(500, new { statusCode = 500, message = "Fetch failed", error = ex.Message });
            }
        }

        [HttpGet("getbyid/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@RoleId", id);

                var role = await conn.QueryFirstOrDefaultAsync<UserRoleViewModel>(
                    "sp_GetUserRoleById", parameters, commandType: CommandType.StoredProcedure);

                if (role == null)
                    return NotFound(new { statusCode = 404, message = "Role not found" });

                return Ok(role);
            }
            catch (Exception ex)
            {
                await LogErrorAsync("GetById", ex.Message);
                return StatusCode(500, new { statusCode = 500, message = "Fetch failed", error = ex.Message });
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] UserRoleViewModel model)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@RoleId", model.RoleId);
                parameters.Add("@RoleName", model.RoleName);

                await conn.ExecuteAsync("sp_UpdateUserRole", parameters, commandType: CommandType.StoredProcedure);

                return Ok(new { statusCode = 200, message = "Role updated successfully" });
            }
            catch (Exception ex)
            {
                await LogErrorAsync("Update", ex.Message);
                return StatusCode(500, new { statusCode = 500, message = "Update failed", error = ex.Message });
            }
        }


        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@RoleId", id);

                await conn.ExecuteAsync("sp_DeleteUserRole", parameters, commandType: CommandType.StoredProcedure);

                return Ok(new { statusCode = 200, message = "Role deleted successfully" });
            }
            catch (Exception ex)
            {
                await LogErrorAsync("Delete", ex.Message);
                return StatusCode(500, new { statusCode = 500, message = "Delete failed", error = ex.Message });
            }
        }

        private async Task LogErrorAsync(string action, string message, string source = "SQL")
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var param = new DynamicParameters();
                param.Add("@ControllerName", "UserRoleController");
                param.Add("@ActionName", action);
                param.Add("@ErrorMessage", message);
                param.Add("@ErrorSource", source);
                param.Add("@CreatedAt", DateTime.Now);

                await conn.ExecuteAsync("usp_InsertErrorLog", param, commandType: CommandType.StoredProcedure);
            }
            catch
            {
                
            }
        }
    }
}
