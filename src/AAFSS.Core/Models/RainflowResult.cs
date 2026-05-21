using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAFSS.Core.Models;

/// <summary>
/// Stores the result of ASTM E1049 rainflow cycle counting.
/// Contains from-to matrix, mean-amplitude matrix, and cycle statistics.
/// </summary>
public class RainflowResult
{
    /// <summary>Unique rainflow result identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent data source identifier.</summary>
    [Required]
    public Guid DataSourceId { get; set; }

    /// <summary>JSON serialized from-to matrix (NxN).</summary>
    [MaxLength]
    public string FromToMatrixJson { get; set; } = "[]";

    /// <summary>JSON serialized mean-amplitude matrix (NxN).</summary>
    [MaxLength]
    public string MeanAmplitudeMatrixJson { get; set; } = "[]";

    /// <summary>JSON serialized cycle counts per bin.</summary>
    [MaxLength]
    public string CycleCountsJson { get; set; } = "[]";

    /// <summary>Total number of counted cycles.</summary>
    public int TotalCycles { get; set; }

    /// <summary>Maximum amplitude in the result.</summary>
    public double MaxAmplitude { get; set; }

    /// <summary>Minimum mean value in the result.</summary>
    public double MinMean { get; set; }

    /// <summary>Maximum mean value in the result.</summary>
    public double MaxMean { get; set; }

    /// <summary>Number of bins used in the discretization.</summary>
    public int BinCount { get; set; } = 64;

    /// <summary>Timestamp when rainflow counting was performed.</summary>
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to parent data source.</summary>
    [ForeignKey(nameof(DataSourceId))]
    public DataSource? DataSource { get; set; }

    /// <summary>Associated statistical model fits.</summary>
    public List<StatisticalModel> StatisticalModels { get; set; } = new();

    /// <summary>
    /// Gets or sets the from-to matrix.
    /// </summary>
    [NotMapped]
    public double[,] FromToMatrix
    {
        get => DeserializeMatrix2D(FromToMatrixJson, BinCount, BinCount);
        set => FromToMatrixJson = SerializeMatrix2D(value);
    }

    /// <summary>
    /// Gets or sets the mean-amplitude matrix.
    /// </summary>
    [NotMapped]
    public double[,] MeanAmplitudeMatrix
    {
        get => DeserializeMatrix2D(MeanAmplitudeMatrixJson, BinCount, BinCount);
        set => MeanAmplitudeMatrixJson = SerializeMatrix2D(value);
    }

    /// <summary>
    /// Gets or sets the cycle counts array.
    /// </summary>
    [NotMapped]
    public double[] CycleCounts
    {
        get => System.Text.Json.JsonSerializer.Deserialize<double[]>(CycleCountsJson) ?? Array.Empty<double>();
        set => CycleCountsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }

    private static double[,] DeserializeMatrix2D(string json, int rows, int cols)
    {
        var flat = System.Text.Json.JsonSerializer.Deserialize<double[]>(json) ?? Array.Empty<double>();
        var result = new double[rows, cols];
        for (int i = 0; i < Math.Min(flat.Length, rows * cols); i++)
        {
            result[i / cols, i % cols] = flat[i];
        }
        return result;
    }

    private static string SerializeMatrix2D(double[,] matrix)
    {
        var rows = matrix.GetLength(0);
        var cols = matrix.GetLength(1);
        var flat = new double[rows * cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                flat[i * cols + j] = matrix[i, j];
        return System.Text.Json.JsonSerializer.Serialize(flat);
    }
}
