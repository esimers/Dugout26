class InsertSet
{
	public string Name { get; set; } = string.Empty;
	public string Category { get; set; } = "Custom";
	public string Code { get; set; } = string.Empty;
	public bool IsOwned { get; set; }
	public List<int> OwnedCards { get; set; } = new();
	public List<int> ValidCardNumbers { get; set; } = new();
	public List<InsertCardInfo> ValidCards { get; set; } = new();
}
