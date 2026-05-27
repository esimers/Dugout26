using System.Text.Json.Serialization;

class InsertSetFile
{
	[JsonPropertyName("_schemaVersion")]
	public int SchemaVersion { get; set; } = 1;

	[JsonPropertyName("insertSets")]
	public List<InsertSet> InsertSets { get; set; } = new();
}
