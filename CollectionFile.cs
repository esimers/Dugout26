using System.Text.Json.Serialization;

class CollectionFile
{
	[JsonPropertyName("_schemaVersion")]
	public int SchemaVersion { get; set; } = 1;

	[JsonPropertyName("cards")]
	public List<Card> Cards { get; set; } = new();
}
