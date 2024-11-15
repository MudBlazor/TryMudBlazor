using Microsoft.AspNetCore.Mvc;
using MudBlazor.Examples.Data;
using MudBlazor.Examples.Data.Models;

namespace TryMudBlazor.Server.Controllers;

[Route("wasm/webapi/[controller]")]
[Route("webapi/[controller]")]
[ApiController]
public class PeriodicTableController : ControllerBase
{
    private readonly IPeriodicTableService _periodicTableService;

    public PeriodicTableController(IPeriodicTableService periodicTableService)
    {
        _periodicTableService = periodicTableService;
    }

    [HttpGet("{search}")]
    public async Task<IEnumerable<Element>> Get(string search)
    {
        return await _periodicTableService.GetElements(search);
    }

    [HttpGet]
    public async Task<IEnumerable<Element>> Get()
    {
        return await _periodicTableService.GetElements();
    }
}