namespace CPIService;

public class CPINote
{
    public int? CPIValue { get; set; }
    public string? StringNotes { get; set; }
    public string? Year { get; set; }
    public string? Month { get; set; }
    public FootNote[]? Notes{ get; set; }
}