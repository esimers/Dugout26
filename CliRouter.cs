using System.Text.Json;
using Spectre.Console;

public static class CliRouter
{
	public static void Run(
		List<Card> cards,
		List<InsertSet> insertSets,
		List<OddsEntry> oddsEntries,
		string filePath,
		string insertSetsPath,
		JsonSerializerOptions jsonOptions)
	{
		var autoStatsPanel = true;

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

			ProcessCommand(command, argument, cards, insertSets, oddsEntries, filePath, insertSetsPath, jsonOptions, ref autoStatsPanel);
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
				else if (menuChoice == "hit")
				{
					promptArg = AnsiConsole.Ask<string>("[yellow]Enter parallel hit details (ex: 24 Gold #45/2026 or 24 Red Auto /10):[/]");
				}

				return ProcessCommand(menuChoice, promptArg, cards, insertSets, oddsEntries, filePath, insertSetsPath, jsonOptions, ref autoStatsPanel);

			case "have":
			case "h":
				if (string.IsNullOrWhiteSpace(argument))
				{
					Console.WriteLine("Usage: have 1, 5, 10-15");
					break;
				}

				var ids = CardLogic.ParseIds(argument);
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

				StorageService.SaveCards(filePath, cards, jsonOptions);
				ConsoleUi.SetColor(ConsoleColor.Green);
				Console.WriteLine($"Roster update: signed {markedCount} base card(s).");
				Console.ResetColor();
				if (autoStatsPanel)
				{
					StatsRenderer.DrawStatsPanel(cards, oddsEntries, insertSets);
				}
				break;

			case "series":
			case "s":
				ConsoleUi.PrintSeriesReport(cards);
				break;

			case "missing":
			case "m":
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
					var baseStatus = card.IsOwned ? "OWNED" : "MISSING";
					var parallelOwned = card.Variants.Count(v => v.IsOwned);
					ConsoleUi.SetColor(card.IsOwned ? ConsoleColor.Green : ConsoleColor.Red);
					Console.WriteLine($"{ConsoleUi.FormatCardLabel(card),-36}  Base:{baseStatus,-7}  Parallels:{parallelOwned}");
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

				ConsoleUi.SetColor(checkCard.IsOwned ? ConsoleColor.Green : ConsoleColor.Yellow);
				Console.WriteLine($"{ConsoleUi.FormatCardLabel(checkCard)} - {(checkCard.IsOwned ? "OWNED" : "MISSING")}");
				Console.ResetColor();
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
				var unhitVariant = unhitCard.Variants.FirstOrDefault(v => v.Name.Equals(unhitVariantName, StringComparison.OrdinalIgnoreCase));
				if (unhitVariant is null || !unhitVariant.IsOwned)
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
}

