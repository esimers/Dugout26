using System.IO;
using System.Text;
using System.Text.Json;

public static class StorageService
{
	public static List<Card> LoadOrCreateCards(string path, JsonSerializerOptions options, string checklistPdfPath)
	{
		if (File.Exists(path))
		{
			var json = File.ReadAllText(path);
			if (json.TrimStart().StartsWith('{'))
			{
				var file = JsonSerializer.Deserialize<CollectionFile>(json, options);
				if (file?.Cards is not null)
				{
					return file.Cards;
				}
			}
			else
			{
				// Migrate legacy bare-array format to v1 wrapper
				var loaded = JsonSerializer.Deserialize<List<Card>>(json, options);
				if (loaded is not null)
				{
					SaveCards(path, loaded, options);
					Console.WriteLine("Collection migrated to schema v1.");
					return loaded;
				}
			}
		}

		var initializedCards = ChecklistPdfParser.InitializeFromPdf(checklistPdfPath);
		SaveCards(path, initializedCards, options);
		return initializedCards;
	}

	public static List<InsertSet> LoadOrCreateInsertSets(string path, JsonSerializerOptions options)
	{
		if (File.Exists(path))
		{
			var json = File.ReadAllText(path);
			if (json.TrimStart().StartsWith('{'))
			{
				var file = JsonSerializer.Deserialize<InsertSetFile>(json, options);
				if (file?.InsertSets is not null)
				{
					return file.InsertSets;
				}
			}
			else
			{
				// Migrate legacy bare-array format to v1 wrapper
				var loaded = JsonSerializer.Deserialize<List<InsertSet>>(json, options);
				if (loaded is not null)
				{
					SaveInsertSets(path, loaded, options);
					Console.WriteLine("Insert sets migrated to schema v1.");
					return loaded;
				}
			}
		}

		var defaults = CommandHandler.CreateDefaultInsertSets();
		SaveInsertSets(path, defaults, options);
		return defaults;
	}

	public static void NormalizeCards(List<Card> cards)
	{
		foreach (var card in cards)
		{
			if (string.IsNullOrWhiteSpace(card.PlayerName))
			{
				card.PlayerName = $"Card {card.Id}";
			}

			card.Variants ??= new List<CardVariant>();
		}
	}

	public static void SaveCards(string path, List<Card> cards, JsonSerializerOptions options)
	{
		var file = new CollectionFile { Cards = cards };
		var json = JsonSerializer.Serialize(file, options);
		AtomicSave(path, json);
	}

	public static void SaveInsertSets(string path, List<InsertSet> insertSets, JsonSerializerOptions options)
	{
		var file = new InsertSetFile { InsertSets = insertSets };
		var json = JsonSerializer.Serialize(file, options);
		AtomicSave(path, json);
	}

	public static void AtomicSave(string path, string json)
	{
		var backup = path + ".bak";
		var tmp = path + ".tmp";

		File.WriteAllText(tmp, json, Encoding.UTF8);

		if (File.Exists(path))
		{
			File.Copy(path, backup, overwrite: true);
		}

		File.Move(tmp, path, overwrite: true);
	}
}
