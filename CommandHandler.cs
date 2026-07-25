using System.Text.RegularExpressions;

public static class CommandHandler
{
	public static bool HandleInsertCommand(string argument, List<InsertSet> insertSets)
	{
		if (string.IsNullOrWhiteSpace(argument))
		{
			ConsoleUi.PrintInsertSets(insertSets, null, null, null);
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
						ConsoleUi.SetColor(ConsoleColor.Green);
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
						ConsoleUi.SetColor(ConsoleColor.Green);
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
						ConsoleUi.SetColor(ConsoleColor.Yellow);
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
						ConsoleUi.SetColor(ConsoleColor.Yellow);
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

				ConsoleUi.SetColor(ConsoleColor.Cyan);
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

				ConsoleUi.PrintOwnedInsertCards(cardsSet);
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
						ConsoleUi.PrintOwnedInsertCards(directSet);
						return false;
					}
				}

				if (!TryParseInsertFilters(argument, out var ownershipFilter, out var categoryFilter, out var nameFilter))
				{
					Console.WriteLine("Invalid inserts filter. Try: inserts help");
					return false;
				}

				ConsoleUi.PrintInsertSets(insertSets, ownershipFilter, categoryFilter, nameFilter);
				return false;
		}
	}

	public static bool TryParseInsertFilters(string input, out bool? ownershipFilter, out string? categoryFilter, out string? nameFilter)
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

	public static void UpdateInsertOwnershipFlag(InsertSet set)
	{
		var ownedDistinct = set.OwnedCards.Where(n => n > 0).Distinct().ToHashSet();
		if (set.ValidCardNumbers.Count > 0)
		{
			set.IsOwned = set.ValidCardNumbers.All(ownedDistinct.Contains);
			return;
		}

		set.IsOwned = set.IsOwned || ownedDistinct.Count > 0;
	}

	public static bool IsInsertOwned(InsertSet set)
	{
		return CardLogic.GetInsertProgress(set).IsComplete;
	}

	public static bool TryResolveInsertTarget(List<InsertSet> insertSets, string input, out InsertSet? set, out int? number)
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

	public static bool TryExpandInsertTarget(
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

	public static InsertSet? FindInsertSet(List<InsertSet> insertSets, string name)
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

	public static List<InsertSet> CreateDefaultInsertSets()
	{
		return GetDefaultInsertSetDefinitions()
			.Select(d => new InsertSet { Name = d.Name, Category = d.Category, Code = d.Code, IsOwned = false })
			.ToList();
	}

	public static List<(string Name, string Category, string Code)> GetDefaultInsertSetDefinitions()
	{
		return new List<(string Name, string Category, string Code)>
		{
			// Series 1
			("2025 All Topps Team", "Standard Inserts", "ATT"),
			("Topps Profiles", "Standard Inserts", "TP"),
			("Big Ticket Players", "Standard Inserts", "BTP"),
			("2025's Greatest Hits", "Standard Inserts", "GH"),
			("First Pitch", "Standard Inserts", "FP"),
			("Heavy Lumber", "Standard Inserts", "HL"),
			("Home Field", "Standard Inserts", "HA"),
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
			("75 Years of Topps Baseball Gifts", "Super Box and Specialty Exclusives", "75Y"),
			// Series 2
			("Crooked Numbers", "Standard Inserts", "CN"),
			("Glove Work", "Standard Inserts", "GW"),
			("Highlight Reels", "Standard Inserts", "HR"),
			("Diamond Dust", "Super Box and Specialty Exclusives", "DD"),
			("Swinging With The Stars", "Fanatics Fest Exclusive Inserts", "SWS"),
			("Oversized 1991 Topps Baseball Cards", "Super Box and Specialty Exclusives", "O91"),
			("Funko Pop", "Super Box and Specialty Exclusives", "FPOP"),
			("Bulk Order", "Super Box and Specialty Exclusives", "BO"),
			("The Flagship Collection Base Cards", "Super Box and Specialty Exclusives", "TFC"),
			("The Flagship Collection Chrome Base Cards", "Super Box and Specialty Exclusives", "TFCC"),
			("In The Name Relics", "Relics", "ITN"),
		};
	}

	public static bool NormalizeInsertSets(List<InsertSet> insertSets)
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
					Name = ChecklistPdfParser.NormalizeInsertCardName(g.First().Name)
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

	public static string NormalizeInsertCode(string? code)
	{
		if (string.IsNullOrWhiteSpace(code))
		{
			return string.Empty;
		}

		return Regex.Replace(code.ToUpperInvariant(), @"[^A-Z0-9]", string.Empty);
	}

	public static string GenerateInsertCode(string name, IEnumerable<string> existingCodes)
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
}
