class CardVariant
{
	public string Name { get; set; } = string.Empty;
	public bool IsOwned { get; set; }
	public string? Odds { get; set; }
	public string Rarity { get; set; } = "Unknown";
}
