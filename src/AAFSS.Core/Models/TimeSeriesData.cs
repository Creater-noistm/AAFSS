using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAFSS.Core.Models;

/// <summary>
/// Metadata describing a time series dataset stored in HDF5.
/// Contains sample rate, channel information, and the HDF5 path reference.
/// </summary>
public class TimeSeriesData
{
    /// <summary>Unique time series identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent data source identifier.</summary>
    [Required]
    public Guid DataSourceId { get; set; }

    /// <summary>Sample rate in Hz.</summary>
    public double SampleRate { get; set; }

    /// <summary>Number of channels.</summary>
    public int ChannelCount { get; set; }

    /// <summary>Duration of the recording in seconds.</summary>
    public double Duration { get; set; }

    /// <summary>Total number of samples per channel.</summary>
    public long SampleCount { get; set; }

    /// <summary>HDF5 internal path to the dataset (e.g., "/data/channel_1").</summary>
    [Required]
    [MaxLength(512)]
    public string Hdf5Path { get; set; } = string.Empty;

    /// <summary>JSON serialized array of channel names.</summary>
    [MaxLength(4000)]
    public string ChannelNamesJson { get; set; } = "[]";

    /// <summary>JSON serialized array of channel units.</summary>
    [MaxLength(2000)]
    public string ChannelUnitsJson { get; set; } = "[]";

    /// <summary>Physical quantity measured (e.g., "SoundPressure", "Acceleration").</summary>
    [MaxLength(128)]
    public string Quantity { get; set; } = "SoundPressure";

    /// <summary>Navigation property to parent data source.</summary>
    [ForeignKey(nameof(DataSourceId))]
    public DataSource? DataSource { get; set; }

    /// <summary>
    /// Gets or sets the channel names as a string array.
    /// </summary>
    [NotMapped]
    public string[] ChannelNames
    {
        get => System.Text.Json.JsonSerializer.Deserialize<string[]>(ChannelNamesJson) ?? Array.Empty<string>();
        set => ChannelNamesJson = System.Text.Json.JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Gets or sets the channel units as a string array.
    /// </summary>
    [NotMapped]
    public string[] ChannelUnits
    {
        get => System.Text.Json.JsonSerializer.Deserialize<string[]>(ChannelUnitsJson) ?? Array.Empty<string>();
        set => ChannelUnitsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
}
