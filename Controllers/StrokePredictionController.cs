using Microsoft.AspNetCore.Mvc;
using CSProject.Services;
using CSProject.Models;

namespace CSProject.Controllers;

[ApiController]
[Route("api/stroke")]
public class StrokePredictionController : Controller
{
    private readonly StrokePredictionService _strokePredictionService;

    public StrokePredictionController(StrokePredictionService strokePredictionService)
    {
        _strokePredictionService = strokePredictionService;
        _strokePredictionService.LoadModel("MachineLearning/Models/Stroke/stroke_risk_model.zip");
    }

    [HttpPost("predict")]
    public IActionResult Predict(StrokePredictionInput input)
    {
        var predictionResult = _strokePredictionService.Predict(input);
        return Ok(predictionResult);
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok("api está funcionando");
    }
}


