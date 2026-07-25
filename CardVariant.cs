public class CardVariant
{
	public string Name { get; set; } = string.Empty;
	public bool IsOwned { get; set; }
	public string? Odds { get; set; }
	public string Rarity { get; set; } = "Unknown";

	public int? PrintRun { get; set; }
	public string? SerialNum { get; set; }
	public bool IsAuto { get; set; }
	public bool IsRelic { get; set; }

	public bool Is1Of1 => PrintRun == 1 || (SerialNum != null && SerialNum.Trim().Equals("1/1", StringComparison.OrdinalIgnoreCase));
}
