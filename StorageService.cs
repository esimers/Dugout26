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

			if (card.IsOwned && card.Quantity == 0)
			{
				card.Quantity = 1;
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

	public static string ExportMissingCards(List<Card> cards, string seriesArg, string format = "txt")
	{
		var dir = "exports";
		if (!Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}

		var isCsv = format.Equals("csv", StringComparison.OrdinalIgnoreCase);
		var ext = isCsv ? "csv" : "txt";
		var argTrim = seriesArg.Trim().ToLowerInvariant();

		var missingQuery = cards.Where(c => !c.IsOwned);
		string seriesLabel;
		string filename;

		if (argTrim is "s1" or "series1" or "series 1")
		{
			missingQuery = missingQuery.Where(c => c.Id >= 1 && c.Id <= 350);
			seriesLabel = "Series 1 (#1 - #350)";
			filename = Path.Combine(dir, $"missing_series1.{ext}");
		}
		else if (argTrim is "s2" or "series2" or "series 2")
		{
			missingQuery = missingQuery.Where(c => c.Id >= 351 && c.Id <= 700);
			seriesLabel = "Series 2 (#351 - #700)";
			filename = Path.Combine(dir, $"missing_series2.{ext}");
		}
		else if (ConsoleUi.TryParseRange(seriesArg, out var startId, out var endId))
		{
			missingQuery = missingQuery.Where(c => c.Id >= startId && c.Id <= endId);
			seriesLabel = $"Cards #{startId} - #{endId}";
			filename = Path.Combine(dir, $"missing_{startId}_{endId}.{ext}");
		}
		else
		{
			seriesLabel = "All Series (#1 - #700)";
			filename = Path.Combine(dir, $"missing_all.{ext}");
		}

		var missingCards = missingQuery.OrderBy(c => c.Id).ToList();

		var sb = new StringBuilder();
		if (isCsv)
		{
			sb.AppendLine("CardId,PlayerName,Series");
			foreach (var card in missingCards)
			{
				var series = card.Id <= 350 ? "Series 1" : "Series 2";
				var escapedName = card.PlayerName.Contains(',') ? $"\"{card.PlayerName}\"" : card.PlayerName;
				sb.AppendLine($"{card.Id},{escapedName},{series}");
			}
		}
		else
		{
			sb.AppendLine("==================================================");
			sb.AppendLine($"DUGOUT '26 - MISSING BASE CARDS REPORT ({seriesLabel})");
			sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
			sb.AppendLine("==================================================");
			sb.AppendLine();

			if (missingCards.Count == 0)
			{
				sb.AppendLine("No missing cards found in this series query!");
			}
			else
			{
				foreach (var card in missingCards)
				{
					var series = card.Id <= 350 ? "S1" : "S2";
					sb.AppendLine($"#{card.Id,3}  {card.PlayerName,-30} [{series}]");
				}

				sb.AppendLine();
				sb.AppendLine("--------------------------------------------------");
				sb.AppendLine($"Total Missing: {missingCards.Count} card(s)");
				sb.AppendLine("--------------------------------------------------");
			}
		}

		File.WriteAllText(filename, sb.ToString(), Encoding.UTF8);
		return filename;
	}
}
