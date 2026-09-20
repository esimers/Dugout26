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

	public static string CreateBackup(
		List<Card> cards,
		List<InsertSet> insertSets,
		JsonSerializerOptions options,
		string backupRoot = "backups")
	{
		Directory.CreateDirectory(backupRoot);
		var stamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
		var dir = Path.Combine(backupRoot, stamp);
		var suffix = 2;
		while (Directory.Exists(dir))
		{
			dir = Path.Combine(backupRoot, $"{stamp}_{suffix}");
			suffix++;
		}

		Directory.CreateDirectory(dir);
		File.WriteAllText(
			Path.Combine(dir, "collection.json"),
			JsonSerializer.Serialize(new CollectionFile { Cards = cards }, options),
			Encoding.UTF8);
		File.WriteAllText(
			Path.Combine(dir, "insert_sets.json"),
			JsonSerializer.Serialize(new InsertSetFile { InsertSets = insertSets }, options),
			Encoding.UTF8);
		return dir;
	}

	public static List<string> ListBackups(string backupRoot = "backups")
	{
		if (!Directory.Exists(backupRoot))
		{
			return new List<string>();
		}

		return Directory.GetDirectories(backupRoot)
			.Where(dir => File.Exists(Path.Combine(dir, "collection.json")) || File.Exists(Path.Combine(dir, "insert_sets.json")))
			.OrderByDescending(dir => dir, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	public static bool TryReadSnapshot(
		string path,
		JsonSerializerOptions options,
		out List<Card>? cards,
		out List<InsertSet>? insertSets,
		out string error)
	{
		cards = null;
		insertSets = null;
		error = string.Empty;

		if (string.IsNullOrWhiteSpace(path))
		{
			error = "Path is required.";
			return false;
		}

		path = path.Trim().Trim('"');

		if (Directory.Exists(path))
		{
			if (!TryLoadIfExists(Path.Combine(path, "collection.json"), options, ref cards, ref insertSets, out error) ||
				!TryLoadIfExists(Path.Combine(path, "insert_sets.json"), options, ref cards, ref insertSets, out error))
			{
				return false;
			}

			if (cards is null && insertSets is null)
			{
				error = $"No collection.json or insert_sets.json found in '{path}'.";
				return false;
			}

			return true;
		}

		if (!File.Exists(path))
		{
			error = $"Path not found: {path}";
			return false;
		}

		if (!TryLoadTypedFile(path, options, out cards, out insertSets, out error))
		{
			return false;
		}

		var dir = Path.GetDirectoryName(Path.GetFullPath(path));
		if (string.IsNullOrEmpty(dir))
		{
			return true;
		}

		if (cards is not null && insertSets is null)
		{
			foreach (var sibling in new[] { "insert_sets.json", "insert_sets.json.bak" })
			{
				var siblingPath = Path.Combine(dir, sibling);
				if (!File.Exists(siblingPath) || PathsEqual(siblingPath, path))
				{
					continue;
				}

				if (!TryLoadTypedFile(siblingPath, options, out _, out var loadedSets, out error))
				{
					return false;
				}

				if (loadedSets is not null)
				{
					insertSets = loadedSets;
					break;
				}
			}
		}
		else if (insertSets is not null && cards is null)
		{
			foreach (var sibling in new[] { "collection.json", "collection.json.bak" })
			{
				var siblingPath = Path.Combine(dir, sibling);
				if (!File.Exists(siblingPath) || PathsEqual(siblingPath, path))
				{
					continue;
				}

				if (!TryLoadTypedFile(siblingPath, options, out var loadedCards, out _, out error))
				{
					return false;
				}

				if (loadedCards is not null)
				{
					cards = loadedCards;
					break;
				}
			}
		}

		return true;
	}

	private static bool TryLoadIfExists(
		string path,
		JsonSerializerOptions options,
		ref List<Card>? cards,
		ref List<InsertSet>? insertSets,
		out string error)
	{
		error = string.Empty;
		if (!File.Exists(path))
		{
			return true;
		}

		if (!TryLoadTypedFile(path, options, out var loadedCards, out var loadedSets, out error))
		{
			return false;
		}

		if (loadedCards is not null)
		{
			cards = loadedCards;
		}

		if (loadedSets is not null)
		{
			insertSets = loadedSets;
		}

		return true;
	}

	private static bool TryLoadTypedFile(
		string path,
		JsonSerializerOptions options,
		out List<Card>? cards,
		out List<InsertSet>? insertSets,
		out string error)
	{
		cards = null;
		insertSets = null;
		error = string.Empty;

		try
		{
			var json = File.ReadAllText(path);
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			if (root.ValueKind == JsonValueKind.Array)
			{
				if (root.GetArrayLength() > 0 &&
					root[0].ValueKind == JsonValueKind.Object &&
					(root[0].TryGetProperty("Name", out _) || root[0].TryGetProperty("name", out _)) &&
					(root[0].TryGetProperty("Code", out _) || root[0].TryGetProperty("code", out _)))
				{
					insertSets = JsonSerializer.Deserialize<List<InsertSet>>(json, options) ?? new List<InsertSet>();
					return true;
				}

				cards = JsonSerializer.Deserialize<List<Card>>(json, options) ?? new List<Card>();
				return true;
			}

			if (root.ValueKind != JsonValueKind.Object)
			{
				error = $"'{path}' is not a JSON object or array.";
				return false;
			}

			if (root.TryGetProperty("cards", out _))
			{
				var file = JsonSerializer.Deserialize<CollectionFile>(json, options);
				if (file?.Cards is null)
				{
					error = $"'{path}' has no cards array.";
					return false;
				}

				cards = file.Cards;
				return true;
			}

			if (root.TryGetProperty("insertSets", out _))
			{
				var file = JsonSerializer.Deserialize<InsertSetFile>(json, options);
				if (file?.InsertSets is null)
				{
					error = $"'{path}' has no insertSets array.";
					return false;
				}

				insertSets = file.InsertSets;
				return true;
			}

			error = $"'{path}' is not a Dugout collection or insert-sets file.";
			return false;
		}
		catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
		{
			error = $"Could not read '{path}': {ex.Message}";
			return false;
		}
	}

	private static bool PathsEqual(string left, string right)
	{
		return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
	}

	public static string ExportMissingCards(List<Card> cards, string seriesArg, string format = "txt")
	{
		var filtered = CardLogic.FilterBySeriesArg(cards.Where(c => !c.IsOwned), seriesArg, out var seriesLabel, out var fileTag)
			.OrderBy(c => c.Id)
			.ToList();

		var isCsv = format.Equals("csv", StringComparison.OrdinalIgnoreCase);
		var filename = Path.Combine(EnsureExportDir(), $"missing_{fileTag}.{(isCsv ? "csv" : "txt")}");

		var sb = new StringBuilder();
		if (isCsv)
		{
			sb.AppendLine("CardId,PlayerName,Series");
			foreach (var card in filtered)
			{
				sb.AppendLine($"{card.Id},{EscapeCsv(card.PlayerName)},{SeriesName(card.Id)}");
			}
		}
		else
		{
			sb.AppendLine("==================================================");
			sb.AppendLine($"DUGOUT '26 - MISSING BASE CARDS REPORT ({seriesLabel})");
			sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
			sb.AppendLine("==================================================");
			sb.AppendLine();

			if (filtered.Count == 0)
			{
				sb.AppendLine("No missing cards found in this series query!");
			}
			else
			{
				foreach (var card in filtered)
				{
					sb.AppendLine($"#{card.Id,3}  {card.PlayerName,-30} [{SeriesTag(card.Id)}]");
				}

				sb.AppendLine();
				sb.AppendLine("--------------------------------------------------");
				sb.AppendLine($"Total Missing: {filtered.Count} card(s)");
				sb.AppendLine("--------------------------------------------------");
			}
		}

		File.WriteAllText(filename, sb.ToString(), Encoding.UTF8);
		return filename;
	}

	public static string ExportDuplicateCards(List<Card> cards, string seriesArg, string format = "txt")
	{
		var filtered = CardLogic.FilterBySeriesArg(cards.Where(c => c.Quantity > 1), seriesArg, out var seriesLabel, out var fileTag)
			.OrderBy(c => c.Id)
			.ToList();

		var isCsv = format.Equals("csv", StringComparison.OrdinalIgnoreCase);
		var filename = Path.Combine(EnsureExportDir(), $"duplicates_{fileTag}.{(isCsv ? "csv" : "txt")}");
		var totalExtras = filtered.Sum(c => c.Quantity - 1);

		var sb = new StringBuilder();
		if (isCsv)
		{
			sb.AppendLine("CardId,PlayerName,Series,Quantity,Extras");
			foreach (var card in filtered)
			{
				sb.AppendLine($"{card.Id},{EscapeCsv(card.PlayerName)},{SeriesName(card.Id)},{card.Quantity},{card.Quantity - 1}");
			}
		}
		else
		{
			sb.AppendLine("==================================================");
			sb.AppendLine($"DUGOUT '26 - DUPLICATE BASE CARDS / TRADE LIST ({seriesLabel})");
			sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
			sb.AppendLine("==================================================");
			sb.AppendLine();

			if (filtered.Count == 0)
			{
				sb.AppendLine("No duplicate base cards found in this series query!");
			}
			else
			{
				foreach (var card in filtered)
				{
					sb.AppendLine($"#{card.Id,3}  {card.PlayerName,-30} [{SeriesTag(card.Id)}]  qty {card.Quantity}  extra +{card.Quantity - 1}");
				}

				sb.AppendLine();
				sb.AppendLine("--------------------------------------------------");
				sb.AppendLine($"Players with extras: {filtered.Count}    Trade copies: {totalExtras}");
				sb.AppendLine("--------------------------------------------------");
			}
		}

		File.WriteAllText(filename, sb.ToString(), Encoding.UTF8);
		return filename;
	}

	private static string EnsureExportDir()
	{
		const string dir = "exports";
		if (!Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}

		return dir;
	}

	private static string EscapeCsv(string value)
	{
		return value.Contains(',') ? $"\"{value}\"" : value;
	}

	private static string SeriesName(int id) => id <= 350 ? "Series 1" : "Series 2";

	private static string SeriesTag(int id) => id <= 350 ? "S1" : "S2";
}
