using System.Collections.Generic;
using Spectre.Console;

public static class ConsoleUi
{
	public static void SetColor(ConsoleColor color)
	{
		Console.ForegroundColor = color;
	}

	public static ConsoleColor GetRarityColor(string rarity)
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

	public static string FormatCardLabel(Card card)
	{
		return $"#{card.Id,3}  {card.PlayerName}";
	}

	public static void DrawArcadeHeader(List<Card> cards, List<OddsEntry> oddsEntries, bool animate = false)
	{
		AnsiAnimation.PlayTypewriterHeader(colDelayMs: animate ? 25 : 0);

		var owned = cards.Count(c => c.IsOwned);
		var missing = cards.Count - owned;
		var scoreboard = new Markup($"[bold blue]SCOREBOARD[/] | [bold green]OWNED {owned}/{cards.Count}[/] | [bold red]MISSING {missing}[/] | [bold yellow]ODDS LINES {oddsEntries.Count}[/]\n");

		if (animate)
		{
			AnsiAnimation.SlowRevealRenderable(scoreboard, lineDelayMs: 65);
		}
		else
		{
			AnsiConsole.Write(scoreboard);
			AnsiConsole.WriteLine();
		}
	}

	public static void RenderTopFrame(List<Card> cards, List<OddsEntry> oddsEntries, List<InsertSet> insertSets)
	{
		var owned = cards.Count(c => c.IsOwned);
		var total = cards.Count;
		var pct = total == 0 ? 0 : (owned * 100.0) / total;

		var s1Owned = cards.Count(c => c.Id >= 1 && c.Id <= 350 && c.IsOwned);
		var s2Owned = cards.Count(c => c.Id >= 351 && c.Id <= 700 && c.IsOwned);

		var parallels = cards.SelectMany(c => c.Variants).Count(v => v.IsOwned);
		var ownedInserts = insertSets.Count(CommandHandler.IsInsertOwned);

		var headerGrid = new Grid();
		headerGrid.AddColumn(new GridColumn().LeftAligned());
		headerGrid.AddColumn(new GridColumn().RightAligned());

		headerGrid.AddRow(
			"[bold red]⚾ DUGOUT MANAGER '26[/] [dim]|[/] [bold gold1]DUGOUT '26 BASEBALL TRACKER[/]",
			$"[bold green]BASE: {owned}/{total} ({pct:0.0}%)[/] [dim]|[/] [bold cyan]S1: {s1Owned}/350[/] [dim]|[/] [bold yellow]S2: {s2Owned}/350[/] [dim]|[/] [bold magenta]HITS: {parallels}[/]"
		);

		var quickHelp = "[bold grey]QUICK COMMANDS:[/] [green]have 1,5,10-15[/] [dim]|[/] [green]missing <s1|s2|range>[/] [dim]|[/] [green]series[/] [dim]|[/] [green]hit 24 Gold #45/2026[/] [dim]|[/] [green]parallels[/] [dim]|[/] [green]inserts[/] [dim]|[/] [green]menu[/]";

		var panel = new Panel(new Rows(headerGrid, new Rule().RuleStyle(Style.Parse("dim")), new Markup(quickHelp)))
		{
			Header = new PanelHeader("[bold gold1] DUGOUT OS - FIXED DASHBOARD & MENU FRAME [/]", Justify.Center),
			Border = BoxBorder.Rounded,
			BorderStyle = new Style(Color.Gold1),
			Padding = new Padding(1, 0, 1, 0)
		};

		AnsiConsole.Write(panel);
		AnsiConsole.WriteLine();
	}

	public static void RenderCommandFrameHeader()
	{
		var rule = new Rule("[bold cyan] ⚾ DUGOUT COMMAND TERMINAL ⚾ [/]")
		{
			Style = Style.Parse("cyan1"),
			Justification = Justify.Left
		};
		AnsiConsole.Write(rule);
	}

	public static void PrintSeriesReport(List<Card> cards)
	{
		var s1Cards = cards.Where(c => c.Id >= 1 && c.Id <= 350).ToList();
		var s1Owned = s1Cards.Count(c => c.IsOwned);
		var s1Total = s1Cards.Count > 0 ? s1Cards.Count : 350;
		var s1Missing = s1Total - s1Owned;
		var s1Pct = s1Total == 0 ? 0 : (s1Owned * 100.0) / s1Total;

		var s2Cards = cards.Where(c => c.Id >= 351 && c.Id <= 700).ToList();
		var s2Owned = s2Cards.Count(c => c.IsOwned);
		var s2Total = s2Cards.Count > 0 ? s2Cards.Count : 350;
		var s2Missing = s2Total - s2Owned;
		var s2Pct = s2Total == 0 ? 0 : (s2Owned * 100.0) / s2Total;

		var totalOwned = cards.Count(c => c.IsOwned);
		var totalCount = cards.Count;
		var totalMissing = totalCount - totalOwned;
		var totalPct = totalCount == 0 ? 0 : (totalOwned * 100.0) / totalCount;

		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title("[bold yellow]SERIES COMPLETENESS REPORT[/]")
			.AddColumn(new TableColumn("[bold]Series[/]"))
			.AddColumn(new TableColumn("[bold]Card Range[/]").Centered())
			.AddColumn(new TableColumn("[bold]Owned[/]").RightAligned())
			.AddColumn(new TableColumn("[bold]Missing[/]").RightAligned())
			.AddColumn(new TableColumn("[bold]Completion %[/]").RightAligned())
			.AddColumn(new TableColumn("[bold]Status[/]").Centered());

		var s1Status = s1Missing == 0 ? "[bold green]COMPLETE[/]" : (s1Owned > 0 ? "[bold yellow]PARTIAL[/]" : "[bold red]EMPTY[/]");
		var s2Status = s2Missing == 0 ? "[bold green]COMPLETE[/]" : (s2Owned > 0 ? "[bold yellow]PARTIAL[/]" : "[bold red]EMPTY[/]");
		var totalStatus = totalMissing == 0 ? "[bold green]COMPLETE[/]" : (totalOwned > 0 ? "[bold yellow]PARTIAL[/]" : "[bold red]EMPTY[/]");

		table.AddRow("[cyan]Series 1[/]", "[dim]#1 - #350[/]", $"[bold white]{s1Owned}/{s1Total}[/]", $"[red]{s1Missing}[/]", $"[bold cyan]{s1Pct:0.0}%[/]", s1Status);
		table.AddRow("[yellow]Series 2[/]", "[dim]#351 - #700[/]", $"[bold white]{s2Owned}/{s2Total}[/]", $"[red]{s2Missing}[/]", $"[bold yellow]{s2Pct:0.0}%[/]", s2Status);
		table.AddRow("[bold gold1]TOTAL OVERALL[/]", "[dim]#1 - #700[/]", $"[bold white]{totalOwned}/{totalCount}[/]", $"[bold red]{totalMissing}[/]", $"[bold gold1]{totalPct:0.0}%[/]", totalStatus);

		AnsiConsole.Write(table);
	}

	public static string ShowInteractiveMenu()
	{
		var prompt = new SelectionPrompt<string>()
			.Title("[bold yellow]DUGOUT MANAGER '26 - SELECT ACTION[/]")
			.PageSize(13)
			.MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
			.AddChoices(new[]
			{
				"⚾ Play Batter Swinging Animation (anim)",
				"📊 Display Arcade Stats Dashboard (stats)",
				"📦 View Series Completeness Report (series)",
				"✅ Mark Base Cards Owned (have)",
				"🔥 Log Parallel Hit (hit)",
				"🔍 View Missing Base Cards (missing)",
				"✨ View Insert Set Checklist (inserts)",
				"💎 View Logged Parallel Hits (parallels)",
				"🏟️ View Team Card Report (teams)",
				"🎲 View Parallel Odds Board (odds)",
				"❓ Help & Command Reference (help)",
				"❌ Exit Application (quit)"
			});

		var choice = AnsiConsole.Prompt(prompt);

		if (choice.StartsWith("⚾")) return "anim";
		if (choice.StartsWith("📊")) return "stats";
		if (choice.StartsWith("📦")) return "series";
		if (choice.StartsWith("✅")) return "have";
		if (choice.StartsWith("🔥")) return "hit";
		if (choice.StartsWith("🔍")) return "missing";
		if (choice.StartsWith("✨")) return "inserts";
		if (choice.StartsWith("💎")) return "parallels";
		if (choice.StartsWith("🏟️")) return "teams";
		if (choice.StartsWith("🎲")) return "odds";
		if (choice.StartsWith("❓")) return "help";
		return "quit";
	}

	public static void PrintHelp(bool animate = false)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title("[bold yellow]COMMAND REFERENCE[/]")
			.AddColumn("[bold cyan]Command[/]")
			.AddColumn("[bold gold1]Alias[/]")
			.AddColumn("[bold white]Description & Usage Example[/]");

		table.AddRow("[bold green]menu[/]", "[bold]m[/]", "Open interactive arrow-key selection prompt menu");
		table.AddRow("[bold green]anim[/]", "[bold]swing[/]", "Play 8-bit ASCII baseball batter swinging animation");
		table.AddRow("[bold green]have[/]", "[bold]h[/]", "Mark base cards owned (ex: [yellow]have 1, 5, 10-15[/])");
		table.AddRow("[bold green]missing[/]", "[bold]m[/]", "List missing cards (ex: [yellow]missing s1[/], [yellow]missing s2[/], [yellow]missing 25-80[/])");
		table.AddRow("[bold green]series[/]", "[bold]s[/]", "Show Series 1 (#1-350) & Series 2 (#351-700) completeness report");
		table.AddRow("[bold green]roster[/]", "[bold]list[/]", "Show full base roster ownership + parallel hit counts");
		table.AddRow("[bold green]check[/]", "[bold]c[/]", "Show status of one card (ex: [yellow]check 24[/])");
		table.AddRow("[bold green]hit[/]", "", "Log parallel hit (ex: [yellow]hit 24 Gold #45/2026[/] or [yellow]hit 24 Red Auto /10[/])");
		table.AddRow("[bold green]unhit[/]", "[bold]uh[/]", "Remove logged parallel hit (ex: [yellow]unhit 24 Gold[/])");
		table.AddRow("[bold green]parallels[/]", "[bold]p[/]", "Show owned parallels (ex: [yellow]parallels rare[/], [yellow]parallels auto[/], [yellow]parallels /75[/])");
		table.AddRow("[bold green]inserts[/]", "", "Insert sets checklist (ex: [yellow]inserts h TOG1-3[/], [yellow]inserts cat:Retail[/])");
		table.AddRow("[bold green]stats[/]", "", "Render Arcade Stats Panel dashboard & progress bars");
		table.AddRow("[bold green]teams[/]", "", "Show MLB 30-team card coverage report");
		table.AddRow("[bold green]odds[/]", "", "Display top pull odds catalog from Topps PDFs");
		table.AddRow("[bold green]quit[/]", "[bold]exit, q[/]", "Save collection and exit");

		if (animate)
		{
			AnsiAnimation.SlowRevealRenderable(table, lineDelayMs: 55);
		}
		else
		{
			AnsiConsole.Write(table);
			AnsiConsole.WriteLine();
		}
	}

	public static void PrintTeamCardReport(List<Card> cards)
	{
		var present = StatsRenderer.GetPresentTeamCards(cards);
		var expected = StatsRenderer.GetMlbTeams();
		var missing = expected.Where(team => !present.Contains(team)).ToList();

		AnsiConsole.MarkupLine($"[bold cyan]Team Card Report:[/] Found [bold green]{present.Count}/30[/] MLB team cards.");

		if (missing.Count == 0)
		{
			AnsiConsole.MarkupLine("[bold green]All 30 MLB team cards are present in your collection![/]");
			return;
		}

		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title("[bold yellow]Missing MLB Team Cards[/]")
			.AddColumn("[bold white]Team Name[/]");

		foreach (var team in missing)
		{
			table.AddRow($"[yellow]- {team}[/]");
		}

		AnsiConsole.Write(table);
	}

	public static void PrintOwnedParallelsReport(List<Card> cards, int? startId, int? endId, string? rarityFilter)
	{
		var ownedParallels = cards
			.Where(card => !startId.HasValue || !endId.HasValue || (card.Id >= startId.Value && card.Id <= endId.Value))
			.SelectMany(card => card.Variants
				.Where(variant => variant.IsOwned && MatchesParallelFilter(variant, rarityFilter))
				.Select(variant => new { Card = card, Variant = variant }))
			.OrderBy(item => item.Card.Id)
			.ThenBy(item => item.Variant.Name)
			.ToList();

		if (ownedParallels.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No parallels logged matching that filter.[/]");
			return;
		}

		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title("[bold cyan]Owned Parallels Report[/]")
			.AddColumn("[bold]Card #[/]")
			.AddColumn("[bold]Player Name[/]")
			.AddColumn("[bold]Parallel Variant / Serial[/]")
			.AddColumn("[bold]Rarity[/]")
			.AddColumn("[bold]Odds[/]");

		foreach (var item in ownedParallels)
		{
			var v = item.Variant;
			var serialText = !string.IsNullOrWhiteSpace(v.SerialNum)
				? $" [bold yellow]#{v.SerialNum}[/]"
				: (v.PrintRun.HasValue ? $" [cyan]/{v.PrintRun}[/]" : string.Empty);
			var autoText = v.IsAuto ? " [gold1][[AUTO]][/]" : string.Empty;
			var relicText = v.IsRelic ? " [blue][[RELIC]][/]" : string.Empty;
			var variantDisplay = $"{v.Name}{serialText}{autoText}{relicText}";

			table.AddRow(
				$"[white]#{item.Card.Id}[/]",
				$"[bold white]{item.Card.PlayerName}[/]",
				variantDisplay,
				$"[bold]{v.Rarity}[/]",
				$"[dim]{v.Odds ?? "n/a"}[/]"
			);
		}

		AnsiConsole.Write(table);
	}

	public static bool TryParseParallelFilters(string input, out int? startId, out int? endId, out string? rarityFilter)
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

	public static bool IsSupportedRarityFilter(string rarityFilter)
	{
		var normalized = NormalizeRarityFilterLabel(rarityFilter);
		if (normalized is "common" or "common/base" or "uncommon" or "rare" or "ultra rare" or "unknown" or "auto" or "autograph" or "relic" or "1/1" or "1of1")
		{
			return true;
		}

		if (normalized.StartsWith('/') && int.TryParse(normalized.Substring(1), out _))
		{
			return true;
		}

		return false;
	}

	public static string NormalizeRarityFilterLabel(string rarityFilter)
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

	public static bool MatchesParallelFilter(CardVariant variant, string? filter)
	{
		if (string.IsNullOrWhiteSpace(filter))
		{
			return true;
		}

		var norm = filter.Trim().ToLowerInvariant();
		if (norm is "auto" or "autograph" or "sig")
		{
			return variant.IsAuto;
		}

		if (norm is "relic" or "patch")
		{
			return variant.IsRelic;
		}

		if (norm is "1/1" or "1of1" or "superfractor")
		{
			return variant.Is1Of1;
		}

		if (norm.StartsWith('/') && int.TryParse(norm.Substring(1), out var printCap))
		{
			return variant.PrintRun.HasValue && variant.PrintRun.Value <= printCap;
		}

		return MatchesRarityFilter(variant.Rarity, filter);
	}

	public static bool MatchesRarityFilter(string rarity, string? rarityFilter)
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

	public static void PrintInsertSets(List<InsertSet> insertSets, bool? ownershipFilter, string? categoryFilter, string? nameFilter)
	{
		var rows = insertSets
			.Where(i => !ownershipFilter.HasValue || CommandHandler.IsInsertOwned(i) == ownershipFilter.Value)
			.Where(i => string.IsNullOrWhiteSpace(categoryFilter) || i.Category.Contains(categoryFilter, StringComparison.OrdinalIgnoreCase))
			.Where(i => string.IsNullOrWhiteSpace(nameFilter) || i.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase) || i.Code.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
			.OrderBy(i => i.Category)
			.ThenBy(i => i.Name)
			.ToList();

		if (rows.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No insert sets match that filter.[/]");
			return;
		}

		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title("[bold cyan]Insert Set Checklist[/]")
			.AddColumn(new TableColumn("[bold]Status[/]").Centered())
			.AddColumn(new TableColumn("[bold]Code[/]").Centered())
			.AddColumn(new TableColumn("[bold]Category[/]"))
			.AddColumn(new TableColumn("[bold]Set Name[/]"))
			.AddColumn(new TableColumn("[bold]Progress[/]").RightAligned());

		foreach (var item in rows)
		{
			var progress = CardLogic.GetInsertProgress(item);
			var statusMarkup = progress.IsComplete ? "[bold green]OWNED[/]" : progress.OwnedCount > 0 ? "[bold yellow]PARTIAL[/]" : "[bold red]MISSING[/]";
			var progressMarkup = item.ValidCardNumbers.Count == 0
				? (item.IsOwned ? "[green]Complete[/]" : "[red]0/0[/]")
				: $"[white]{progress.OwnedCount}/{progress.TotalCount}[/]";

			table.AddRow(
				statusMarkup,
				$"[cyan]{item.Code}[/]",
				$"[dim]{item.Category}[/]",
				$"[bold white]{item.Name}[/]",
				progressMarkup
			);
		}

		AnsiConsole.Write(table);
	}

	public static void PrintOwnedInsertCards(InsertSet set)
	{
		var ownedCards = set.OwnedCards.Where(n => n > 0).Distinct().OrderBy(n => n).ToList();
		var nameByNumber = (set.ValidCards ?? new List<InsertCardInfo>())
			.Where(c => c.Number > 0)
			.GroupBy(c => c.Number)
			.ToDictionary(g => g.Key, g => g.First().Name ?? string.Empty);

		var totalChecklist = set.ValidCardNumbers.Count;
		var ownedCount = ownedCards.Count;
		var isComplete = totalChecklist > 0
			? ownedCount >= totalChecklist
			: set.IsOwned;

		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title($"[bold cyan]Owned Insert Cards: {set.Name} [{set.Code}][/]")
			.AddColumn(new TableColumn("[bold]Card #[/]").RightAligned())
			.AddColumn(new TableColumn("[bold]Player Name[/]"));

		if (ownedCards.Count == 0)
		{
			AnsiConsole.MarkupLine(isComplete
				? "[green]All cards are considered owned for this set.[/]"
				: "[red]No owned cards logged for this set yet.[/]");
			return;
		}

		foreach (var number in ownedCards)
		{
			var cardName = nameByNumber.TryGetValue(number, out var foundName) && !string.IsNullOrWhiteSpace(foundName)
				? foundName
				: $"Card {number}";
			table.AddRow($"[yellow]#{number}[/]", $"[bold white]{cardName}[/]");
		}

		AnsiConsole.Write(table);
	}

	public static void PrintOddsBoard(List<OddsEntry> oddsEntries)
	{
		if (oddsEntries.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No odds entries loaded.[/]");
			return;
		}

		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title("[bold cyan]Odds Board (Top 25 Easiest Pulls)[/]")
			.AddColumn("[bold]Parallel / Hit Name[/]")
			.AddColumn(new TableColumn("[bold]Odds Ratio[/]").RightAligned())
			.AddColumn("[bold]Rarity Tier[/]");

		foreach (var entry in oddsEntries.Take(25))
		{
			table.AddRow(
				$"[bold white]{entry.Name}[/]",
				$"[yellow]{entry.OddsText}[/]",
				$"[bold]{entry.Rarity}[/]"
			);
		}

		AnsiConsole.Write(table);
	}

	public static bool TryParseRange(string input, out int start, out int end)
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
}
