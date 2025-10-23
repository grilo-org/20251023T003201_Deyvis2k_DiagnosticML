using Microsoft.ML;
using CSProject.Models;
using Microsoft.Extensions.Logging;

namespace CSProject.Services
{
    public class StrokePredictionService
    {
        private readonly MLContext _mlContext;
        private PredictionEngine<StrokePredictionInput, StrokePredictionResult>? _predictionEngine;
        private ITransformer? _trainedModel;
        private DataViewSchema? _modelSchema;
        private readonly ILogger<StrokePredictionService> _logger;

        public StrokePredictionService(ILogger<StrokePredictionService> logger)
        {
            _mlContext = new MLContext();
            _logger = logger;
        }

        public void Train(string dataPath)
        {
            try 
            {
                _logger.LogInformation("Iniciando treinamento do modelo com dados de {DataPath}", dataPath);

                if (!File.Exists(dataPath))
                {
                    _logger.LogError("Arquivo de dados não encontrado: {DataPath}", dataPath);
                    throw new FileNotFoundException($"Arquivo de dados não encontrado: {dataPath}");
                }
                
                var dataView = _mlContext.Data.LoadFromTextFile<StrokePredictionModel>(
                    path: dataPath,
                    separatorChar: ',',
                    hasHeader: true);

                _logger.LogInformation("Dados carregados com sucesso. Dividindo em conjuntos de treino/teste");
                var splitDataView = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding("gender")
                .Append(_mlContext.Transforms.Concatenate("Features", GetFeatureColumns()))
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                    labelColumnName: "at_risk", 
                    featureColumnName: "Features"));

                _logger.LogInformation("Iniciando treinamento do modelo");
                _trainedModel = pipeline.Fit(splitDataView.TrainSet);
                
                _modelSchema = splitDataView.TrainSet.Schema;
                
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<StrokePredictionInput, StrokePredictionResult>(_trainedModel);
                _logger.LogInformation("Modelo treinado com sucesso");

                _logger.LogInformation("Avaliando modelo em dados de teste");
                var testPredictions = _trainedModel.Transform(splitDataView.TestSet);
                var metrics = _mlContext.BinaryClassification.Evaluate(
                    data: testPredictions,
                    labelColumnName: "at_risk",
                    scoreColumnName: "Score");
                
                _logger.LogInformation("=== Métricas de Avaliação ===");
                _logger.LogInformation("Accuracy: {Accuracy:P2}", metrics.Accuracy);
                _logger.LogInformation("AUC: {AUC:P2}", metrics.AreaUnderRocCurve);
                _logger.LogInformation("F1Score: {F1Score:P2}", metrics.F1Score);
                _logger.LogInformation("Precision: {Precision:P2}", metrics.PositivePrecision);
                _logger.LogInformation("Recall: {Recall:P2}", metrics.PositiveRecall);
                _logger.LogInformation("============================");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante o treinamento do modelo: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public void SaveModel(string modelPath)
        {
            try
            {
                _logger.LogInformation("Salvando modelo em {ModelPath}", modelPath);
                
                if (_trainedModel == null || _modelSchema == null)
                {
                    _logger.LogWarning("Tentativa de salvar um modelo nulo. O modelo precisa ser treinado primeiro.");
                    return;
                }
                
                // Garantir que o diretório existe
                var directory = Path.GetDirectoryName(modelPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                _mlContext.Model.Save(_trainedModel, _modelSchema, modelPath);
                _logger.LogInformation("Modelo salvo com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar o modelo: {ErrorMessage}", ex.Message);
                throw;
            }
        }
        public void LoadModel(string modelPath)
        {
            try
            {
                _logger.LogInformation("Carregando modelo de {ModelPath}", modelPath);
                
                if (!File.Exists(modelPath))
                {
                    _logger.LogError("Arquivo de modelo não encontrado: {ModelPath}", modelPath);
                    throw new FileNotFoundException($"Arquivo de modelo não encontrado: {modelPath}");
                }
                
                _trainedModel = _mlContext.Model.Load(modelPath, out _modelSchema);
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<StrokePredictionInput, StrokePredictionResult>(_trainedModel);
                _logger.LogInformation("Modelo carregado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar o modelo: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public StrokePredictionResult Predict(StrokePredictionInput input)
        {
            if (_predictionEngine == null)
            {
                throw new InvalidOperationException("O modelo não foi carregado. Chame LoadModel antes de fazer previsões.");
            }
            return _predictionEngine.Predict(input);
        }

        private static string[] GetFeatureColumns()
        {
            return new string[]
            {
                "age", "gender", "chest_pain", "shortness_of_breath", "irregular_heartbeat",
                "fatigue_weakness", "dizziness", "swelling_edema", "neck_jaw_pain", "excessive_sweating",
                "persistent_cough", "nausea_vomiting", "high_blood_pressure", "chest_discomfort",
                "cold_hands_feet", "snoring_sleep_apnea", "anxiety_doom"
            };
        }
    }
}

