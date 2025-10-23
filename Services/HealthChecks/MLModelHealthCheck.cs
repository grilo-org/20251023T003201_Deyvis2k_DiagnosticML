using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CSProject.Services.HealthChecks
{
    public class MLModelHealthCheck : IHealthCheck
    {
        private readonly ILogger<MLModelHealthCheck> _logger;

        public MLModelHealthCheck(ILogger<MLModelHealthCheck> logger)
        {
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var modelPath = Path.Combine(Directory.GetCurrentDirectory(),
                    "MachineLearning",
                    "Models",
                    "Stroke",
                    "stroke_risk_model.zip");

                if (!File.Exists(modelPath))
                {
                    _logger.LogWarning("Modelo ML não encontrado no caminho: {ModelPath}", modelPath);
                    return Task.FromResult(
                        new HealthCheckResult(
                            context.Registration.FailureStatus,
                            description: "Modelo ML não encontrado no caminho esperado",
                            data: new Dictionary<string, object>
                            {
                                { "modelPath", modelPath },
                                { "lastChecked", DateTime.UtcNow }
                            }));
                }

                var fileInfo = new FileInfo(modelPath);
                var lastModified = fileInfo.LastWriteTimeUtc;
                var fileSizeKb = fileInfo.Length / 1024;

                _logger.LogInformation("Health check do modelo ML bem-sucedido. Tamanho: {SizeKb}KB, Última Modificação: {LastModified}", 
                    fileSizeKb, lastModified);

                return Task.FromResult(
                    HealthCheckResult.Healthy(
                        "Modelo ML disponível e acessível",
                        new Dictionary<string, object>
                        {
                            { "modelPath", modelPath },
                            { "lastModified", lastModified },
                            { "sizeKb", fileSizeKb },
                            { "lastChecked", DateTime.UtcNow }
                        }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante health check do modelo ML");
                return Task.FromResult(
                    new HealthCheckResult(
                        context.Registration.FailureStatus,
                        description: "Erro ao verificar o modelo ML",
                        exception: ex,
                        data: new Dictionary<string, object>
                        {
                            { "errorMessage", ex.Message },
                            { "lastChecked", DateTime.UtcNow }
                        }));
            }
        }
    }
}
