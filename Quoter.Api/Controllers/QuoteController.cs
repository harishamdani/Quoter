using Microsoft.AspNetCore.Mvc;
using Quoter.Api.Services;

namespace Quoter.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class QuoteController : ControllerBase
{
    private readonly IQuoteService _quoteService;

    public QuoteController(
        IQuoteService quoteService)
    {
        _quoteService = quoteService;
    }
    
    [HttpGet(Name = "GetQuote")]
    public async Task<IActionResult> Get(string code)
    {
        if (string.IsNullOrEmpty(code))
        {

        }

        var result = await _quoteService.GetQuotesAsync(code);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}