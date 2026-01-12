using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Dapper;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using SWCAPI.Services;
using System.Data;


namespace SWCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly OtpEmailService _otpEmailService;
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration config, OtpEmailService otpEmailService)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(config), "Connection string 'DefaultConnection' not found.");
            _otpEmailService = otpEmailService;
            _configuration = config;
        }
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

                await conn.ExecuteAsync("usp_InsertErrorLog", parameters, commandType: System.Data.CommandType.StoredProcedure);
            }
            catch
            {
                
            }
        }

        public class EmailRequestModel
        {
            public string? EmailId { get; set; }
        }

        public class OtpVerificationModel
        {
            public string? EmailId { get; set; }
            public Guid LoginCode { get; set; }
            public string? Otp { get; set; }
        }

        [HttpPost("request-otp")]
        public async Task<IActionResult> RequestOtp([FromBody] EmailRequestModel model)
        {
            if (string.IsNullOrWhiteSpace(model.EmailId))
                return BadRequest(new { statusCode = 400, message = "EmailId is required." });

            try
            {
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@EmailId", model.EmailId);

                var result = await conn.QueryFirstOrDefaultAsync(
                    "sp_CheckEmailAndGenerateOtp",
                    parameters,
                    commandType: System.Data.CommandType.StoredProcedure
                );

                if (result?.StatusCode != 1)
                {
                    return NotFound(new { statusCode = 404, message = "Email ID not found or account is inactive" });
                }

                string otp = result.Otp ?? throw new InvalidOperationException("OTP is null");
                await _otpEmailService.SendOtpEmailAsync(model.EmailId!, otp);

                return Ok(new
                {
                    statusCode = 200,
                    message = "OTP has been sent successfully to your registered email ID",
                    loginCode = result.Code
                });
            }
            catch (Exception ex)
            {
                string source = ex.Message.Contains("Sql") ? "SQL" : "Controller";
                await LogErrorAsync("LoginController", "RequestOtp", ex.Message, source);
                return StatusCode(500, new { statusCode = 500, message = "ERROR", error = ex.Message });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerificationModel model)
        {
            if (string.IsNullOrWhiteSpace(model.EmailId) || string.IsNullOrWhiteSpace(model.Otp) || model.LoginCode == Guid.Empty)
                return BadRequest(new { statusCode = 400, message = "Invalid input." });

            try
            {
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@EmailId", model.EmailId);
                parameters.Add("@LoginCode", model.LoginCode);
                parameters.Add("@Otp", model.Otp);
                var user = await conn.QueryFirstOrDefaultAsync(
                    "sp_VerifyOtpAndGetUser",
                    parameters,
                    commandType: System.Data.CommandType.StoredProcedure
                );

                if (user == null)
                {
                    return NotFound(new { statusCode = 404, message = "Invalid OTP or expired." });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Login successful",
                    userId = user.UserId,
                    name = user.UserName,
                    email = user.EmailId,
                    roleid = user.RoleId,
                    role = user.RoleName,
                    token = GenerateJwtToken(user.EmailId),
                    expiryTime = DateTime.Now.AddHours(24)
                });
            }
            catch (Exception ex)
            {
                string source = ex.Message.Contains("Sql") ? "SQL" : "Controller";
                await LogErrorAsync("LoginController", "VerifyOtp", ex.Message, source);
                return StatusCode(500, new { statusCode = 500, message = "Server error", error = ex.Message });
            }
        }
            private string GenerateJwtToken(string email)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer))
            {
                throw new InvalidOperationException("JWT configuration is not properly set up");
            }
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtIssuer,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
    
}
    