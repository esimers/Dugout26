using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

public static class ChecklistPdfParser
{
	public static List<Card> InitializeFromPdf(string checklistPdfPath)
	{
		var cards = Enumerable.Range(1, 700)
			.Select(id => new Card { Id = id, PlayerName = $"Card {id}", IsOwned = false })
			.ToList();

		if (!TryExtractChecklistNames(checklistPdfPath, out var namesById))
		{
			Console.WriteLine($"Checklist names unavailable from {checklistPdfPath}. Using placeholder names.");
			return cards;
		}

		foreach (var card in cards)
		{
			if (namesById.TryGetValue(card.Id, out var playerName))
			{
				card.PlayerName = playerName;
			}
		}

		Console.WriteLine($"Initialized 700 cards from PDF. Matched names: {namesById.Count}.");

		return cards;
	}

	public static bool HydratePlaceholderNamesFromChecklist(List<Card> cards, string checklistPdfPath)
	{
		var placeholders = cards.Where(c => IsPlaceholderName(c.PlayerName, c.Id)).ToList();
		if (placeholders.Count == 0)
		{
			return false;
		}

		if (!TryExtractChecklistNames(checklistPdfPath, out var namesById))
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

	public static bool HydrateSeriesTwoCards(List<Card> cards, string checklistPdfPath)
	{
		const int Series2Start = 351;
		const int Series2End = 700;

		var existingIds = new HashSet<int>(cards.Select(c => c.Id));
		var newCards = Enumerable.Range(Series2Start, Series2End - Series2Start + 1)
			.Where(id => !existingIds.Contains(id))
			.Select(id => new Card { Id = id, PlayerName = $"Card {id}", IsOwned = false })
			.ToList();

		cards.AddRange(newCards);
		var added = newCards.Count;
		var updated = 0;

		if (TryExtractChecklistNames(checklistPdfPath, out var namesById))
		{
			var placeholders = cards
				.Where(c => c.Id >= Series2Start && c.Id <= Series2End && IsPlaceholderName(c.PlayerName, c.Id))
				.ToList();

			foreach (var card in placeholders)
			{
				if (namesById.TryGetValue(card.Id, out var playerName) && !IsPlaceholderName(playerName, card.Id))
				{
					card.PlayerName = playerName;
					updated++;
				}
			}
		}

		if (added > 0)
			Console.WriteLine($"Series 2: added {added} cards (351-700).");
		if (updated > 0)
			Console.WriteLine($"Series 2: hydrated {updated} player names from checklist.");

		return added > 0 || updated > 0;
	}

	public static bool HydrateInsertSetNumbersFromChecklist(List<InsertSet> insertSets, string checklistPdfPath)
	{
		if (!TryExtractInsertSetCardsFromChecklist(checklistPdfPath, out var cardsByCode))
		{
			return false;
		}

		var changed = false;
		foreach (var set in insertSets)
		{
			var code = CommandHandler.NormalizeInsertCode(set.Code);
			set.ValidCardNumbers ??= new List<int>();
			set.ValidCards ??= new List<InsertCardInfo>();

			if (!cardsByCode.TryGetValue(code, out var cards))
			{
				continue;
			}

			var ordered = cards.Keys.OrderBy(n => n).ToList();
			var existingNumbers = new HashSet<int>(set.ValidCardNumbers);
			var newNumbers = ordered.Where(n => !existingNumbers.Contains(n)).ToList();
			if (newNumbers.Count > 0)
			{
				set.ValidCardNumbers = set.ValidCardNumbers.Concat(newNumbers).OrderBy(n => n).ToList();
				changed = true;
			}

			var orderedCards = cards
				.OrderBy(kvp => kvp.Key)
				.Select(kvp => new InsertCardInfo { Number = kvp.Key, Name = kvp.Value })
				.ToList();

			var existingCardNumbers = new HashSet<int>(set.ValidCards.Select(c => c.Number));
			var newCardEntries = orderedCards.Where(c => !existingCardNumbers.Contains(c.Number)).ToList();
			if (newCardEntries.Count > 0)
			{
				set.ValidCards = set.ValidCards.Concat(newCardEntries).OrderBy(c => c.Number).ToList();
				changed = true;
			}
		}

		return changed;
	}

	public static bool TryExtractChecklistNames(string checklistPdfPath, out Dictionary<int, string> namesById)
	{
		namesById = new Dictionary<int, string>();

		if (!File.Exists(checklistPdfPath))
		{
			Console.WriteLine($"Checklist PDF not found: {checklistPdfPath}.");
			return false;
		}

		try
		{
			var allText = new StringBuilder();
			using var document = PdfDocument.Open(checklistPdfPath);
			foreach (var page in document.GetPages())
			{
				allText.AppendLine(page.Text);
			}

			namesById = ExtractChecklistNamesFromText(allText.ToString());
			if (namesById.Count > 0)
			{
				return true;
			}

			if (TryReadTextWithPdftotext(checklistPdfPath, out var pdftotextOutput))
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

			if (TryReadTextWithPdftotext(checklistPdfPath, out var pdftotextOutput))
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

	public static Dictionary<int, string> ExtractChecklistNamesFromText(string text)
	{
		var namesById = new Dictionary<int, string>();
		var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
		var linePattern = new Regex(@"^\s*(?<id>(?:[1-9]|[1-9]\d|[1-6]\d{2}|700))\s+(?<name>.+?)\s*$", RegexOptions.Compiled);

		foreach (var rawLine in lines)
		{
			var line = Regex.Replace(rawLine.Trim(), @"\s+", " ");
			var match = linePattern.Match(line);
			if (!match.Success)
			{
				continue;
			}

			if (!int.TryParse(match.Groups["id"].Value, out var id) || id < 1 || id > 700)
			{
				continue;
			}

			var candidate = match.Groups["name"].Value.Trim();
			candidate = Regex.Replace(candidate, @"\s+", " ");
			candidate = candidate.Replace("®", string.Empty).Replace("™", string.Empty).Trim();
			if (StatsRenderer.TryNormalizeTeamName(candidate, out var normalizedTeamName))
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

	public static bool TryReadTextWithPdftotext(string pdfPath, out string output)
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

			startInfo.ArgumentList.Add(pdfPath);
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

	public static bool TryExtractInsertSetCardsFromChecklist(string checklistPdfPath, out Dictionary<string, Dictionary<int, string>> cardsByCode)
	{
		cardsByCode = new Dictionary<string, Dictionary<int, string>>(StringComparer.OrdinalIgnoreCase);

		if (!TryReadTextWithPdftotext(checklistPdfPath, out var text))
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
				var prefix = CommandHandler.NormalizeInsertCode(prefixed.Groups["prefix"].Value);
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

	public static bool IsAllowedPrefixForSet(string currentCode, string prefix)
	{
		var normalizedCode = CommandHandler.NormalizeInsertCode(currentCode);
		if (prefix.Equals(normalizedCode, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return normalizedCode switch
		{
			"8BB" => prefix.Equals("8B", StringComparison.OrdinalIgnoreCase),
			"T91C" => prefix.Equals("91C", StringComparison.OrdinalIgnoreCase),
			"MAS" => prefix.Equals("M", StringComparison.OrdinalIgnoreCase),
			"O91" => prefix.Equals("91O", StringComparison.OrdinalIgnoreCase),
			"CAC" => prefix.Equals("CA", StringComparison.OrdinalIgnoreCase),
			_ => false
		};
	}

	public static void SetInsertCardName(Dictionary<int, string> cardsByNumber, int number, string candidateName)
	{
		if (!cardsByNumber.TryGetValue(number, out var existing) || string.IsNullOrWhiteSpace(existing))
		{
			cardsByNumber[number] = candidateName;
		}
	}

	public static string NormalizeInsertCardName(string value)
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

	public static bool IsLikelyInsertCardName(string value)
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

	public static bool IsLikelyTeamName(string value)
	{
		var cleaned = NormalizeInsertCardName(value);
		if (cleaned.Length == 0)
		{
			return false;
		}

		if (StatsRenderer.GetMlbTeams().Any(team => cleaned.Equals(team, StringComparison.OrdinalIgnoreCase)))
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

	public static Dictionary<string, string> GetInsertHeadingToCodeMap()
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
			[NormalizeHeading("75 YEARS OF TOPPS BASEBALL GIFTS")] = "75Y",
			[NormalizeHeading("HEAVY LUMBER")] = "HL",
			[NormalizeHeading("HOME FIELD")] = "HA",
			[NormalizeHeading("CROOKED NUMBERS")] = "CN",
			[NormalizeHeading("GLOVE WORK")] = "GW",
			[NormalizeHeading("HIGHLIGHT REELS")] = "HR",
			[NormalizeHeading("OVERSIZED 1991 TOPPS BASEBALL")] = "O91",
			[NormalizeHeading("SWINGING WITH THE STARS")] = "SWS",
			[NormalizeHeading("HOME FIELD FANFEST")] = "HAFF",
			[NormalizeHeading("FUNKO POP")] = "FPOP",
			[NormalizeHeading("BULK ORDER")] = "BO",
			[NormalizeHeading("THE FLAGSHIP COLLECTION BASE CARDS")] = "TFC",
			[NormalizeHeading("THE FLAGSHIP COLLECTION CHROME BASE CARDS")] = "TFCC",
			[NormalizeHeading("DIAMOND DUST")] = "DD",
			[NormalizeHeading("IN THE NAME RELICS")] = "ITN",
		};
	}

	public static string NormalizeHeading(string value)
	{
		var upper = value.ToUpperInvariant();
		upper = upper.Replace("'", string.Empty);
		upper = Regex.Replace(upper, @"[^A-Z0-9 ]", " ");
		upper = Regex.Replace(upper, @"\s+", " ").Trim();
		return upper;
	}

	public static bool IsLikelyPlayerName(string value)
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
		if (StatsRenderer.TryNormalizeTeamName(value, out _))
		{
			return true;
		}

		if (words.Length < 2 || words.Length > 5)
		{
			return false;
		}

		return words.All(w => Regex.IsMatch(w, @"^[A-Za-zÀ-ÖØ-öø-ÿ.'\-]+$"));
	}

	public static bool IsPlaceholderName(string playerName, int id)
	{
		return playerName.Equals($"Card {id}", StringComparison.OrdinalIgnoreCase);
	}
}
