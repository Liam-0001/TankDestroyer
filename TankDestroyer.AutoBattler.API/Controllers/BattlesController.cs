using Microsoft.AspNetCore.Mvc;
using TankDestroyer.AutoBattler.API.Dto;

namespace TankDestroyer.AutoBattler.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BattlesController : Controller
{

    [HttpPost]
    public IActionResult CreateBattle([FromBody] BattleRequestDto request)
    {
        return Ok();
    }
}