public class Card
{
	public int Id { get; set; }
	public string PlayerName { get; set; } = string.Empty;
	public bool IsOwned { get; set; }
	public List<CardVariant> Variants { get; set; } = new();
}
