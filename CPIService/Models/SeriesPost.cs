public class SeriesPost
{
    public required List<string> seriesid { get; set; }
    public required string startyear { get; set; }
    public required string endyear { get; set; }
    public bool catalog { get; set; }
    public bool calculations { get; set; }
    public bool annualaverage { get; set; }
}