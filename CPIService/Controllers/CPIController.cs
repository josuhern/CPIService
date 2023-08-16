using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CPIService.Controllers;


[ApiController]
[Route("[controller]")]
public class CPIController : ControllerBase
{
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpClientFactory _httpClientFactory;

    public CPIController(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
    {
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
    }

    private bool IsValidMonth(string month)
    {
        month = char.ToUpper(month[0]) + month.Substring(1).ToLower();
        return Array.IndexOf(CultureInfo.CurrentCulture.DateTimeFormat.MonthNames, month) >= 0;
    }

    private bool IsValidYear(int year)
    {
        int currentYear = DateTime.Now.Year;
        return year > 1960 && year <= currentYear;
    }

    private string StandardizeString(string? value)
    {
        if (value == null) return "";
        else return Regex.Replace(value ?? "", "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled).ToLower();
    }

    private string GenerateCacheKey(string month, string year)
    {
        return $"{StandardizeString(month)}{StandardizeString(year)}";
    }

    [HttpGet(Name = "GetCPI")]
    public async Task<IActionResult> Get(string month, int year)
    {
        if (!IsValidMonth(month) || !IsValidYear(year))
        {
            return BadRequest("Month or Year are out of range");
        }

        // Standarize the input parameters
        string stdMonth = StandardizeString(month);
        string stdYear = year.ToString();

        // Get uniqueKey for cache
        string cacheKey = GenerateCacheKey(stdMonth, stdYear);


        // Check if request is in cache
        if (!_memoryCache.TryGetValue(cacheKey, out CPINote? cachedCPINote))
        {
            string apiUrl = "https://api.bls.gov/publicAPI/v2/timeseries/data/";
            var httpClient = _httpClientFactory.CreateClient();

            var seriesPost = new SeriesPost
            {
                seriesid = new List<string> { "LAUCN040010000000005" },
                startyear = stdYear,
                endyear = stdYear,
                catalog = true,
                calculations = true,
                annualaverage = true,
            };

            var newJson = JsonSerializer.Serialize(seriesPost);

            var httpContent = new StringContent(newJson);
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            using (var response = await httpClient.PostAsync(apiUrl, httpContent))
            {
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonDocument = JsonDocument.Parse(responseBody);
                    var root = jsonDocument.RootElement;

                    var results = root.GetProperty("Results");
                    var seriesArray = results.GetProperty("series").EnumerateArray();

                    CPINote? CPIResult = null;
                    foreach (var seriesElement in seriesArray)
                    {
                        var data = seriesElement.GetProperty("data").EnumerateArray();

                        foreach (var dataElement in data)
                        {
                            var node = new CPINote
                            {                                
                                Month = dataElement.GetProperty("periodName").GetString(),
                                Year = dataElement.GetProperty("year").GetString()
                            };

                            var CPIValue = dataElement.GetProperty("value").GetString();
                            if (Int32.TryParse(CPIValue, out int j))
                            {
                                node.CPIValue = j;
                            }

                            var nodeNotes = dataElement.GetProperty("footnotes").EnumerateArray();
                            List<FootNote> notes = new List<FootNote>();
                            string notesString = "";
                            foreach (var note in nodeNotes)
                            {
                                var auxCode = note.GetProperty("code").GetString();
                                var auxText = note.GetProperty("text").GetString();
                                notes.Add(new FootNote
                                {
                                    Code = auxCode,
                                    Text = auxText
                                });
                                notesString += auxCode + ": " + auxText +" ";
                            }
                            node.Notes = notes.ToArray();
                            node.StringNotes = notesString;

                            string stdNodeMonth = StandardizeString(node.Month);
                            string newCacheKey = GenerateCacheKey(stdNodeMonth, StandardizeString(node.Year));
                            _memoryCache.Set(newCacheKey, node, TimeSpan.FromDays(1));

                            if (stdNodeMonth.Equals(stdMonth))
                            {
                                CPIResult = node;
                            }
                        }
                    }
                    if (CPIResult != null)
                    {
                        return Ok(new { result = CPIResult, source = "retrieved from API" });
                    }
                    else
                    {
                        return Ok($"No data found for provided month {month} and year {year}");
                    }
                }
                else
                {
                    return StatusCode((int)response.StatusCode, "Failed to retrieve CPI data.");
                }
            }
        }
        else
        {
            return Ok(new { result = cachedCPINote, source = "retrieved from Cache" });
        }
    }
}