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
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public UserController(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
        }

     
        public class UserInsertModel
        {
            public string? UserName { get; set; }
            public string? EmailId { get; set; }
            public Guid? RoleId { get; set; }
            public Guid? UserCode { get; set; }
            public string? EmployeeType { get; set; }
        }

      
        public class UserModel
        {
            public Guid UserId { get; set; }
            public string? UserName { get; set; }
            public string? EmailId { get; set; }
            public Guid? RoleId { get; set; }
            public Guid? UserCode { get; set; }
            public string? EmployeeType { get; set; }
            public string? RoleName { get; set; }
                 
        }

   
        [HttpPost("insert")]
        public async Task<IActionResult> Insert([FromBody] UserInsertModel model)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@UserName", model.UserName);
                parameters.Add("@EmailId", model.EmailId);
                parameters.Add("@RoleId", model.RoleId);
                parameters.Add("@UserCode", model.UserCode);
                parameters.Add("@EmployeeType", model.EmployeeType);

                await conn.ExecuteAsync("sp_InsertUser", parameters, commandType: CommandType.StoredProcedure);

                return Ok(new { statusCode = 200, message = "User inserted successfully" });
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

                var result = await conn.QueryAsync<UserModel>("sp_GetUsers", commandType: CommandType.StoredProcedure);

                return Ok(result);
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
                parameters.Add("@UserId", id);

                var result = await conn.QueryFirstOrDefaultAsync<UserModel>(
                    "sp_GetUserById", parameters, commandType: CommandType.StoredProcedure);

                if (result == null)
                    return NotFound(new { statusCode = 404, message = "User not found" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                await LogErrorAsync("GetById", ex.Message);
                return StatusCode(500, new { statusCode = 500, message = "Fetch failed", error = ex.Message });
            }
        }

       
        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] UserModel model)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", model.UserId);
                parameters.Add("@UserName", model.UserName);
                parameters.Add("@EmailId", model.EmailId);
                parameters.Add("@RoleId", model.RoleId);
                parameters.Add("@UserCode", model.UserCode);
                parameters.Add("@EmployeeType", model.EmployeeType);
                await conn.ExecuteAsync("sp_UpdateUser", parameters, commandType: CommandType.StoredProcedure);

                return Ok(new { statusCode = 200, message = "User updated successfully" });
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
                parameters.Add("@UserId", id);

                await conn.ExecuteAsync("sp_DeleteUser", parameters, commandType: CommandType.StoredProcedure);

                return Ok(new { statusCode = 200, message = "User deleted successfully" });
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
                param.Add("@ControllerName", "UserController");
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
