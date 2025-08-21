using Microsoft.AspNetCore.Mvc;

namespace BudgetManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public abstract class BaseController : ControllerBase
{
}
