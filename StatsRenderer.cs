using System.Collections.Generic;
using Spectre.Console;
using System.Linq;
using System;

public static class StatsRenderer
{
	public static void DrawStatsPanel(List<Card> cards, List<OddsEntry> oddsEntries, List<InsertSet> insertSets, bool animate = false)
	{
		var totalBase = cards.Count;
		var ownedBase = cards.Count(c => c.IsOwned);
		var missingBase = totalBase - ownedBase;
		var completion = totalBase == 0 ? 0 : (ownedBase * 100.0) / totalBase;
		var ownedParallels = cards.SelectMany(c => c.Variants).Where(v => v.IsOwned).ToList();
		var teamCardCount = GetPresentTeamCards(cards).Count;
		var ownedInsertSets = insertSets.Count(CommandHandler.IsInsertOwned);
		var totalInsertSets = insertSets.Count;

		var commonHits = ownedParallels.Count(v => v.Rarity.StartsWith("Common", StringComparison.OrdinalIgnoreCase));
		var uncommonHits = ownedParallels.Count(v => v.Rarity.Equals("Uncommon", StringComparison.OrdinalIgnoreCase));
		var rareHits = ownedParallels.Count(v => v.Rarity.Equals("Rare", StringComparison.OrdinalIgnoreCase));
		var ultraRareHits = ownedParallels.Count(v => v.Rarity.Equals("Ultra Rare", StringComparison.OrdinalIgnoreCase));
		var autosCount = ownedParallels.Count(v => v.IsAuto);
		var relicsCount = ownedParallels.Count(v => v.IsRelic);
		var oneOfOneCount = ownedParallels.Count(v => v.Is1Of1);

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

		var barWidth = 24;
		var filledWidth = totalBase == 0 ? 0 : (int)Math.Round((ownedBase * (double)barWidth) / totalBase);
		filledWidth = Math.Clamp(filledWidth, 0, barWidth);
		var filledBar = new string('█', filledWidth);
		var emptyBar = new string('░', barWidth - filledWidth);

		var s1FilledWidth = Math.Clamp((int)Math.Round((s1Owned * (double)barWidth) / s1Total), 0, barWidth);
		var s1FilledBar = new string('█', s1FilledWidth);
		var s1EmptyBar = new string('░', barWidth - s1FilledWidth);

		var s2FilledWidth = Math.Clamp((int)Math.Round((s2Owned * (double)barWidth) / s2Total), 0, barWidth);
		var s2FilledBar = new string('█', s2FilledWidth);
		var s2EmptyBar = new string('░', barWidth - s2FilledWidth);

		var grid = new Grid();
		grid.AddColumn();
		grid.AddColumn();

		grid.AddRow(
			"[bold green]Overall Base Progress:[/]",
			$"[bold green]{filledBar}[/][dim]{emptyBar}[/] [bold white]{completion:0.0}%[/] ({ownedBase}/{totalBase})"
		);
		grid.AddRow(
			"[bold cyan]Series 1 (#1-350):[/]",
			$"[bold cyan]{s1FilledBar}[/][dim]{s1EmptyBar}[/] [bold white]{s1Pct:0.0}%[/] ({s1Owned}/{s1Total}, missing {s1Missing})"
		);
		grid.AddRow(
			"[bold yellow]Series 2 (#351-700):[/]",
			$"[bold yellow]{s2FilledBar}[/][dim]{s2EmptyBar}[/] [bold white]{s2Pct:0.0}%[/] ({s2Owned}/{s2Total}, missing {s2Missing})"
		);
		grid.AddRow("[bold red]Total Missing Base Cards:[/]", $"[bold white]{missingBase}[/]");
		grid.AddRow("[bold cyan]Parallel Hits Logged:[/]", $"[bold white]{ownedParallels.Count}[/] (Autos: [yellow]{autosCount}[/], Relics: [cyan]{relicsCount}[/], 1/1: [magenta]{oneOfOneCount}[/])");
		grid.AddRow("[bold gold1]Rare + Ultra Hits:[/]", $"[bold gold1]{rareHits + ultraRareHits}[/]");
		grid.AddRow("[bold green]MLB Team Coverage:[/]", $"[bold white]{teamCardCount}/30[/] Teams");
		grid.AddRow("[bold blue]Insert Set Progress:[/]", $"[bold white]{ownedInsertSets}/{totalInsertSets}[/] Sets Complete");
		grid.AddRow("[dim]Odds Catalog Size:[/]", $"[dim]{oddsEntries.Count} odds lines[/]");

		var panel = new Panel(grid)
		{
			Header = new PanelHeader("[bold red]⚾ DUGOUT MANAGER '26 ARCADE DASHBOARD ⚾[/]", Justify.Center),
			Border = BoxBorder.Rounded,
			BorderStyle = new Style(Color.Gold1)
		};

		if (animate)
		{
			AnsiAnimation.SlowRevealRenderable(panel, lineDelayMs: 55);
		}
		else
		{
			AnsiConsole.Write(panel);
		}

		if (ownedParallels.Count > 0)
		{
			var chart = new BreakdownChart()
				.Width(60)
				.AddItem("Common", commonHits, Color.Green)
				.AddItem("Uncommon", uncommonHits, Color.Cyan1)
				.AddItem("Rare", rareHits, Color.Yellow)
				.AddItem("Ultra Rare", ultraRareHits, Color.Magenta1);

			AnsiConsole.MarkupLine("[bold grey]Parallel Rarity Breakdown:[/]");
			if (animate)
			{
				AnsiAnimation.SlowRevealRenderable(chart, lineDelayMs: 55);
			}
			else
			{
				AnsiConsole.Write(chart);
			}
			AnsiConsole.WriteLine();
		}
	}

	public static HashSet<string> GetPresentTeamCards(List<Card> cards)
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

	public static bool TryNormalizeTeamName(string value, out string normalized)
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

	public static List<string> GetMlbTeams()
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
}
