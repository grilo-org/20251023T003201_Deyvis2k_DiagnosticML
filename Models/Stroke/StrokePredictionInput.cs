using Microsoft.ML.Data;


namespace CSProject.Models;

public class StrokePredictionInput
{
    [LoadColumn(0), ColumnName("age")] public float age { get; set; }
    [LoadColumn(1), ColumnName("gender")] public string gender { get; set; } = null!;
    [LoadColumn(2), ColumnName("chest_pain")] public float chest_pain { get; set; }
    [LoadColumn(3), ColumnName("shortness_of_breath")] public float shortness_of_breath { get; set; }
    [LoadColumn(4), ColumnName("irregular_heartbeat")] public float irregular_heartbeat { get; set; }
    [LoadColumn(5), ColumnName("fatigue_weakness")] public float fatigue_weakness { get; set; }
    [LoadColumn(6), ColumnName("dizziness")] public float dizziness { get; set; }
    [LoadColumn(7), ColumnName("swelling_edema")] public float swelling_edema { get; set; }
    [LoadColumn(8), ColumnName("neck_jaw_pain")] public float neck_jaw_pain { get; set; }
    [LoadColumn(9), ColumnName("excessive_sweating")] public float excessive_sweating { get; set; }
    [LoadColumn(10), ColumnName("persistent_cough")] public float persistent_cough { get; set; }
    [LoadColumn(11), ColumnName("nausea_vomiting")] public float nausea_vomiting { get; set; }
    [LoadColumn(12), ColumnName("high_blood_pressure")] public float high_blood_pressure { get; set; }
    [LoadColumn(13), ColumnName("chest_discomfort")] public float chest_discomfort { get; set; }
    [LoadColumn(14), ColumnName("cold_hands_feet")] public float cold_hands_feet { get; set; }
    [LoadColumn(15), ColumnName("snoring_sleep_apnea")] public float snoring_sleep_apnea { get; set; }
    [LoadColumn(16), ColumnName("anxiety_doom")] public float anxiety_doom { get; set; }
}
