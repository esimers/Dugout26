using System.Text.Json;
using Spectre.Console;

public static class CliRouter
{
	private sealed class SessionLog
	{
		public int BaseAdded;
		public int DupsAdded;
		public int CopiesRemoved;
		public int HitsLogged;
		public int HitsRemoved;

		public bool IsEmpty =>
			BaseAdded == 0 && DupsAdded == 0 && CopiesRemoved == 0 && HitsLogged == 0 && HitsRemoved == 0;
	}

	public static void Run(
		List<Card> cards,
		List<InsertSet> insertSets,
		List<OddsEntry> oddsEntries,
		string filePath,
		string insertSetsPath,
		JsonSerializerOptions jsonOptions)
	{
		var autoStatsPanel = true;
		var session = new SessionLog();

		ConsoleUi.DrawArcadeHeader(cards, oddsEntries, animate: true);
		ConsoleUi.PrintHelp(animate: true);
		StatsRenderer.DrawStatsPanel(cards, oddsEntries, insertSets, animate: true);

		ConsoleUi.RenderCommandFrameHeader();

		while (true)
		{
			ConsoleUi.SetColor(ConsoleColor.DarkRed);
			Console.Write("DUGOUT");
			ConsoleUi.SetColor(ConsoleColor.Gray);
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

			ProcessCommand(command, argument, cards, insertSets, oddsEntries, filePath, insertSetsPath, jsonOptions, session, ref autoStatsPanel);
		}
	}

	private static bool ProcessCommand(
		string command,
		string argument,
		List<Card> cards,
		List<InsertSet> insertSets,
		List<OddsEntry> oddsEntries,
		string filePath,
		string insertSetsPath,
		JsonSerializerOptions jsonOptions,
		SessionLog session,
		ref bool autoStatsPanel)
	{
		switch (command)
		{
			case "anim":
			case "swing":
				AnsiAnimation.PlayBatterSwinging();
				return true;

			case "menu":
			case "interactive":
				var menuChoice = ConsoleUi.ShowInteractiveMenu();
				if (menuChoice == "quit")
				{
					StorageService.SaveCards(filePath, cards, jsonOptions);
					AnsiConsole.MarkupLine("[green]Collection saved. Goodbye![/]");
					Environment.Exit(0);
				}

				string promptArg = string.Empty;
				if (menuChoice == "have")
				{
					promptArg = AnsiConsole.Ask<string>("[yellow]Enter card IDs to mark owned (ex: 1, 5, 10-15):[/]");
				}
				else if (menuChoice == "dup")
				{
					promptArg = AnsiConsole.Ask<string>("[yellow]Enter owned card IDs to add extra copies (ex: 24, 31):[/]");
				}
				else if (menuChoice == "unhave")
				{
					promptArg = AnsiConsole.Ask<string>("[yellow]Enter card IDs to remove one copy (ex: 24, 10-15):[/]");
				}
				else if (menuChoice == "find")
				{
					promptArg = AnsiConsole.Ask<string>("[yellow]Enter player name or card number (ex: judge):[/]");
				}
				else if (menuChoice == "hit")
				{
					promptArg = AnsiConsole.Ask<string>("[yellow]Enter parallel hit details (ex: 24 Gold #45/2026 or 24 Red Auto /10):[/]");
				}
				else if (menuChoice == "import")
				{
					promptArg = AnsiConsole.Ask<string>("[yellow]Enter backup folder or JSON path (ex: backups/2026-09-20_143052):[/]");
				}

				return ProcessCommand(menuChoice, promptArg, cards, insertSets, oddsEntries, filePath, insertSetsPath, jsonOptions, session, ref autoStatsPanel);

			case "have":
			case "h":
				ApplyQuantityCommand(argument, cards, filePath, jsonOptions, insertSets, oddsEntries, session, ref autoStatsPanel, "have", "Usage: have 1, 5, 10-15");
				break;

			case "dup":
				ApplyQuantityCommand(argument, cards, filePath, jsonOptions, insertSets, oddsEntries, session, ref autoStatsPanel, "dup", "Usage: dup 24, 31  (adds extra copies of owned cards)");
				break;

			case "unhave":
			case "uhave":
				ApplyQuantityCommand(argument, cards, filePath, jsonOptions, insertSets, oddsEntries, session, ref autoStatsPanel, "unhave", "Usage: unhave 24, 10-15  (removes one copy)");
				break;

			case "dups":
			case "duplicates":
				ConsoleUi.PrintDuplicatesReport(cards, argument);
				break;

			case "export":
				var exportParts = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				var exportSub = exportParts.Length > 0 ? exportParts[0].ToLowerInvariant() : string.Empty;

				if (exportSub is "dups" or "dup" or "duplicates" or "trade")
				{
					var (dupSeries, dupFormat) = ParseSeriesAndFormat(exportParts, 1);
					var exportedDupsPath = StorageService.ExportDuplicateCards(cards, dupSeries, dupFormat);
					ConsoleUi.SetColor(ConsoleColor.Green);
					Console.WriteLine($"Export successful: Saved duplicate/trade list to '{exportedDupsPath}'.");
					Console.ResetColor();
				}
				else if (exportSub == "missing" || string.IsNullOrWhiteSpace(exportSub) || exportSub is "s1" or "s2" or "series1" or "series2" or "all")
				{
					var startIndex = exportSub == "missing" ? 1 : 0;
					var (seriesArg, formatArg) = ParseSeriesAndFormat(exportParts, startIndex);
					if (exportSub is "s1" or "s2" or "series1" or "series2" or "all")
					{
						seriesArg = exportSub;
						formatArg = exportParts.Length > 1 && IsFormatToken(exportParts[1]) ? exportParts[1] : "txt";
					}

					var exportedPath = StorageService.ExportMissingCards(cards, seriesArg, formatArg);
					ConsoleUi.SetColor(ConsoleColor.Green);
					Console.WriteLine($"Export successful: Saved missing card report to '{exportedPath}'.");
					Console.ResetColor();
				}
				else
				{
					Console.WriteLine("Usage: export missing [s1|s2|all|range] [txt|csv]");
					Console.WriteLine("       export dups [s1|s2|all|range] [txt|csv]");
					Console.WriteLine("Examples: export missing s1 | export dups csv | export trade s2 | export missing 500-559");
				}
				break;

			case "series":
			case "s":
				ConsoleUi.PrintSeriesReport(cards);
				break;

			case "missing":
			case "m":
				if (!string.IsNullOrWhiteSpace(argument) && argument.Contains("export", StringComparison.OrdinalIgnoreCase))
				{
					var cleanArg = argument.Replace("export", "", StringComparison.OrdinalIgnoreCase).Trim();
					var formatArg = cleanArg.EndsWith("csv", StringComparison.OrdinalIgnoreCase) ? "csv" : "txt";
					cleanArg = cleanArg.Replace("csv", "", StringComparison.OrdinalIgnoreCase).Trim();

					var exportedPath = StorageService.ExportMissingCards(cards, cleanArg, formatArg);
					ConsoleUi.SetColor(ConsoleColor.Green);
					Console.WriteLine($"Export successful: Saved missing card report to '{exportedPath}'.");
					Console.ResetColor();
					break;
				}

				var missingQuery = cards.Where(c => !c.IsOwned);
				var argTrim = argument.Trim().ToLowerInvariant();

				if (!string.IsNullOrWhiteSpace(argTrim) && argTrim != "-a" && argTrim != "all")
				{
					if (argTrim is "s1" or "series1" or "series 1")
					{
						missingQuery = missingQuery.Where(c => c.Id >= 1 && c.Id <= 350);
					}
					else if (argTrim is "s2" or "series2" or "series 2")
					{
						missingQuery = missingQuery.Where(c => c.Id >= 351 && c.Id <= 700);
					}
					else if (!ConsoleUi.TryParseRange(argument, out var startId, out var endId))
					{
						Console.WriteLine("Usage: missing, missing s1, missing s2, missing -a, or missing <start-end> (example: missing 25-80)");
						break;
					}
					else
					{
						missingQuery = missingQuery.Where(c => c.Id >= startId && c.Id <= endId);
					}
				}

				var missingCards = missingQuery.OrderBy(c => c.Id).ToList();
				if (missingCards.Count == 0)
				{
					ConsoleUi.SetColor(ConsoleColor.Green);
					if (string.IsNullOrWhiteSpace(argument) || argument.Equals("-a", StringComparison.OrdinalIgnoreCase) || argument.Equals("all", StringComparison.OrdinalIgnoreCase))
					{
						Console.WriteLine("WORLD SERIES COMPLETE: all base cards owned.");
					}
					else
					{
						Console.WriteLine($"No missing cards in that query ({argument}).");
					}
					Console.ResetColor();
					break;
				}

				ConsoleUi.SetColor(ConsoleColor.Yellow);
				var reportTitle = argTrim switch
				{
					"s1" or "series1" or "series 1" => "Scout Report - Missing Series 1 Base Cards (#1 - #350)",
					"s2" or "series2" or "series 2" => "Scout Report - Missing Series 2 Base Cards (#351 - #700)",
					_ => "Scout Report - Missing Base Cards"
				};
				Console.WriteLine(reportTitle);
				Console.ResetColor();
				foreach (var card in missingCards)
				{
					Console.WriteLine(ConsoleUi.FormatCardLabel(card));
				}
				break;

			case "list":
			case "roster":
				foreach (var card in cards.OrderBy(c => c.Id))
				{
					var baseStatus = card.IsOwned ? (card.Quantity > 1 ? $"OWNED(x{card.Quantity})" : "OWNED") : "MISSING";
					var parallelOwned = card.Variants.Count(v => v.IsOwned);
					ConsoleUi.SetColor(card.IsOwned ? ConsoleColor.Green : ConsoleColor.Red);
					Console.WriteLine($"{ConsoleUi.FormatCardLabel(card),-36}  Base:{baseStatus,-11}  Parallels:{parallelOwned}");
					Console.ResetColor();
				}
				break;

			case "check":
			case "c":
				if (string.IsNullOrWhiteSpace(argument))
				{
					Console.WriteLine("Usage: check <cardId|player name>  (ex: check 24 or check judge)");
					break;
				}

				var checkMatches = CardLogic.FindCards(cards, argument);
				if (checkMatches.Count == 0)
				{
					ConsoleUi.SetColor(ConsoleColor.Yellow);
					Console.WriteLine(int.TryParse(argument.Trim(), out var missingId)
						? $"Card #{missingId} not found."
						: $"No players matching '{argument.Trim()}'.");
					Console.ResetColor();
					break;
				}

				if (checkMatches.Count == 1)
				{
					ConsoleUi.PrintCardStatus(checkMatches[0]);
				}
				else
				{
					ConsoleUi.PrintPlayerSearch(checkMatches, argument.Trim());
				}
				break;

			case "find":
			case "f":
				if (string.IsNullOrWhiteSpace(argument))
				{
					Console.WriteLine("Usage: find <player name|cardId>  (ex: find judge)");
					break;
				}

				ConsoleUi.PrintPlayerSearch(CardLogic.FindCards(cards, argument), argument.Trim());
				break;

			case "hit":
				if (string.IsNullOrWhiteSpace(argument))
				{
					Console.WriteLine("Usage: hit <cardId> <parallel name> [#serial/total or /printrun]");
					Console.WriteLine("Examples: hit 24 Gold Foil | hit 24 Gold #45/2026 | hit 24 Red Auto /10 | hit 24 Platinum 1/1");
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

				var matchedOdds = OddsPdfParser.FindBestOddsMatch(variantName, oddsEntries);
				var parsedVariant = VariantParser.Parse(variantName, matchedOdds?.OddsText, matchedOdds?.Rarity ?? "Unknown");

				var variant = hitCard.Variants.FirstOrDefault(v => v.Name.Equals(parsedVariant.Name, StringComparison.OrdinalIgnoreCase));
				if (variant is null)
				{
					variant = parsedVariant;
					hitCard.Variants.Add(variant);
				}
				else
				{
					variant.IsOwned = true;
					variant.Odds = matchedOdds?.OddsText ?? variant.Odds;
					variant.Rarity = matchedOdds?.Rarity ?? variant.Rarity;
					if (parsedVariant.PrintRun.HasValue) variant.PrintRun = parsedVariant.PrintRun;
					if (!string.IsNullOrWhiteSpace(parsedVariant.SerialNum)) variant.SerialNum = parsedVariant.SerialNum;
					if (parsedVariant.IsAuto) variant.IsAuto = true;
					if (parsedVariant.IsRelic) variant.IsRelic = true;
				}

				StorageService.SaveCards(filePath, cards, jsonOptions);
				session.HitsLogged++;
				ConsoleUi.SetColor(ConsoleUi.GetRarityColor(variant.Rarity));
				var serialSummary = !string.IsNullOrWhiteSpace(variant.SerialNum) ? $" #{variant.SerialNum}" : (variant.PrintRun.HasValue ? $" /{variant.PrintRun}" : string.Empty);
				var autoSummary = variant.IsAuto ? " [AUTO]" : string.Empty;
				var relicSummary = variant.IsRelic ? " [RELIC]" : string.Empty;
				Console.WriteLine($"Hit logged: {ConsoleUi.FormatCardLabel(hitCard)} - {variant.Name}{serialSummary}{autoSummary}{relicSummary} ({variant.Rarity}){(string.IsNullOrWhiteSpace(variant.Odds) ? string.Empty : $", odds {variant.Odds}")}");
				Console.ResetColor();
				if (autoStatsPanel)
				{
					StatsRenderer.DrawStatsPanel(cards, oddsEntries, insertSets);
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
				var unhitVariant = unhitCard.Variants.FirstOrDefault(v => v.IsOwned && VariantParser.NameMatches(v, unhitVariantName));
				if (unhitVariant is null)
				{
					Console.WriteLine($"No owned parallel named '{unhitVariantName}' is logged for {ConsoleUi.FormatCardLabel(unhitCard)}.");
					break;
				}

				unhitVariant.IsOwned = false;
				if (!unhitCard.Variants.Any(v => v.IsOwned))
				{
					unhitCard.Variants.RemoveAll(v => !v.IsOwned);
				}

				StorageService.SaveCards(filePath, cards, jsonOptions);
				session.HitsRemoved++;
				ConsoleUi.SetColor(ConsoleColor.Yellow);
				Console.WriteLine($"Removed parallel: {ConsoleUi.FormatCardLabel(unhitCard)} - {unhitVariant.Name}");
				Console.ResetColor();
				if (autoStatsPanel)
				{
					StatsRenderer.DrawStatsPanel(cards, oddsEntries, insertSets);
				}
				break;

			case "stats":
				StatsRenderer.DrawStatsPanel(cards, oddsEntries, insertSets);
				break;

			case "inserts":
			case "insert":
				if (CommandHandler.HandleInsertCommand(argument, insertSets))
				{
					StorageService.SaveInsertSets(insertSetsPath, insertSets, jsonOptions);
				}
				break;

			case "parallels":
			case "p":
				if (!ConsoleUi.TryParseParallelFilters(argument, out var parallelStartId, out var parallelEndId, out var rarityFilter))
				{
					Console.WriteLine("Usage: parallels [start-end] [rarity]");
					Console.WriteLine("Examples: parallels rare | parallels 1-100 | parallels 1-100 ultra rare");
					break;
				}

				ConsoleUi.PrintOwnedParallelsReport(cards, parallelStartId, parallelEndId, rarityFilter);
				break;

			case "teams":
				ConsoleUi.PrintTeamCardReport(cards);
				break;

			case "odds":
				ConsoleUi.PrintOddsBoard(oddsEntries);
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

			case "session":
				PrintSessionSummary(session);
				break;

			case "backup":
				if (argument.Equals("list", StringComparison.OrdinalIgnoreCase) ||
					argument.Equals("ls", StringComparison.OrdinalIgnoreCase))
				{
					PrintBackupList();
				}
				else if (string.IsNullOrWhiteSpace(argument))
				{
					var backupPath = StorageService.CreateBackup(cards, insertSets, jsonOptions);
					ConsoleUi.SetColor(ConsoleColor.Green);
					Console.WriteLine($"Backup saved to '{backupPath}'.");
					Console.ResetColor();
					Console.WriteLine($"Restore later with: import {backupPath}");
				}
				else
				{
					Console.WriteLine("Usage: backup");
					Console.WriteLine("       backup list");
				}
				break;

			case "import":
			case "restore":
				if (string.IsNullOrWhiteSpace(argument))
				{
					Console.WriteLine("Usage: import <backup-folder|collection.json>");
					Console.WriteLine("Examples: import backups/2026-09-20_143052");
					Console.WriteLine("          import collection.json");
					PrintBackupList();
					break;
				}

				if (CommandHandler.ImportCollection(argument, cards, insertSets, filePath, insertSetsPath, jsonOptions) && autoStatsPanel)
				{
					StatsRenderer.DrawStatsPanel(cards, oddsEntries, insertSets);
				}
				break;

			case "help":
				ConsoleUi.PrintHelp();
				break;

			case "reset":
			case "clear":
			case "fresh":
				if (CommandHandler.ExecuteResetCollection(cards, insertSets, filePath, insertSetsPath, jsonOptions) && autoStatsPanel)
				{
					StatsRenderer.DrawStatsPanel(cards, oddsEntries, insertSets);
				}
				break;


			case "exit":
			case "quit":
			case "q":
				StorageService.SaveCards(filePath, cards, jsonOptions);
				Console.WriteLine("Collection saved. Goodbye.");
				Environment.Exit(0);
				return true;

			default:
				Console.WriteLine("Unknown command. Type 'help' or 'menu' for available options.");
				break;
		}

		return true;
	}

	private static void ApplyQuantityCommand(
		string argument,
		List<Card> cards,
		string filePath,
		JsonSerializerOptions jsonOptions,
		List<InsertSet> insertSets,
		List<OddsEntry> oddsEntries,
		SessionLog session,
		ref bool autoStatsPanel,
		string mode,
		string usage)
	{
		if (string.IsNullOrWhiteSpace(argument))
		{
			Console.WriteLine(usage);
			return;
		}

		var ids = CardLogic.ParseIds(argument);
		if (ids.Count == 0)
		{
			Console.WriteLine("No valid IDs found.");
			return;
		}

		var changed = 0;
		var skipped = 0;
		foreach (var id in ids.OrderBy(i => i))
		{
			var card = cards.FirstOrDefault(c => c.Id == id);
			if (card is null)
			{
				Console.WriteLine($"Card #{id} not found.");
				continue;
			}

			var result = mode switch
			{
				"dup" => CardLogic.ApplyDup(card),
				"unhave" => CardLogic.ApplyUnhave(card),
				_ => CardLogic.ApplyHave(card)
			};

			if (result.Changed)
			{
				changed++;
				if (mode == "dup") session.DupsAdded++;
				else if (mode == "unhave") session.CopiesRemoved++;
				else session.BaseAdded++;
			}
			else
			{
				skipped++;
			}

			if (mode != "have" || !result.Changed)
			{
				Console.WriteLine(result.Message);
			}
		}

		if (changed == 0)
		{
			return;
		}

		StorageService.SaveCards(filePath, cards, jsonOptions);
		if (mode == "have")
		{
			ConsoleUi.SetColor(ConsoleColor.Green);
			Console.WriteLine($"Roster update: signed {changed} new base card(s).");
			Console.ResetColor();
			if (skipped > 0)
			{
				Console.WriteLine($"{skipped} already owned (not incremented). Use dup <id> to add extras.");
			}
		}

		if (autoStatsPanel)
		{
			StatsRenderer.DrawStatsPanel(cards, oddsEntries, insertSets);
		}
	}

	private static void PrintBackupList()
	{
		var backups = StorageService.ListBackups();
		if (backups.Count == 0)
		{
			Console.WriteLine("No snapshots in backups/. Run backup first.");
			return;
		}

		ConsoleUi.SetColor(ConsoleColor.Cyan);
		Console.WriteLine("Available backups:");
		Console.ResetColor();
		foreach (var dir in backups)
		{
			Console.WriteLine($"  {dir}");
		}

		Console.WriteLine($"Import one with: import {backups[0]}");
	}

	private static void PrintSessionSummary(SessionLog session)
	{
		if (session.IsEmpty)
		{
			Console.WriteLine("This session: no collection changes yet.");
			return;
		}

		ConsoleUi.SetColor(ConsoleColor.Cyan);
		Console.WriteLine(
			$"This session: {session.BaseAdded} new base, {session.DupsAdded} extras, {session.CopiesRemoved} copies removed, {session.HitsLogged} hits logged, {session.HitsRemoved} hits removed.");
		Console.ResetColor();
	}

	private static (string Series, string Format) ParseSeriesAndFormat(string[] parts, int startIndex)
	{
		var series = "all";
		var format = "txt";
		for (var i = startIndex; i < parts.Length; i++)
		{
			if (IsFormatToken(parts[i]))
			{
				format = parts[i].ToLowerInvariant();
			}
			else
			{
				series = parts[i];
			}
		}

		return (series, format);
	}

	private static bool IsFormatToken(string token)
	{
		return token.Equals("txt", StringComparison.OrdinalIgnoreCase) ||
			token.Equals("csv", StringComparison.OrdinalIgnoreCase);
	}
}

