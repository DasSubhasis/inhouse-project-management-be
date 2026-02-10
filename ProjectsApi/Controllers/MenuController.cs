using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SWCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly string _connectionString;

        public MenuController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
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

                await conn.ExecuteAsync("usp_InsertErrorLog", parameters, commandType: CommandType.StoredProcedure);
            }
            catch
            {
                
            }
        }

        public class MenuCreateModel
        {
            public string? MenuName { get; set; }
            public string? MenuURL { get; set; }
            public string? MenuIcon { get; set; }
            public int Order { get; set; }
            public Guid? MainMenuId { get; set; }
        }

        public class MenuRaw
        {
            public Guid MenuId { get; set; }
            public string? MenuName { get; set; }
            public string? MenuURL { get; set; }
            public string? MenuIcon { get; set; }
            public int Order { get; set; }
            public Guid? MainMenuId { get; set; }
        }

        public class MenuStructured
        {
            public Guid MenuId { get; set; }
            public string? MenuName { get; set; }
            public string? MenuURL { get; set; }
            public string? MenuIcon { get; set; }
            public int Order { get; set; }
            public Guid? MainMenuId { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public List<MenuStructured>? Submenu { get; set; }
        }

       
        [HttpGet("all")]
        public async Task<IActionResult> GetAllMenus()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var rawMenus = (await conn.QueryAsync<MenuRaw>("usp_GetAllMenus", commandType: CommandType.StoredProcedure)).ToList();

              var structuredMenus = rawMenus
    .Where(m => m.MainMenuId == null )
    .OrderBy(m => m.Order)
    .Select(main => new MenuStructured
    {
        MenuId = main.MenuId,
        MenuName = main.MenuName,
        MenuURL = main.MenuURL,
        MenuIcon = main.MenuIcon,
        Order = main.Order,
        MainMenuId = main.MainMenuId,
        Submenu = rawMenus
            .Where(child => child.MainMenuId == main.MenuId)
            .OrderBy(child => child.Order)
            .Select(child => new MenuStructured
            {
                MenuId = child.MenuId,
                MenuName = child.MenuName,
                MenuURL = child.MenuURL,
                MenuIcon = child.MenuIcon,
                Order = child.Order,
                MainMenuId = child.MainMenuId
            })
            .ToList()
    })
    .ToList();

                structuredMenus.ForEach(m => { if (m.Submenu != null && m.Submenu.Count == 0) m.Submenu = null; });
                return Ok(new { statusCode = 200, data = structuredMenus });
            }
            catch (Exception ex)
            {
                await LogErrorAsync("MenuController", "GetAllMenus", ex.Message, "SQL");
                return StatusCode(500, new { statusCode = 500, message = "Server error", error = ex.Message });
            }
        }

[HttpGet("all-by-role/{roleId}")]
public async Task<IActionResult> GetAllMenusByRole(Guid roleId)
{
    try
    {
        using var conn = new SqlConnection(_connectionString);

        var rawMenus = (await conn.QueryAsync<MenuRaw>(
            "usp_GetAllMenus_ByRoleId",
            new { RoleId = roleId },
            commandType: CommandType.StoredProcedure
        )).ToList();

        var structuredMenus = rawMenus
            .Where(m => m.MainMenuId == null)
            .OrderBy(m => m.Order)
            .Select(main => new MenuStructured
            {
                MenuId = main.MenuId,
                MenuName = main.MenuName,
                MenuURL = main.MenuURL,
                MenuIcon = main.MenuIcon,
                Order = main.Order,
                MainMenuId = main.MainMenuId,
                Submenu = rawMenus
                    .Where(child => child.MainMenuId == main.MenuId)
                    .OrderBy(child => child.Order)
                    .Select(child => new MenuStructured
                    {
                        MenuId = child.MenuId,
                        MenuName = child.MenuName,
                        MenuURL = child.MenuURL,
                        MenuIcon = child.MenuIcon,
                        Order = child.Order,
                        MainMenuId = child.MainMenuId
                    })
                    .ToList()
            })
            .ToList();

        structuredMenus.ForEach(m =>
        {
            if (m.Submenu != null && m.Submenu.Count == 0)
                m.Submenu = null;
        });

        return Ok(new { statusCode = 200, data = structuredMenus });
    }
    catch (Exception ex)
    {
        await LogErrorAsync("MenuController", "GetAllMenusByRole", ex.Message, "SQL");
        return StatusCode(500, new
        {
            statusCode = 500,
            message = "Server error",
            error = ex.Message
        });
    }
}

        [HttpPost("create")]
        public async Task<IActionResult> CreateMenu([FromBody] MenuCreateModel model)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@MenuName", model.MenuName);
                parameters.Add("@MenuURL", model.MenuURL);
                parameters.Add("@MenuIcon", model.MenuIcon);
                parameters.Add("@Order", model.Order);
                parameters.Add("@MainMenuId", model.MainMenuId);

                var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "usp_InsertMenu", parameters, commandType: CommandType.StoredProcedure);

                return Ok(new
                {
                    statusCode = result?.StatusCode ?? 500,
                    message = result?.Message ?? "Unknown error"
                });
            }
            catch (Exception ex)
            {
                await LogErrorAsync("MenuController", "CreateMenu", ex.Message, "SQL");
                return StatusCode(500, new { statusCode = 500, message = "Server error", error = ex.Message });
            }
        }

        // ==== GET BY ID ====
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMenuById(Guid id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync(
                    "usp_GetMenuById",
                    new { MenuId = id },
                    commandType: CommandType.StoredProcedure
                );

                return result == null
                    ? NotFound(new { statusCode = 404, message = "Menu not found" })
                    : Ok(new { statusCode = 200, data = result });
            }
            catch (Exception ex)
            {
                await LogErrorAsync("MenuController", "GetMenuById", ex.Message, "SQL");
                return StatusCode(500, new { statusCode = 500, message = "Server error", error = ex.Message });
            }
        }

 
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateMenu(Guid id, [FromBody] MenuCreateModel model)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@MenuId", id);
                parameters.Add("@MenuName", model.MenuName);
                parameters.Add("@MenuURL", model.MenuURL);
                parameters.Add("@MenuIcon", model.MenuIcon);
                parameters.Add("@Order", model.Order);
                parameters.Add("@MainMenuId", model.MainMenuId);
                parameters.Add("@OutputMessage", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);

                await conn.ExecuteAsync("usp_UpdateMenu", parameters, commandType: CommandType.StoredProcedure);
                var result = parameters.Get<string>("@OutputMessage");

                return result == "SUCCESS"
                    ? Ok(new { statusCode = 200, message = "Menu updated successfully" })
                    : StatusCode(500, new { statusCode = 500, message = result });
            }
            catch (Exception ex)
            {
                await LogErrorAsync("MenuController", "UpdateMenu", ex.Message, "SQL");
                return StatusCode(500, new { statusCode = 500, message = "Server error", error = ex.Message });
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteMenu(Guid id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@MenuId", id);
                parameters.Add("@OutputMessage", dbType: DbType.String, direction: ParameterDirection.Output, size: 200);
                await conn.ExecuteAsync("usp_DeleteMenuById", parameters, commandType: CommandType.StoredProcedure);
                var result = parameters.Get<string>("@OutputMessage");

                return result == "SUCCESS"
                    ? Ok(new { statusCode = 200, message = "Menu deleted successfully" })
                    : StatusCode(500, new { statusCode = 500, message = result });
            }
            catch (Exception ex)
            {
                await LogErrorAsync("MenuController", "DeleteMenu", ex.Message, "SQL");
                return StatusCode(500, new { statusCode = 500, message = "Server error", error = ex.Message });
            }
        }

            [HttpGet("only-main-menu")]
        public async Task<IActionResult> GetMainMenus()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var rawMenus = (await conn.QueryAsync<MenuRaw>("usp_GetAllMenus", commandType: CommandType.StoredProcedure)).ToList();

                var mainMenus = rawMenus
                    .Where(m => m.MainMenuId == null)
                    .OrderBy(m => m.Order)
                    .Select(m => new
                    {
                        m.MenuId,
                        m.MenuName,
                        m.MenuURL,
                        m.MenuIcon,
                        m.Order,
                        m.MainMenuId
                    })
                    .ToList();

                return Ok(new { statusCode = 200, data = mainMenus });
            }
            catch (Exception ex)
            {
                await LogErrorAsync("MenuController", "GetMainMenus", ex.Message, "SQL");
                return StatusCode(500, new { statusCode = 500, message = "Server error", error = ex.Message });
            }
        }
    }
}
