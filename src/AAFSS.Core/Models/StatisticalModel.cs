using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAFSS.Core.Models;

/// <summary>
/// Represents a statistical distribution model fitted to rainflow cycle data.
/// Stores distribution parameters, goodness-of-fit metrics, and fit status.
/// </summary>
public class StatisticalModel
{
    /// <summary>Unique statistical model identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent rainflow result identifier.</summary>
    [Required]
    public Guid RainflowResultId { get; set; }

    /// <summary>Fitted distribution type.</summary>
    public DistributionType DistributionType { get; set; } = DistributionType.Normal;

    /// <summary>JSON serialized distribution parameters (varies by distribution).</summary>
    [MaxLength(4000)]
    public string ParametersJson { get; set; } = "[]";

    /// <summary>Kolmogorov-Smirnov test statistic.</summary>
    public double KsStatistic { get; set; }

    /// <summary>K-S test p-value.</summary>
    public double KsPValue { get; set; }

    /// <summary>Akaike Information Criterion value.</summary>
    public double AicValue { get; set; }

    /// <summary>Goodness of fit metric (0-1, higher is better).</summary>
    public double GoodnessOfFit { get; set; }

    /// <summary>Fit status message.</summary>
    [MaxLength(256)]
    public string FitStatus { get; set; } = "Pending";

    /// <summary>Timestamp when the model was fitted.</summary>
    public DateTime FittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to parent rainflow result.</summary>
    [ForeignKey(nameof(RainflowResultId))]
    public RainflowResult? RainflowResult { get; set; }

    /// <summary>
    /// Gets or sets the distribution parameters as a double array.
    /// </summary>
    [NotMapped]
    public double[] Parameters
    {
        get => System.Text.Json.JsonSerializer.Deserialize<double[]>(ParametersJson) ?? Array.Empty<double>();
        set => ParametersJson = System.Text.Json.JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Returns a human-readable summary of the fit.
    /// </summary>
    public string GetSummary() =>
        $"{DistributionType}: K-S={KsStatistic:F4}, AIC={AicValue:F2}, GoF={GoodnessOfFit:F3}";
}
