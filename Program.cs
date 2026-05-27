using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

const string FilePath = "collection.json";
const string InsertSetsPath = "insert_sets.json";
const string ChecklistPdfPath = "checklistInputs/2026_Topps_Series_1_Baseball_Checklist.pdf";
const string OddsPdfPath = "checklistInputs/2026_Topps_Baseball_Series_1_Odds.pdf";
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
var cards = LoadOrCreateCards(FilePath, jsonOptions);
var insertSets = LoadOrCreateInsertSets(InsertSetsPath, jsonOptions);
if (NormalizeInsertSets(insertSets))
	{
	SaveInsertSets(InsertSetsPath, insertSets, jsonOptions);
}
if (HydrateInsertSetNumbersFromChecklist(insertSets))
{
	SaveInsertSets(InsertSetsPath, insertSets, jsonOptions);
}

NormalizeCards(cards);
if (HydratePlaceholderNamesFromChecklist(cards))
{
	SaveCards(FilePath, cards, jsonOptions);
}

var oddsEntries = LoadOddsFromPdf(OddsPdfPath);
var autoStatsPanel = true;

DrawArcadeHeader(cards, oddsEntries);
PrintHelp();
DrawStatsPanel(cards, oddsEntries, insertSets);

while (true)
{
	SetColor(ConsoleColor.DarkRed);
	Console.Write("DUGOUT");
	SetColor(ConsoleColor.Gray);
	Console.Write("> ");
	Console.ResetColor();

	var input = Console.ReadLine();
	if (string.IsNullOrWhiteSpace(input))
	{
		continue;
	}

	var parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
	var command = parts[0].ToLowerInvariant();
	var argument = parts.Length > 1 ? parts[1] : string.Empty;

	switch (command)
	{
		case "have":
		case "h":
			if (string.IsNullOrWhiteSpace(argument))
			{
				Console.WriteLine("Usage: have 1, 5, 10-15");
				break;
			}

			var ids = ParseIds(argument);
			if (ids.Count == 0)
			{
				Console.WriteLine("No valid IDs found.");
				break;
			}

			var markedCount = 0;
			foreach (var id in ids)
			{
				var card = cards.FirstOrDefault(c => c.Id == id);
				if (card is null)
				{
					Console.WriteLine($"Card #{id} not found.");
					continue;
				}

				if (!card.IsOwned)
				{
					card.IsOwned = true;
					markedCount++;
				}
			}

			SaveCards(FilePath, cards, jsonOptions);
			SetColor(ConsoleColor.Green);
			Console.WriteLine($"Roster update: signed {markedCount} base card(s).");
			Console.ResetColor();
			if (autoStatsPanel)
			{
				DrawStatsPanel(cards, oddsEntries, insertSets);
			}
			break;

		case "missing":
		case "m":
			var missingQuery = cards.Where(c => !c.IsOwned);
			if (!string.IsNullOrWhiteSpace(argument) && !argument.Equals("-a", StringComparison.OrdinalIgnoreCase) && !argument.Equals("all", StringComparison.OrdinalIgnoreCase))
			{
				if (!TryParseRange(argument, out var startId, out var endId))
				{
					Console.WriteLine("Usage: missing, missing -a, or missing <start-end> (example: missing 25-80)");
					break;
				}

				missingQuery = missingQuery.Where(c => c.Id >= startId && c.Id <= endId);
			}

			var missingCards = missingQuery.OrderBy(c => c.Id).ToList();
			if (missingCards.Count == 0)
			{
				SetColor(ConsoleColor.Green);
				if (string.IsNullOrWhiteSpace(argument) || argument.Equals("-a", StringComparison.OrdinalIgnoreCase) || argument.Equals("all", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("WORLD SERIES COMPLETE: all base cards owned.");
				}
				else
				{
					Console.WriteLine("No missing cards in that range.");
				}
				Console.ResetColor();
				break;
			}

			SetColor(ConsoleColor.Yellow);
			Console.WriteLine("Scout Report - Missing Base Cards");
			Console.ResetColor();
			foreach (var card in missingCards)
			{
				Console.WriteLine(FormatCardLabel(card));
			}
			break;

		case "list":
		case "roster":
			foreach (var card in cards.OrderBy(c => c.Id))
			{
				var baseStatus = card.IsOwned ? "OWNED" : "MISSING";
				var parallelOwned = card.Variants.Count(v => v.IsOwned);
				SetColor(card.IsOwned ? ConsoleColor.Green : ConsoleColor.Red);
				Console.WriteLine($"{FormatCardLabel(card),-36}  Base:{baseStatus,-7}  Parallels:{parallelOwned}");
				Console.ResetColor();
			}
			break;

		case "check":
		case "c":
			if (!int.TryParse(argument, out var checkId))
			{
				Console.WriteLine("Usage: check <cardId>");
				break;
			}

			var checkCard = cards.FirstOrDefault(c => c.Id == checkId);
			if (checkCard is null)
			{
				Console.WriteLine($"Card #{checkId} not found.");
				break;
			}

			SetColor(checkCard.IsOwned ? ConsoleColor.Green : ConsoleColor.Yellow);
			Console.WriteLine($"{FormatCardLabel(checkCard)} - {(checkCard.IsOwned ? "OWNED" : "MISSING")}");
			Console.ResetColor();
			break;

		case "hit":
			if (string.IsNullOrWhiteSpace(argument))
			{
				Console.WriteLine("Usage: hit <cardId> <parallel name>");
				Console.WriteLine("Example: hit 24 Gold Foil");
				break;
			}

			var hitParts = argument.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
			if (hitParts.Length < 2 || !int.TryParse(hitParts[0], out var hitCardId))
			{
				Console.WriteLine("Usage: hit <cardId> <parallel name>");
				break;
			}

			var hitCard = cards.FirstOrDefault(c => c.Id == hitCardId);
			if (hitCard is null)
			{
				Console.WriteLine($"Card #{hitCardId} not found.");
				break;
			}

			var variantName = hitParts[1].Trim();
			if (variantName.Length == 0)
			{
				Console.WriteLine("Parallel name is required.");
				break;
			}

			var variant = hitCard.Variants.FirstOrDefault(v => v.Name.Equals(variantName, StringComparison.OrdinalIgnoreCase));
			if (variant is null)
			{
				var matchedOdds = FindBestOddsMatch(variantName, oddsEntries);
				variant = new CardVariant
				{
					Name = variantName,
					Odds = matchedOdds?.OddsText,
					Rarity = matchedOdds?.Rarity ?? "Unknown"
				};
				hitCard.Variants.Add(variant);
			}

			variant.IsOwned = true;
			SaveCards(FilePath, cards, jsonOptions);
			SetColor(GetRarityColor(variant.Rarity));
			Console.WriteLine($"Hit logged: {FormatCardLabel(hitCard)} - {variant.Name} ({variant.Rarity}){(string.IsNullOrWhiteSpace(variant.Odds) ? string.Empty : $", odds {variant.Odds}")}");
			Console.ResetColor();
			if (autoStatsPanel)
			{
				DrawStatsPanel(cards, oddsEntries, insertSets);
			}
			break;

		case "unhit":
		case "uh":
			if (string.IsNullOrWhiteSpace(argument))
			{
				Console.WriteLine("Usage: unhit <cardId> <parallel name>");
				Console.WriteLine("Example: unhit 24 Gold Foil");
				break;
			}

			var unhitParts = argument.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
			if (unhitParts.Length < 2 || !int.TryParse(unhitParts[0], out var unhitCardId))
			{
				Console.WriteLine("Usage: unhit <cardId> <parallel name>");
				break;
			}

			var unhitCard = cards.FirstOrDefault(c => c.Id == unhitCardId);
			if (unhitCard is null)
			{
				Console.WriteLine($"Card #{unhitCardId} not found.");
				break;
			}

			var unhitVariantName = unhitParts[1].Trim();
			var unhitVariant = unhitCard.Variants.FirstOrDefault(v => v.Name.Equals(unhitVariantName, StringComparison.OrdinalIgnoreCase));
			if (unhitVariant is null || !unhitVariant.IsOwned)
			{
				Console.WriteLine($"No owned parallel named '{unhitVariantName}' is logged for {FormatCardLabel(unhitCard)}.");
				break;
			}

			unhitVariant.IsOwned = false;
			if (!unhitCard.Variants.Any(v => v.IsOwned))
			{
				unhitCard.Variants.RemoveAll(v => !v.IsOwned);
			}

			SaveCards(FilePath, cards, jsonOptions);
			SetColor(ConsoleColor.Yellow);
			Console.WriteLine($"Removed parallel: {FormatCardLabel(unhitCard)} - {unhitVariant.Name}");
			Console.ResetColor();
			if (autoStatsPanel)
			{
				DrawStatsPanel(cards, oddsEntries, insertSets);
			}
			break;

		case "stats":
			DrawStatsPanel(cards, oddsEntries, insertSets);
			break;

		case "inserts":
		case "insert":
			if (HandleInsertCommand(argument, insertSets))
			{
				SaveInsertSets(InsertSetsPath, insertSets, jsonOptions);
			}
			break;

		case "parallels":
		case "p":
			if (!TryParseParallelFilters(argument, out var parallelStartId, out var parallelEndId, out var rarityFilter))
			{
				Console.WriteLine("Usage: parallels [start-end] [rarity]");
				Console.WriteLine("Examples: parallels rare | parallels 1-100 | parallels 1-100 ultra rare");
				break;
			}

			PrintOwnedParallelsReport(cards, parallelStartId, parallelEndId, rarityFilter);
			break;

		case "teams":
			PrintTeamCardReport(cards);
			break;

		case "odds":
			PrintOddsBoard(oddsEntries);
			break;

		case "panel":
			if (argument.Equals("on", StringComparison.OrdinalIgnoreCase))
			{
				autoStatsPanel = true;
				Console.WriteLine("Stats panel is ON.");
			}
			else if (argument.Equals("off", StringComparison.OrdinalIgnoreCase))
			{
				autoStatsPanel = false;
				Console.WriteLine("Stats panel is OFF.");
			}
			else
			{
				Console.WriteLine("Usage: panel on|off");
			}
			break;

		case "help":
			PrintHelp();
			break;

		case "exit":
		case "quit":
		case "q":
			SaveCards(FilePath, cards, jsonOptions);
			Console.WriteLine("Collection saved. Goodbye.");
			return;

		default:
			Console.WriteLine("Unknown command. Type 'help' for available commands.");
			break;
	}
}

static List<Card> LoadOrCreateCards(string path, JsonSerializerOptions options)
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

	var initializedCards = InitializeFromPdf();
	SaveCards(path, initializedCards, options);
	return initializedCards;
}

static List<InsertSet> LoadOrCreateInsertSets(string path, JsonSerializerOptions options)
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

	var defaults = CreateDefaultInsertSets();
	SaveInsertSets(path, defaults, options);
	return defaults;
}

static List<Card> InitializeFromPdf()
{
	var cards = Enumerable.Range(1, 350)
		.Select(id => new Card { Id = id, PlayerName = $"Card {id}", IsOwned = false })
		.ToList();

	if (!TryExtractChecklistNames(out var namesById))
	{
		Console.WriteLine($"Checklist names unavailable from {ChecklistPdfPath}. Using placeholder names.");
		return cards;
	}

	foreach (var card in cards)
	{
		if (namesById.TryGetValue(card.Id, out var playerName))
		{
			card.PlayerName = playerName;
		}
	}

	Console.WriteLine($"Initialized 350 cards from PDF. Matched names: {namesById.Count}.");

	return cards;
}

static bool HydratePlaceholderNamesFromChecklist(List<Card> cards)
{
	var placeholders = cards.Where(c => IsPlaceholderName(c.PlayerName, c.Id)).ToList();
	if (placeholders.Count == 0)
	{
		return false;
	}

	if (!TryExtractChecklistNames(out var namesById))
	{
		return false;
	}

	var updated = 0;
	foreach (var card in placeholders)
	{
		if (namesById.TryGetValue(card.Id, out var playerName) && !IsPlaceholderName(playerName, card.Id))
		{
			card.PlayerName = playerName;
			updated++;
		}
	}

	if (updated > 0)
	{
		Console.WriteLine($"Checklist sync: updated {updated} placeholder player names.");
	}

	return updated > 0;
}

static bool TryExtractChecklistNames(out Dictionary<int, string> namesById)
{
	namesById = new Dictionary<int, string>();

	if (!File.Exists(ChecklistPdfPath))
	{
		Console.WriteLine($"Checklist PDF not found: {ChecklistPdfPath}.");
		return false;
	}

	try
	{
		var allText = new StringBuilder();
		using var document = PdfDocument.Open(ChecklistPdfPath);
		foreach (var page in document.GetPages())
		{
			allText.AppendLine(page.Text);
		}

		namesById = ExtractChecklistNamesFromText(allText.ToString());
		if (namesById.Count > 0)
		{
			return true;
		}

		if (TryReadTextWithPdftotext(out var pdftotextOutput))
		{
			namesById = ExtractChecklistNamesFromText(pdftotextOutput);
			if (namesById.Count > 0)
			{
				return true;
			}
		}

		return false;
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Failed to read checklist PDF: {ex.Message}");

		if (TryReadTextWithPdftotext(out var pdftotextOutput))
		{
			namesById = ExtractChecklistNamesFromText(pdftotextOutput);
			if (namesById.Count > 0)
			{
				return true;
			}
		}

		return false;
	}
}

static Dictionary<int, string> ExtractChecklistNamesFromText(string text)
{
	var namesById = new Dictionary<int, string>();
	var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
	var linePattern = new Regex(@"^\s*(?<id>(?:[1-9]|[1-9]\d|[12]\d{2}|3[0-4]\d|350))\s+(?<name>.+?)\s*$", RegexOptions.Compiled);

	foreach (var rawLine in lines)
	{
		var line = Regex.Replace(rawLine.Trim(), @"\s+", " ");
		var match = linePattern.Match(line);
		if (!match.Success)
		{
			continue;
		}

		if (!int.TryParse(match.Groups["id"].Value, out var id) || id < 1 || id > 350)
		{
			continue;
		}

		var candidate = match.Groups["name"].Value.Trim();
		candidate = Regex.Replace(candidate, @"\s+", " ");
		candidate = candidate.Replace("®", string.Empty).Replace("™", string.Empty).Trim();
		if (TryNormalizeTeamName(candidate, out var normalizedTeamName))
		{
			candidate = normalizedTeamName;
		}

		if (!IsLikelyPlayerName(candidate))
		{
			continue;
		}

		if (!namesById.ContainsKey(id))
		{
			namesById[id] = candidate;
		}
	}

	return namesById;
}

static bool TryReadTextWithPdftotext(out string output)
{
	output = string.Empty;

	try
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = "pdftotext",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		startInfo.ArgumentList.Add(ChecklistPdfPath);
		startInfo.ArgumentList.Add("-");

		using var process = Process.Start(startInfo);
		if (process is null)
		{
			return false;
		}

		output = process.StandardOutput.ReadToEnd();
		process.WaitForExit();

		return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
	}
	catch
	{
		return false;
	}
}

static bool HydrateInsertSetNumbersFromChecklist(List<InsertSet> insertSets)
{
	if (!TryExtractInsertSetCardsFromChecklist(out var cardsByCode))
	{
		return false;
	}

	var changed = false;
	foreach (var set in insertSets)
	{
		var code = NormalizeInsertCode(set.Code);
		set.ValidCardNumbers ??= new List<int>();
		set.ValidCards ??= new List<InsertCardInfo>();

		if (!cardsByCode.TryGetValue(code, out var cards))
		{
			continue;
		}

		var ordered = cards.Keys.OrderBy(n => n).ToList();
		if (set.ValidCardNumbers.Count != ordered.Count || !set.ValidCardNumbers.SequenceEqual(ordered))
		{
			set.ValidCardNumbers = ordered;
			changed = true;
		}

		var orderedCards = cards
			.OrderBy(kvp => kvp.Key)
			.Select(kvp => new InsertCardInfo { Number = kvp.Key, Name = kvp.Value })
			.ToList();

		if (set.ValidCards.Count != orderedCards.Count ||
			!set.ValidCards.Select(c => (c.Number, c.Name)).SequenceEqual(orderedCards.Select(c => (c.Number, c.Name))))
		{
			set.ValidCards = orderedCards;
			changed = true;
		}
	}

	return changed;
}

static bool TryExtractInsertSetCardsFromChecklist(out Dictionary<string, Dictionary<int, string>> cardsByCode)
{
	cardsByCode = new Dictionary<string, Dictionary<int, string>>(StringComparer.OrdinalIgnoreCase);

	if (!TryReadTextWithPdftotext(out var text))
	{
		return false;
	}

	var headingMap = GetInsertHeadingToCodeMap();
	var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
	string? currentCode = null;
	var pendingNumbers = new Queue<int>();

	foreach (var rawLine in lines)
	{
		var line = Regex.Replace(rawLine.Trim(), @"\s+", " ");
		if (line.Length == 0)
		{
			continue;
		}

		var normalizedHeading = NormalizeHeading(line);
		if (headingMap.TryGetValue(normalizedHeading, out var mappedCode))
		{
			currentCode = mappedCode;
			pendingNumbers.Clear();
			if (!cardsByCode.ContainsKey(currentCode))
			{
				cardsByCode[currentCode] = new Dictionary<int, string>();
			}
			continue;
		}

		if (currentCode is null)
		{
			continue;
		}

		var prefixed = Regex.Match(line, @"^(?<prefix>[A-Z0-9]+)-(?<num>\d{1,3})(?:\s+(?<name>.+))?$");
		if (prefixed.Success)
		{
			var prefix = NormalizeInsertCode(prefixed.Groups["prefix"].Value);
			if (!IsAllowedPrefixForSet(currentCode, prefix))
			{
				continue;
			}

			if (int.TryParse(prefixed.Groups["num"].Value, out var prefixedNumber) && prefixedNumber > 0)
			{
				var name = NormalizeInsertCardName(prefixed.Groups["name"].Value);
				if (name.Length > 0)
				{
					SetInsertCardName(cardsByCode[currentCode], prefixedNumber, name);
				}
				else
				{
					if (!cardsByCode[currentCode].ContainsKey(prefixedNumber))
					{
						cardsByCode[currentCode][prefixedNumber] = string.Empty;
					}
					pendingNumbers.Enqueue(prefixedNumber);
				}
			}
			continue;
		}

		var inlineNumeric = Regex.Match(line, @"^(?<num>\d{1,3})\s+(?<name>.+)$");
		if (inlineNumeric.Success)
		{
			if (int.TryParse(inlineNumeric.Groups["num"].Value, out var inlineNumber) && inlineNumber > 0)
			{
				var name = NormalizeInsertCardName(inlineNumeric.Groups["name"].Value);
				if (name.Length > 0 && IsLikelyInsertCardName(name))
				{
					SetInsertCardName(cardsByCode[currentCode], inlineNumber, name);
				}
				else if (!cardsByCode[currentCode].ContainsKey(inlineNumber))
				{
					cardsByCode[currentCode][inlineNumber] = string.Empty;
				}
			}
			continue;
		}

		if (pendingNumbers.Count > 0)
		{
			var parsedName = NormalizeInsertCardName(line);
			if (parsedName.Length > 0 && IsLikelyInsertCardName(parsedName))
			{
				var nextNumber = pendingNumbers.Dequeue();
				cardsByCode[currentCode][nextNumber] = parsedName;
			}
		}
	}

	foreach (var code in cardsByCode.Keys.ToList())
	{
		var normalized = cardsByCode[code]
			.Where(kvp => kvp.Key > 0)
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, comparer: EqualityComparer<int>.Default);

		cardsByCode[code] = normalized;
	}

	return cardsByCode.Count > 0;
}

static bool IsAllowedPrefixForSet(string currentCode, string prefix)
{
	var normalizedCode = NormalizeInsertCode(currentCode);
	if (prefix.Equals(normalizedCode, StringComparison.OrdinalIgnoreCase))
	{
		return true;
	}

	return normalizedCode switch
	{
		"8BB" => prefix.Equals("8B", StringComparison.OrdinalIgnoreCase),
		"T91C" => prefix.Equals("91C", StringComparison.OrdinalIgnoreCase),
		"MAS" => prefix.Equals("M", StringComparison.OrdinalIgnoreCase),
		_ => false
	};
}

static void SetInsertCardName(Dictionary<int, string> cardsByNumber, int number, string candidateName)
{
	if (!cardsByNumber.TryGetValue(number, out var existing) || string.IsNullOrWhiteSpace(existing))
	{
		cardsByNumber[number] = candidateName;
	}
}

static string NormalizeInsertCardName(string value)
{
	if (string.IsNullOrWhiteSpace(value))
	{
		return string.Empty;
	}

	var cleaned = value.Trim()
		.Replace("®", string.Empty)
		.Replace("™", string.Empty)
		.Trim();
	cleaned = Regex.Replace(cleaned, @"\s+", " ");
	return cleaned;
}

static bool IsLikelyInsertCardName(string value)
{
	if (value.Length < 2 || value.Any(char.IsDigit))
	{
		return false;
	}

	if (IsLikelyTeamName(value))
	{
		return false;
	}

	var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
	if (words.Length > 6)
	{
		return false;
	}

	if (words.Any(w => w.Equals("Rookie", StringComparison.OrdinalIgnoreCase)))
	{
		return false;
	}

	return words.All(w => Regex.IsMatch(w, @"^[A-Za-zÀ-ÖØ-öø-ÿ.'\-]+$"));
}

static bool IsLikelyTeamName(string value)
{
	var cleaned = NormalizeInsertCardName(value);
	if (cleaned.Length == 0)
	{
		return false;
	}

	if (GetMlbTeams().Any(team => cleaned.Equals(team, StringComparison.OrdinalIgnoreCase)))
	{
		return true;
	}

	var nicknames = new[]
	{
		"Angels",
		"Athletics",
		"Braves",
		"Brewers",
		"Cardinals",
		"Cubs",
		"Dodgers",
		"Giants",
		"Guardians",
		"Mariners",
		"Marlins",
		"Mets",
		"Nationals",
		"Orioles",
		"Padres",
		"Phillies",
		"Pirates",
		"Rangers",
		"Rays",
		"Red Sox",
		"Reds",
		"Rockies",
		"Royals",
		"Tigers",
		"Twins",
		"White Sox",
		"Yankees"
	};

	return nicknames.Any(n => cleaned.Equals(n, StringComparison.OrdinalIgnoreCase));
}

static Dictionary<string, string> GetInsertHeadingToCodeMap()
{
	return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		[NormalizeHeading("2025 ALL TOPPS TEAM")] = "ATT",
		[NormalizeHeading("TOPPS PROFILES")] = "TP",
		[NormalizeHeading("BIG TICKET PLAYERS")] = "BTP",
		[NormalizeHeading("2025 GREATEST HITS")] = "GH",
		[NormalizeHeading("FIRST PITCH")] = "FP",
		[NormalizeHeading("COVER ATHLETES CARDS")] = "CAC",
		[NormalizeHeading("ALL ACES")] = "AA",
		[NormalizeHeading("ALL KINGS")] = "AK",
		[NormalizeHeading("1991 TOPPS BASEBALL")] = "T91",
		[NormalizeHeading("STARS OF MLB")] = "SMLB",
		[NormalizeHeading("TITANS OF THE GAME")] = "TOG",
		[NormalizeHeading("DUGOUT PEEKS")] = "DP",
		[NormalizeHeading("WALK THIS WAY")] = "WTW",
		[NormalizeHeading("MASCOTS")] = "MAS",
		[NormalizeHeading("GAMEDAY DRIP")] = "GD",
		[NormalizeHeading("PERENNIAL ALL STARS")] = "PAS",
		[NormalizeHeading("8 BIT BALLERS")] = "8BB",
		[NormalizeHeading("HIDDEN MASCOTS")] = "HM",
		[NormalizeHeading("OVERSIZED 2026 TOPPS BASEBALL")] = "O26",
		[NormalizeHeading("COMPANION CARD")] = "CC",
		[NormalizeHeading("FUNKO BASE CARDS")] = "FBC",
		[NormalizeHeading("1991 TOPPS BASEBALL CHROME BASE CARDS")] = "T91C",
		[NormalizeHeading("ICONIC TOPPS BUYBACK CARDS")] = "ITB",
		[NormalizeHeading("75 YEARS OF TOPPS BASEBALL GIFTS")] = "75Y"
	};
}

static string NormalizeHeading(string value)
{
	var upper = value.ToUpperInvariant();
	upper = upper.Replace("'", string.Empty);
	upper = Regex.Replace(upper, @"[^A-Z0-9 ]", " ");
	upper = Regex.Replace(upper, @"\s+", " ").Trim();
	return upper;
}

static bool IsLikelyPlayerName(string value)
{
	if (string.IsNullOrWhiteSpace(value))
	{
		return false;
	}

	if (value.Any(char.IsDigit))
	{
		return false;
	}

	var normalized = value.ToUpperInvariant();
	var blockedWords = new[] { "BASE", "SET", "CHECKLIST", "SUBJECT", "ODDS", "INSERT", "TEAM", "ROOKIES", "TOPPS" };
	if (blockedWords.Any(normalized.Contains))
	{
		return false;
	}

	var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
	if (TryNormalizeTeamName(value, out _))
	{
		return true;
	}

	if (words.Length < 2 || words.Length > 5)
	{
		return false;
	}

	return words.All(w => Regex.IsMatch(w, @"^[A-Za-zÀ-ÖØ-öø-ÿ.'\-]+$"));
}

static bool IsPlaceholderName(string playerName, int id)
{
	return playerName.Equals($"Card {id}", StringComparison.OrdinalIgnoreCase);
}

static List<OddsEntry> LoadOddsFromPdf(string path)
{
	if (!File.Exists(path))
	{
		Console.WriteLine($"Odds PDF not found: {path}. Odds board disabled.");
		return new List<OddsEntry>();
	}

	try
	{
		var allText = new StringBuilder();
		using var document = PdfDocument.Open(path);
		foreach (var page in document.GetPages())
		{
			allText.AppendLine(page.Text);
		}

		var entries = new List<OddsEntry>();
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var lines = allText.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

		foreach (var rawLine in lines)
		{
			var line = Regex.Replace(rawLine.Trim(), @"\s+", " ");
			if (line.Length < 6)
			{
				continue;
			}

			var match = Regex.Match(line, @"^(?<name>.+?)\s+(?<odds>\d+\s*:\s*\d+)(?:\b|$)");
			if (!match.Success)
			{
				match = Regex.Match(line, @"^(?<odds>\d+\s*:\s*\d+)\s+(?<name>.+)$");
			}

			if (!match.Success)
			{
				continue;
			}

			var name = match.Groups["name"].Value.Trim(' ', '.', '-', ':');
			var odds = match.Groups["odds"].Value.Replace(" ", string.Empty);
			if (name.Length < 3)
			{
				continue;
			}

			var key = $"{name}|{odds}";
			if (!seen.Add(key))
			{
				continue;
			}

			var oneIn = ParseOneInOdds(odds);
			entries.Add(new OddsEntry
			{
				Name = name,
				OddsText = odds,
				OneIn = oneIn,
				Rarity = ClassifyRarity(name, oneIn)
			});
		}

		Console.WriteLine($"Loaded odds entries: {entries.Count}.");
		return entries.OrderBy(o => o.OneIn).ToList();
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Could not parse odds PDF: {ex.Message}");
		return new List<OddsEntry>();
	}
}

static double ParseOneInOdds(string odds)
{
	var match = Regex.Match(odds, @"(?<left>\d+)\s*:\s*(?<right>\d+)");
	if (!match.Success)
	{
		return double.PositiveInfinity;
	}

	if (!double.TryParse(match.Groups["left"].Value, out var left) ||
		!double.TryParse(match.Groups["right"].Value, out var right) ||
		left <= 0)
	{
		return double.PositiveInfinity;
	}

	return right / left;
}

static string ClassifyRarity(string name, double oneIn)
{
	if (name.Contains("base", StringComparison.OrdinalIgnoreCase))
	{
		return "Common/Base";
	}

	if (oneIn <= 6)
	{
		return "Common";
	}

	if (oneIn <= 24)
	{
		return "Uncommon";
	}

	if (oneIn <= 96)
	{
		return "Rare";
	}

	return "Ultra Rare";
}

static OddsEntry? FindBestOddsMatch(string variantName, List<OddsEntry> oddsEntries)
{
	if (oddsEntries.Count == 0)
	{
		return null;
	}

	var exact = oddsEntries.FirstOrDefault(o => o.Name.Equals(variantName, StringComparison.OrdinalIgnoreCase));
	if (exact is not null)
	{
		return exact;
	}

	var contains = oddsEntries.FirstOrDefault(o =>
		o.Name.Contains(variantName, StringComparison.OrdinalIgnoreCase) ||
		variantName.Contains(o.Name, StringComparison.OrdinalIgnoreCase));

	if (contains is not null)
	{
		return contains;
	}

	var words = variantName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
		.Where(w => w.Length >= 4)
		.ToList();

	if (words.Count == 0)
	{
		return null;
	}

	return oddsEntries
		.Select(o => new
		{
			Entry = o,
			Score = words.Count(w => o.Name.Contains(w, StringComparison.OrdinalIgnoreCase))
		})
		.Where(x => x.Score > 0)
		.OrderByDescending(x => x.Score)
		.ThenBy(x => x.Entry.OneIn)
		.Select(x => x.Entry)
		.FirstOrDefault();
}

static void DrawArcadeHeader(List<Card> cards, List<OddsEntry> oddsEntries)
{
	Console.Clear();
	SetColor(ConsoleColor.Red);
	Console.WriteLine("============================================================");
	Console.WriteLine("  TTTT   OOO   PPPP   PPPP   SSSS   DUGOUT MANAGER '96");
	Console.WriteLine("    T   O   O  P   P  P   P  S");
	Console.WriteLine("    T   O   O  PPPP   PPPP   SSS");
	Console.WriteLine("    T   O   O  P      P         S");
	Console.WriteLine("    T    OOO   P      P      SSSS");
	Console.WriteLine("============================================================");
	Console.ResetColor();

	var owned = cards.Count(c => c.IsOwned);
	var missing = cards.Count - owned;
	SetColor(ConsoleColor.DarkBlue);
	Console.WriteLine($"SCOREBOARD | OWNED {owned}/350 | MISSING {missing} | ODDS LINES {oddsEntries.Count}");
	Console.ResetColor();
	Console.WriteLine();
}

static void DrawStatsPanel(List<Card> cards, List<OddsEntry> oddsEntries, List<InsertSet> insertSets)
{
	var totalBase = cards.Count;
	var ownedBase = cards.Count(c => c.IsOwned);
	var missingBase = totalBase - ownedBase;
	var completion = totalBase == 0 ? 0 : (ownedBase * 100.0) / totalBase;
	var ownedParallels = cards.SelectMany(c => c.Variants).Where(v => v.IsOwned).ToList();
	var teamCardCount = GetPresentTeamCards(cards).Count;
	var ownedInsertSets = insertSets.Count(IsInsertOwned);
	var totalInsertSets = insertSets.Count;

	var commonHits = ownedParallels.Count(v => v.Rarity.StartsWith("Common", StringComparison.OrdinalIgnoreCase));
	var uncommonHits = ownedParallels.Count(v => v.Rarity.Equals("Uncommon", StringComparison.OrdinalIgnoreCase));
	var rareHits = ownedParallels.Count(v => v.Rarity.Equals("Rare", StringComparison.OrdinalIgnoreCase));
	var ultraRareHits = ownedParallels.Count(v => v.Rarity.Equals("Ultra Rare", StringComparison.OrdinalIgnoreCase));

	Console.WriteLine("+----------------------------------------------------------+");
	Console.WriteLine("|                    ARCADE STATS PANEL                    |");
	Console.WriteLine("+----------------------------------------------------------+");
	Console.WriteLine($"| Base Progress     : {ownedBase,3}/{totalBase,-3} ({completion,6:0.0}%)                  |");
	Console.WriteLine($"| Missing Base      : {missingBase,3}                                      |");
	Console.WriteLine($"| Parallel Hits     : {ownedParallels.Count,3}                                      |");
	Console.WriteLine($"| Rare+ Ultra Hits  : {rareHits + ultraRareHits,3}                                      |");
	Console.WriteLine($"| Breakdown         : C:{commonHits,-3} U:{uncommonHits,-3} R:{rareHits,-3} UR:{ultraRareHits,-3}                 |");
	Console.WriteLine($"| Team Cards        : {teamCardCount,3}/30                                   |");
	Console.WriteLine($"| Insert Sets       : {ownedInsertSets,3}/{totalInsertSets,-3}                                   |");
	Console.WriteLine($"| Odds Catalog Size : {oddsEntries.Count,3}                                      |");
	Console.WriteLine("+----------------------------------------------------------+");
}

static void PrintTeamCardReport(List<Card> cards)
{
	var present = GetPresentTeamCards(cards);
	var expected = GetMlbTeams();
	var missing = expected.Where(team => !present.Contains(team)).ToList();

	SetColor(ConsoleColor.Cyan);
	Console.WriteLine("Team Card Report");
	Console.ResetColor();
	Console.WriteLine($"Found {present.Count}/30 MLB team cards.");

	if (missing.Count == 0)
	{
		SetColor(ConsoleColor.Green);
		Console.WriteLine("All 30 team cards are present.");
		Console.ResetColor();
		return;
	}

	SetColor(ConsoleColor.Yellow);
	Console.WriteLine("Missing team cards:");
	Console.ResetColor();
	foreach (var team in missing)
	{
		Console.WriteLine($"- {team}");
	}
}

static void PrintOwnedParallelsReport(List<Card> cards, int? startId, int? endId, string? rarityFilter)
{
	var ownedParallels = cards
		.Where(card => !startId.HasValue || !endId.HasValue || (card.Id >= startId.Value && card.Id <= endId.Value))
		.SelectMany(card => card.Variants
			.Where(variant => variant.IsOwned && MatchesRarityFilter(variant.Rarity, rarityFilter))
			.Select(variant => new { Card = card, Variant = variant }))
		.OrderBy(item => item.Card.Id)
		.ThenBy(item => item.Variant.Name)
		.ToList();

	SetColor(ConsoleColor.Cyan);
	var filters = new List<string>();
	if (startId.HasValue && endId.HasValue)
	{
		filters.Add($"range {startId.Value}-{endId.Value}");
	}
	if (!string.IsNullOrWhiteSpace(rarityFilter))
	{
		filters.Add($"rarity {NormalizeRarityFilterLabel(rarityFilter)}");
	}
	Console.WriteLine(filters.Count == 0
		? "Owned Parallels Report"
		: $"Owned Parallels Report ({string.Join(", ", filters)})");
	Console.ResetColor();

	if (ownedParallels.Count == 0)
	{
		Console.WriteLine("No parallels logged yet.");
		return;
	}

	foreach (var item in ownedParallels)
	{
		SetColor(GetRarityColor(item.Variant.Rarity));
		Console.WriteLine($"{FormatCardLabel(item.Card),-36}  {item.Variant.Name,-20}  {item.Variant.Rarity,-11}  {item.Variant.Odds ?? "n/a"}");
		Console.ResetColor();
	}
}

static bool TryParseParallelFilters(string input, out int? startId, out int? endId, out string? rarityFilter)
{
	startId = null;
	endId = null;
	rarityFilter = null;

	if (string.IsNullOrWhiteSpace(input))
	{
		return true;
	}

	var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
	var rangeToken = tokens.FirstOrDefault(token => token.Contains('-'));
	if (rangeToken is not null)
	{
		if (!TryParseRange(rangeToken, out var parsedStart, out var parsedEnd))
		{
			return false;
		}

		startId = parsedStart;
		endId = parsedEnd;
		tokens.Remove(rangeToken);
	}

	if (tokens.Count > 0)
	{
		rarityFilter = string.Join(' ', tokens);
		if (!IsSupportedRarityFilter(rarityFilter))
		{
			return false;
		}
	}

	return true;
}

static bool IsSupportedRarityFilter(string rarityFilter)
{
	var normalized = NormalizeRarityFilterLabel(rarityFilter);
	return normalized is "common" or "common/base" or "uncommon" or "rare" or "ultra rare" or "unknown";
}

static string NormalizeRarityFilterLabel(string rarityFilter)
{
	var normalized = rarityFilter.Trim().ToLowerInvariant();
	return normalized switch
	{
		"common base" => "common/base",
		"common/base" => "common/base",
		"ultrarare" => "ultra rare",
		_ => normalized
	};
}

static bool MatchesRarityFilter(string rarity, string? rarityFilter)
{
	if (string.IsNullOrWhiteSpace(rarityFilter))
	{
		return true;
	}

	var normalizedFilter = NormalizeRarityFilterLabel(rarityFilter);
	var normalizedRarity = NormalizeRarityFilterLabel(rarity);

	if (normalizedFilter == "common")
	{
		return normalizedRarity.StartsWith("common", StringComparison.OrdinalIgnoreCase);
	}

	return normalizedRarity.Equals(normalizedFilter, StringComparison.OrdinalIgnoreCase);
}

static bool HandleInsertCommand(string argument, List<InsertSet> insertSets)
{
	if (string.IsNullOrWhiteSpace(argument))
	{
		PrintInsertSets(insertSets, null, null, null);
		return false;
	}

	var parts = argument.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
	var action = parts[0].ToLowerInvariant();
	var rest = parts.Length > 1 ? parts[1].Trim() : string.Empty;

	switch (action)
	{
		case "have":
		case "h":
			if (string.IsNullOrWhiteSpace(rest))
			{
				Console.WriteLine("Usage: inserts have <name|code|code#|code#-#>[, ...]");
				Console.WriteLine("Examples: inserts h TOG1,TOG2,TOG5-7 | inserts have Titans of the Game");
				return false;
			}

			var haveTargets = rest.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (haveTargets.Length == 0)
			{
				Console.WriteLine("No insert targets found.");
				return false;
			}

			var anyHaveChange = false;
			foreach (var target in haveTargets)
			{
				if (!TryExpandInsertTarget(insertSets, target, out var ownedSet, out var ownedNumbers, out var parseError) || ownedSet is null)
				{
					Console.WriteLine(parseError ?? $"Insert set '{target}' not found.");
					continue;
				}

				if (ownedNumbers is null)
				{
					if (ownedSet.ValidCardNumbers.Count > 0)
					{
						ownedSet.OwnedCards = ownedSet.ValidCardNumbers.Distinct().OrderBy(n => n).ToList();
					}
					else
					{
						ownedSet.IsOwned = true;
					}

					UpdateInsertOwnershipFlag(ownedSet);
					SetColor(ConsoleColor.Green);
					Console.WriteLine($"Insert set marked owned: {ownedSet.Name}");
					Console.ResetColor();
					anyHaveChange = true;
					continue;
				}

				foreach (var ownedNumber in ownedNumbers)
				{
					if (ownedNumber <= 0)
					{
						Console.WriteLine("Insert card number must be greater than 0.");
						continue;
					}

					if (ownedSet.ValidCardNumbers.Count > 0 && !ownedSet.ValidCardNumbers.Contains(ownedNumber))
					{
						Console.WriteLine($"{ownedSet.Name} [{ownedSet.Code}] does not include card #{ownedNumber} in the parsed checklist.");
						continue;
					}

					if (!ownedSet.OwnedCards.Contains(ownedNumber))
					{
						ownedSet.OwnedCards.Add(ownedNumber);
						anyHaveChange = true;
					}

					UpdateInsertOwnershipFlag(ownedSet);
					SetColor(ConsoleColor.Green);
					Console.WriteLine($"Insert card logged: {ownedSet.Name} [{ownedSet.Code}] #{ownedNumber}");
					Console.ResetColor();
				}
			}

			return anyHaveChange;

		case "unhave":
		case "u":
			if (string.IsNullOrWhiteSpace(rest))
			{
				Console.WriteLine("Usage: inserts unhave <name|code|code#|code#-#>[, ...]");
				Console.WriteLine("Examples: inserts u TOG1,TOG2,TOG5-7 | inserts unhave Titans of the Game");
				return false;
			}

			var unhaveTargets = rest.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (unhaveTargets.Length == 0)
			{
				Console.WriteLine("No insert targets found.");
				return false;
			}

			var anyUnhaveChange = false;
			foreach (var target in unhaveTargets)
			{
				if (!TryExpandInsertTarget(insertSets, target, out var unownedSet, out var unownedNumbers, out var parseError) || unownedSet is null)
				{
					Console.WriteLine(parseError ?? $"Insert set '{target}' not found.");
					continue;
				}

				if (unownedNumbers is null)
				{
					unownedSet.IsOwned = false;
					unownedSet.OwnedCards.Clear();
					SetColor(ConsoleColor.Yellow);
					Console.WriteLine($"Insert set marked missing: {unownedSet.Name}");
					Console.ResetColor();
					anyUnhaveChange = true;
					continue;
				}

				foreach (var unownedNumber in unownedNumbers)
				{
					if (!unownedSet.OwnedCards.Remove(unownedNumber))
					{
						Console.WriteLine($"Insert card #{unownedNumber} is not logged for {unownedSet.Name}.");
						continue;
					}

					UpdateInsertOwnershipFlag(unownedSet);
					SetColor(ConsoleColor.Yellow);
					Console.WriteLine($"Insert card removed: {unownedSet.Name} [{unownedSet.Code}] #{unownedNumber}");
					Console.ResetColor();
					anyUnhaveChange = true;
				}
			}

			return anyUnhaveChange;

		case "add":
			if (string.IsNullOrWhiteSpace(rest))
			{
				Console.WriteLine("Usage: inserts add <set name> | <category>");
				Console.WriteLine("Example: inserts add First Pitch | Hobby Exclusive");
				return false;
			}

			var segments = rest.Split('|', 2, StringSplitOptions.TrimEntries);
			var newName = segments[0].Trim();
			var newCategory = segments.Length > 1 ? segments[1].Trim() : "Custom";

			if (newName.Length == 0)
			{
				Console.WriteLine("Insert set name is required.");
				return false;
			}

			if (FindInsertSet(insertSets, newName) is not null)
			{
				Console.WriteLine($"Insert set '{newName}' already exists.");
				return false;
			}

			insertSets.Add(new InsertSet
			{
				Name = newName,
				Category = string.IsNullOrWhiteSpace(newCategory) ? "Custom" : newCategory,
				Code = GenerateInsertCode(newName, insertSets.Select(i => i.Code)),
				IsOwned = false
			});

			SetColor(ConsoleColor.Cyan);
			Console.WriteLine($"Insert set added: {newName} ({newCategory})");
			Console.ResetColor();
			return true;

		case "cards":
		case "card":
		case "cs":
			if (string.IsNullOrWhiteSpace(rest))
			{
				Console.WriteLine("Usage: inserts cards <insert set name|code>");
				Console.WriteLine("Examples: inserts cards TOG | inserts cards Titans of the Game");
				return false;
			}

			if (!TryResolveInsertTarget(insertSets, rest, out var cardsSet, out var cardsNumber) || cardsSet is null)
			{
				Console.WriteLine($"Insert set '{rest}' not found.");
				return false;
			}

			if (cardsNumber.HasValue)
			{
				Console.WriteLine("Use set name or code only for this command (example: inserts cards TOG).");
				return false;
			}

			PrintOwnedInsertCards(cardsSet);
			return false;

		case "help":
			Console.WriteLine("Insert Commands:");
			Console.WriteLine("  inserts                                  List all insert sets");
			Console.WriteLine("  inserts <exact code|exact name>          Show owned card details for one set");
			Console.WriteLine("  inserts missing|owned                    Filter by ownership state");
			Console.WriteLine("  inserts cat:<category text>              Filter by category");
			Console.WriteLine("  inserts find:<name text>                 Filter by insert set name");
			Console.WriteLine("  inserts missing cat:Retail find:Titans   Combine filters");
			Console.WriteLine("  inserts cards <name|code>                Show full owned card numbers for one set");
			Console.WriteLine("  inserts have <name|code>                 Mark full insert set owned");
			Console.WriteLine("  inserts have <code#|code#-#>[, ...]      Mark one or many insert cards (ex: GH13, TOG1-3)");
			Console.WriteLine("  inserts unhave <name|code|code#|code#-#>[, ...]  Remove full set or one/many cards");
			Console.WriteLine("  inserts add <name> | <category> Add a custom insert set");
			return false;

		default:
			if (!argument.Contains(':') &&
				TryResolveInsertTarget(insertSets, argument, out var directSet, out var directNumber) &&
				directSet is not null &&
				!directNumber.HasValue)
			{
				var isExactName = argument.Equals(directSet.Name, StringComparison.OrdinalIgnoreCase);
				var isExactCode = NormalizeInsertCode(argument).Equals(NormalizeInsertCode(directSet.Code), StringComparison.OrdinalIgnoreCase);

				if (isExactName || isExactCode)
				{
					PrintOwnedInsertCards(directSet);
					return false;
				}
			}

			if (!TryParseInsertFilters(argument, out var ownershipFilter, out var categoryFilter, out var nameFilter))
			{
				Console.WriteLine("Invalid inserts filter. Try: inserts help");
				return false;
			}

			PrintInsertSets(insertSets, ownershipFilter, categoryFilter, nameFilter);
			return false;
	}
}

static bool TryParseInsertFilters(string input, out bool? ownershipFilter, out string? categoryFilter, out string? nameFilter)
{
	ownershipFilter = null;
	categoryFilter = null;
	nameFilter = null;

	if (string.IsNullOrWhiteSpace(input))
	{
		return true;
	}

	var remaining = input.Trim();
	var firstToken = remaining.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();
	if (firstToken is "all" or "owned" or "missing")
	{
		ownershipFilter = firstToken switch
		{
			"owned" => true,
			"missing" => false,
			_ => null
		};

		remaining = remaining.Length == firstToken.Length
			? string.Empty
			: remaining.Substring(firstToken.Length).Trim();
	}

	if (remaining.Length == 0)
	{
		return true;
	}

	var keyMatches = Regex.Matches(
		remaining,
		@"(?i)\b(?<key>cat|category|find|name):(?<value>[^:]+?)(?=\s+\b(?:cat|category|find|name):|$)");

	foreach (Match match in keyMatches)
	{
		var key = match.Groups["key"].Value.ToLowerInvariant();
		var value = match.Groups["value"].Value.Trim();
		if (value.Length == 0)
		{
			return false;
		}

		if (key is "cat" or "category")
		{
			categoryFilter = value;
		}
		else
		{
			nameFilter = value;
		}
	}

	if (keyMatches.Count > 0)
	{
		var stripped = Regex.Replace(
			remaining,
			@"(?i)\b(?:cat|category|find|name):[^:]+?(?=\s+\b(?:cat|category|find|name):|$)",
			" ").Trim();

		return stripped.Length == 0;
	}

	if (remaining.StartsWith("category ", StringComparison.OrdinalIgnoreCase) ||
		remaining.StartsWith("cat ", StringComparison.OrdinalIgnoreCase))
	{
		var split = remaining.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
		if (split.Length < 2 || split[1].Trim().Length == 0)
		{
			return false;
		}

		categoryFilter = split[1].Trim();
		return true;
	}

	if (remaining.StartsWith("find ", StringComparison.OrdinalIgnoreCase) ||
		remaining.StartsWith("name ", StringComparison.OrdinalIgnoreCase) ||
		remaining.StartsWith("search ", StringComparison.OrdinalIgnoreCase))
	{
		var split = remaining.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
		if (split.Length < 2 || split[1].Trim().Length == 0)
		{
			return false;
		}

		nameFilter = split[1].Trim();
		return true;
	}

	// Bare text defaults to name search for convenience.
	nameFilter = remaining;
	return true;
}

static void PrintInsertSets(List<InsertSet> insertSets, bool? ownershipFilter, string? categoryFilter, string? nameFilter)
{
	var rows = insertSets
		.Where(i => !ownershipFilter.HasValue || IsInsertOwned(i) == ownershipFilter.Value)
		.Where(i => string.IsNullOrWhiteSpace(categoryFilter) || i.Category.Contains(categoryFilter, StringComparison.OrdinalIgnoreCase))
		.Where(i => string.IsNullOrWhiteSpace(nameFilter) || i.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase) || i.Code.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
		.OrderBy(i => i.Category)
		.ThenBy(i => i.Name)
		.ToList();

	SetColor(ConsoleColor.Cyan);
	var titleFilters = new List<string>();
	if (ownershipFilter.HasValue)
	{
		titleFilters.Add(ownershipFilter.Value ? "owned" : "missing");
	}
	if (!string.IsNullOrWhiteSpace(categoryFilter))
	{
		titleFilters.Add($"category contains '{categoryFilter}'");
	}
	if (!string.IsNullOrWhiteSpace(nameFilter))
	{
		titleFilters.Add($"name contains '{nameFilter}'");
	}

	Console.WriteLine(titleFilters.Count == 0
		? "Insert Set Checklist"
		: $"Insert Set Checklist ({string.Join(", ", titleFilters)})");
	Console.ResetColor();

	if (rows.Count == 0)
	{
		Console.WriteLine("No insert sets match that filter.");
		return;
	}

	foreach (var item in rows)
	{
		var progress = GetInsertProgress(item);
		var status = progress.IsComplete ? "OWNED" : progress.OwnedCount > 0 ? "PARTIAL" : "MISSING";
		SetColor(progress.IsComplete ? ConsoleColor.Green : progress.OwnedCount > 0 ? ConsoleColor.DarkYellow : ConsoleColor.Red);
		var checklistSummary = item.ValidCardNumbers.Count == 0 ? string.Empty : $"  checklist:{item.ValidCardNumbers.Count}";
		var progressSummary = item.ValidCardNumbers.Count == 0 ? string.Empty : $"  progress:{progress.OwnedCount}/{progress.TotalCount}";
		var cardsSummary = item.OwnedCards.Count == 0
			? ""
			: $"  cards:{string.Join(',', item.OwnedCards.OrderBy(n => n).Take(10))}{(item.OwnedCards.Count > 10 ? ",..." : "")}";
		Console.WriteLine($"[{status,-7}] [{item.Code,-4}] {item.Category,-31}  {item.Name}{checklistSummary}{progressSummary}{cardsSummary}");
		Console.ResetColor();
	}
}

static void PrintOwnedInsertCards(InsertSet set)
{
	var ownedCards = set.OwnedCards.Where(n => n > 0).Distinct().OrderBy(n => n).ToList();
	var nameByNumber = (set.ValidCards ?? new List<InsertCardInfo>())
		.Where(c => c.Number > 0)
		.GroupBy(c => c.Number)
		.ToDictionary(g => g.Key, g => g.First().Name ?? string.Empty);

	SetColor(ConsoleColor.Cyan);
	Console.WriteLine($"Owned Insert Cards: {set.Name} [{set.Code}]");
	Console.ResetColor();

	var totalChecklist = set.ValidCardNumbers.Count;
	var ownedCount = ownedCards.Count;
	var isComplete = totalChecklist > 0
		? ownedCount >= totalChecklist
		: set.IsOwned;
	var isPartial = !isComplete && totalChecklist > 0 && ownedCount > 0;

	if (isComplete)
	{
		SetColor(ConsoleColor.Green);
		Console.WriteLine("Set marked owned: YES");
		Console.ResetColor();
	}
	else if (isPartial)
	{
		SetColor(ConsoleColor.DarkYellow);
		Console.WriteLine($"Set progress: {ownedCount}/{totalChecklist}");
		Console.ResetColor();
	}
	else
	{
		SetColor(ConsoleColor.Red);
		Console.WriteLine(totalChecklist > 0 ? $"Set progress: 0/{totalChecklist}" : "Set marked owned: NO");
		Console.ResetColor();
	}

	if (ownedCards.Count == 0)
	{
		if (isComplete)
		{
			Console.WriteLine("All cards are considered owned for this set.");
		}
		else
		{
			Console.WriteLine("No owned cards logged for this set yet.");
		}

		if (set.ValidCardNumbers.Count > 0)
		{
			Console.WriteLine($"Checklist size: {set.ValidCardNumbers.Count}");
		}
		return;
	}

	foreach (var number in ownedCards)
	{
		var cardName = nameByNumber.TryGetValue(number, out var foundName) && !string.IsNullOrWhiteSpace(foundName)
			? foundName
			: $"Card {number}";
		Console.WriteLine($"#{number,3}  {cardName}");
	}

	if (set.ValidCardNumbers.Count > 0)
	{
		Console.WriteLine($"Checklist size: {set.ValidCardNumbers.Count}");
	}
}

static void UpdateInsertOwnershipFlag(InsertSet set)
{
	var ownedDistinct = set.OwnedCards.Where(n => n > 0).Distinct().ToHashSet();
	if (set.ValidCardNumbers.Count > 0)
	{
		set.IsOwned = set.ValidCardNumbers.All(ownedDistinct.Contains);
		return;
	}

	set.IsOwned = set.IsOwned || ownedDistinct.Count > 0;
}

static bool IsInsertOwned(InsertSet set)
{
	return GetInsertProgress(set).IsComplete;
}

static (int OwnedCount, int TotalCount, bool IsComplete) GetInsertProgress(InsertSet set)
{
	var ownedCount = set.OwnedCards.Where(n => n > 0).Distinct().Count();
	var totalCount = set.ValidCardNumbers.Count;

	if (totalCount > 0)
	{
		return (ownedCount, totalCount, ownedCount >= totalCount);
	}

	return (ownedCount, totalCount, set.IsOwned);
}

static bool TryResolveInsertTarget(List<InsertSet> insertSets, string input, out InsertSet? set, out int? number)
{
	set = null;
	number = null;

	var byName = insertSets.FirstOrDefault(i => i.Name.Equals(input, StringComparison.OrdinalIgnoreCase));
	if (byName is not null)
	{
		set = byName;
		return true;
	}

	var normalizedInput = NormalizeInsertCode(input);
	if (normalizedInput.Length == 0)
	{
		return false;
	}

	var byCode = insertSets.FirstOrDefault(i => NormalizeInsertCode(i.Code).Equals(normalizedInput, StringComparison.OrdinalIgnoreCase));
	if (byCode is not null)
	{
		set = byCode;
		return true;
	}

	var match = insertSets
		.Select(i => new { Set = i, Code = NormalizeInsertCode(i.Code) })
		.Where(x => x.Code.Length > 0 && normalizedInput.StartsWith(x.Code, StringComparison.OrdinalIgnoreCase))
		.OrderByDescending(x => x.Code.Length)
		.FirstOrDefault();

	if (match is null)
	{
		return false;
	}

	var suffix = normalizedInput.Substring(match.Code.Length);
	if (suffix.Length == 0)
	{
		set = match.Set;
		return true;
	}

	if (!int.TryParse(suffix, out var parsedNumber))
	{
		return false;
	}

	set = match.Set;
	number = parsedNumber;
	return true;
}

static bool TryExpandInsertTarget(
	List<InsertSet> insertSets,
	string input,
	out InsertSet? set,
	out List<int>? numbers,
	out string? error)
{
	set = null;
	numbers = null;
	error = null;

	if (string.IsNullOrWhiteSpace(input))
	{
		error = "Insert target is empty.";
		return false;
	}

	var target = input.Trim();
	var rangeParts = target.Split('-', StringSplitOptions.TrimEntries);
	if (rangeParts.Length == 2)
	{
		if (!TryResolveInsertTarget(insertSets, rangeParts[0], out var rangeSet, out var startNumber) || rangeSet is null || !startNumber.HasValue)
		{
			error = $"Invalid insert range '{target}'. Use code#-# (example: TOG1-5).";
			return false;
		}

		if (!int.TryParse(rangeParts[1], out var endNumber))
		{
			error = $"Invalid insert range '{target}'. End must be a number.";
			return false;
		}

		if (endNumber <= 0)
		{
			error = "Insert card number must be greater than 0.";
			return false;
		}

		var start = Math.Min(startNumber.Value, endNumber);
		var end = Math.Max(startNumber.Value, endNumber);
		set = rangeSet;
		numbers = Enumerable.Range(start, end - start + 1).ToList();
		return true;
	}

	if (TryResolveInsertTarget(insertSets, target, out var resolvedSet, out var resolvedNumber) && resolvedSet is not null)
	{
		set = resolvedSet;
		numbers = resolvedNumber.HasValue ? new List<int> { resolvedNumber.Value } : null;
		return true;
	}

	error = $"Insert set '{target}' not found.";
	return false;
}

static InsertSet? FindInsertSet(List<InsertSet> insertSets, string name)
{
	var byName = insertSets.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
	if (byName is not null)
	{
		return byName;
	}

	var normalized = NormalizeInsertCode(name);
	var byCode = insertSets.FirstOrDefault(i =>
		NormalizeInsertCode(i.Code).Equals(normalized, StringComparison.OrdinalIgnoreCase));
	if (byCode is not null)
	{
		return byCode;
	}

	var withTrailingNumber = Regex.Match(normalized, @"^(?<prefix>[A-Z]+)(?<number>\d+)$");
	if (withTrailingNumber.Success)
	{
		var prefix = withTrailingNumber.Groups["prefix"].Value;
		return insertSets.FirstOrDefault(i =>
			NormalizeInsertCode(i.Code).Equals(prefix, StringComparison.OrdinalIgnoreCase));
	}

	return null;
}

static List<InsertSet> CreateDefaultInsertSets()
{
	return GetDefaultInsertSetDefinitions()
		.Select(d => new InsertSet { Name = d.Name, Category = d.Category, Code = d.Code, IsOwned = false })
		.ToList();
}

static List<(string Name, string Category, string Code)> GetDefaultInsertSetDefinitions()
{
	return new List<(string Name, string Category, string Code)>
	{
		("2025 All Topps Team", "Standard Inserts", "ATT"),
		("Topps Profiles", "Standard Inserts", "TP"),
		("Big Ticket Players", "Standard Inserts", "BTP"),
		("2025's Greatest Hits", "Standard Inserts", "GH"),
		("First Pitch", "Standard Inserts", "FP"),
		("Cover Athletes Cards", "Standard Inserts", "CAC"),
		("All Aces", "Standard Inserts", "AA"),
		("All Kings", "Standard Inserts", "AK"),
		("1991 Topps Baseball", "Standard Inserts", "T91"),
		("Stars of MLB", "Retail Exclusive Inserts", "SMLB"),
		("Titans of the Game", "Retail Exclusive Inserts", "TOG"),
		("Dugout Peeks", "Celebration Mega Box Exclusive Inserts", "DP"),
		("Walk This Way", "Celebration Mega Box Exclusive Inserts", "WTW"),
		("Mascots", "Celebration Mega Box Exclusive Inserts", "MAS"),
		("Gameday Drip", "Celebration Mega Box Exclusive Inserts", "GD"),
		("Perennial All Stars", "Celebration Mega Box Exclusive Inserts", "PAS"),
		("8 Bit Ballers", "Celebration Mega Box Exclusive Inserts", "8BB"),
		("Hidden Mascots", "Celebration Mega Box Exclusive Inserts", "HM"),
		("Oversized 2026 Topps Baseball Cards", "Super Box and Specialty Exclusives", "O26"),
		("Companion Cards", "Super Box and Specialty Exclusives", "CC"),
		("Funko Base Cards", "Super Box and Specialty Exclusives", "FBC"),
		("1991 Topps Baseball Chrome Base Cards", "Super Box and Specialty Exclusives", "T91C"),
		("Iconic Topps Buyback Cards", "Super Box and Specialty Exclusives", "ITB"),
		("75 Years of Topps Baseball Gifts", "Super Box and Specialty Exclusives", "75Y")
	};
}

static bool NormalizeInsertSets(List<InsertSet> insertSets)
{
	var changed = false;
	var defaults = GetDefaultInsertSetDefinitions().ToDictionary(d => d.Name, d => d.Code, StringComparer.OrdinalIgnoreCase);
	var usedCodes = new HashSet<string>(
		insertSets.Select(i => NormalizeInsertCode(i.Code)).Where(c => !string.IsNullOrWhiteSpace(c)),
		StringComparer.OrdinalIgnoreCase);

	foreach (var definition in GetDefaultInsertSetDefinitions())
	{
		var hasByName = insertSets.Any(i => i.Name.Equals(definition.Name, StringComparison.OrdinalIgnoreCase));
		var hasByCode = insertSets.Any(i => NormalizeInsertCode(i.Code).Equals(NormalizeInsertCode(definition.Code), StringComparison.OrdinalIgnoreCase));
		if (hasByName || hasByCode)
		{
			continue;
		}

		insertSets.Add(new InsertSet
		{
			Name = definition.Name,
			Category = definition.Category,
			Code = definition.Code,
			IsOwned = false
		});

		usedCodes.Add(NormalizeInsertCode(definition.Code));
		changed = true;
	}

	foreach (var item in insertSets)
	{
		item.OwnedCards ??= new List<int>();
		item.ValidCardNumbers ??= new List<int>();
		item.ValidCards ??= new List<InsertCardInfo>();

		var normalizedOwnedCards = item.OwnedCards.Where(n => n > 0).Distinct().OrderBy(n => n).ToList();
		if (normalizedOwnedCards.Count != item.OwnedCards.Count || !normalizedOwnedCards.SequenceEqual(item.OwnedCards))
		{
			item.OwnedCards = normalizedOwnedCards;
			changed = true;
		}

		var normalizedValidCards = item.ValidCardNumbers.Where(n => n > 0).Distinct().OrderBy(n => n).ToList();
		if (normalizedValidCards.Count != item.ValidCardNumbers.Count || !normalizedValidCards.SequenceEqual(item.ValidCardNumbers))
		{
			item.ValidCardNumbers = normalizedValidCards;
			changed = true;
		}

		var normalizedValidCardEntries = item.ValidCards
			.Where(v => v.Number > 0)
			.GroupBy(v => v.Number)
			.Select(g => new InsertCardInfo
			{
				Number = g.Key,
				Name = NormalizeInsertCardName(g.First().Name)
			})
			.OrderBy(v => v.Number)
			.ToList();

		if (normalizedValidCardEntries.Count != item.ValidCards.Count ||
			!normalizedValidCardEntries.Select(v => (v.Number, v.Name)).SequenceEqual(item.ValidCards.Select(v => (v.Number, v.Name))))
		{
			item.ValidCards = normalizedValidCardEntries;
			changed = true;
		}

		var wasOwned = item.IsOwned;
		UpdateInsertOwnershipFlag(item);
		if (wasOwned != item.IsOwned)
		{
			changed = true;
		}

		if (string.IsNullOrWhiteSpace(item.Code))
		{
			if (defaults.TryGetValue(item.Name, out var defaultCode))
			{
				item.Code = defaultCode;
			}
			else
			{
				item.Code = GenerateInsertCode(item.Name, usedCodes);
			}

			usedCodes.Add(NormalizeInsertCode(item.Code));
			changed = true;
		}
	}

	return changed;
}

static string NormalizeInsertCode(string? code)
{
	if (string.IsNullOrWhiteSpace(code))
	{
		return string.Empty;
	}

	return Regex.Replace(code.ToUpperInvariant(), @"[^A-Z0-9]", string.Empty);
}

static string GenerateInsertCode(string name, IEnumerable<string> existingCodes)
{
	var existing = new HashSet<string>(existingCodes.Select(NormalizeInsertCode), StringComparer.OrdinalIgnoreCase);
	var words = Regex.Split(name, @"[^A-Za-z0-9]+").Where(w => w.Length > 0).ToList();

	var baseCode = words.Count == 0
		? "SET"
		: string.Concat(words.Select(w => char.ToUpperInvariant(w[0])));

	baseCode = NormalizeInsertCode(baseCode);
	if (baseCode.Length == 0)
	{
		baseCode = "SET";
	}

	if (!existing.Contains(baseCode))
	{
		return baseCode;
	}

	var index = 2;
	while (existing.Contains($"{baseCode}{index}"))
	{
		index++;
	}

	return $"{baseCode}{index}";
}

static HashSet<string> GetPresentTeamCards(List<Card> cards)
{
	var expected = GetMlbTeams();
	var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	foreach (var card in cards)
	{
		var name = card.PlayerName;
		if (TryNormalizeTeamName(name, out var normalizedTeamName))
		{
			name = normalizedTeamName;
		}

		if (expected.Contains(name, StringComparer.OrdinalIgnoreCase))
		{
			present.Add(name);
		}
	}

	return present;
}

static bool TryNormalizeTeamName(string value, out string normalized)
{
	normalized = value.Trim();

	if (normalized.Equals("Athletics", StringComparison.OrdinalIgnoreCase) ||
		normalized.Equals("A's", StringComparison.OrdinalIgnoreCase))
	{
		normalized = "Oakland Athletics";
		return true;
	}

	return false;
}

static List<string> GetMlbTeams()
{
	return new List<string>
	{
		"Arizona Diamondbacks",
		"Atlanta Braves",
		"Baltimore Orioles",
		"Boston Red Sox",
		"Chicago Cubs",
		"Chicago White Sox",
		"Cincinnati Reds",
		"Cleveland Guardians",
		"Colorado Rockies",
		"Detroit Tigers",
		"Houston Astros",
		"Kansas City Royals",
		"Los Angeles Angels",
		"Los Angeles Dodgers",
		"Miami Marlins",
		"Milwaukee Brewers",
		"Minnesota Twins",
		"New York Mets",
		"New York Yankees",
		"Oakland Athletics",
		"Philadelphia Phillies",
		"Pittsburgh Pirates",
		"San Diego Padres",
		"San Francisco Giants",
		"Seattle Mariners",
		"St. Louis Cardinals",
		"Tampa Bay Rays",
		"Texas Rangers",
		"Toronto Blue Jays",
		"Washington Nationals"
	};
}

static void PrintOddsBoard(List<OddsEntry> oddsEntries)
{
	if (oddsEntries.Count == 0)
	{
		Console.WriteLine("No odds entries loaded.");
		return;
	}

	SetColor(ConsoleColor.Cyan);
	Console.WriteLine("Odds Board (Top 25 easiest pulls)");
	Console.ResetColor();

	foreach (var entry in oddsEntries.Take(25))
	{
		SetColor(GetRarityColor(entry.Rarity));
		Console.WriteLine($"{entry.Name,-38} {entry.OddsText,8}  {entry.Rarity}");
		Console.ResetColor();
	}
}

static void PrintHelp()
{
	Console.WriteLine("Commands:");
	Console.WriteLine("  have|h <ids>             Mark base cards owned (example: have 1, 5, 10-15)");
	Console.WriteLine("  missing|m -a             Show all missing base cards");
	Console.WriteLine("  missing|m [start-end]    Show missing base cards in a range (example: missing 25-80)");
	Console.WriteLine("  list|roster              Show base roster + parallel counts");
	Console.WriteLine("  check|c <cardId>         Show whether a specific base card is owned");
	Console.WriteLine("  hit <cardId> <parallel>  Log a parallel/hit for a card");
	Console.WriteLine("  unhit|uh <cardId> <parallel>  Remove a logged parallel/hit");
	Console.WriteLine("  parallels|p [range] [rarity]  Show owned parallels (example: parallels 1-100 rare)");
	Console.WriteLine("  inserts [filters|subcommand]  Insert checklist (examples: inserts h GH13, inserts h TOG1)");
	Console.WriteLine("  stats                    Show Arcade Stats Panel");
	Console.WriteLine("  teams                    Show MLB team card coverage report");
	Console.WriteLine("  odds                     Show parsed odds board");
	Console.WriteLine("  panel on|off             Auto-show stats panel after updates");
	Console.WriteLine("  help                     Show commands");
	Console.WriteLine("  quit|q|exit              Save and quit");
	Console.WriteLine();
}

static ConsoleColor GetRarityColor(string rarity)
{
	if (rarity.Equals("Ultra Rare", StringComparison.OrdinalIgnoreCase))
	{
		return ConsoleColor.Magenta;
	}

	if (rarity.Equals("Rare", StringComparison.OrdinalIgnoreCase))
	{
		return ConsoleColor.Yellow;
	}

	if (rarity.Equals("Uncommon", StringComparison.OrdinalIgnoreCase))
	{
		return ConsoleColor.Cyan;
	}

	if (rarity.StartsWith("Common", StringComparison.OrdinalIgnoreCase))
	{
		return ConsoleColor.Green;
	}

	return ConsoleColor.Gray;
}

static void SetColor(ConsoleColor color)
{
	Console.ForegroundColor = color;
}

static void NormalizeCards(List<Card> cards)
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

static string FormatCardLabel(Card card)
{
	return $"#{card.Id,3}  {card.PlayerName}";
}

static void SaveCards(string path, List<Card> cards, JsonSerializerOptions options)
{
	var file = new CollectionFile { Cards = cards };
	var json = JsonSerializer.Serialize(file, options);
	AtomicSave(path, json);
}

static void SaveInsertSets(string path, List<InsertSet> insertSets, JsonSerializerOptions options)
{
	var file = new InsertSetFile { InsertSets = insertSets };
	var json = JsonSerializer.Serialize(file, options);
	AtomicSave(path, json);
}

static void AtomicSave(string path, string json)
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

static HashSet<int> ParseIds(string input)
{
	var ids = new HashSet<int>();
	var tokens = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	foreach (var token in tokens)
	{
		if (token.Contains('-'))
		{
			var rangeParts = token.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (rangeParts.Length != 2)
			{
				continue;
			}

			if (!int.TryParse(rangeParts[0], out var start) || !int.TryParse(rangeParts[1], out var end))
			{
				continue;
			}

			if (start > end)
			{
				(start, end) = (end, start);
			}

			for (var id = start; id <= end; id++)
			{
				ids.Add(id);
			}
		}
		else if (int.TryParse(token, out var singleId))
		{
			ids.Add(singleId);
		}
	}

	return ids;
}

static bool TryParseRange(string input, out int start, out int end)
{
	start = 0;
	end = 0;

	var rangeParts = input.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	if (rangeParts.Length != 2)
	{
		return false;
	}

	if (!int.TryParse(rangeParts[0], out start) || !int.TryParse(rangeParts[1], out end))
	{
		return false;
	}

	if (start > end)
	{
		(start, end) = (end, start);
	}

	return true;
}
