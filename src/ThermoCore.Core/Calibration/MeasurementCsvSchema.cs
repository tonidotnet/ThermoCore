namespace ThermoCore.Core.Calibration;

/// <summary>
/// Long-format measurement CSV schema (CAL-002), compatible with DOC-029 <c>series-long.csv</c>.
/// </summary>
public static class MeasurementCsvSchema
{
    public const string TimestampUtc = "timestamp_utc";

    public const string ChannelId = "channel_id";

    public const string Value = "value";

    public const string Unit = "unit";

    public const string HeaderLine = "timestamp_utc,channel_id,value,unit";
}
