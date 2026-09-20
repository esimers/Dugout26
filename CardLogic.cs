using System.Collections.Generic;

public static class CardLogic
{
	public static string ClassifyRarity(string name, double oneIn)
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

	public static (int OwnedCount, int TotalCount, bool IsComplete) GetInsertProgress(InsertSet set)
	{
		var ownedCount = set.OwnedCards.Where(n => n > 0).Distinct().Count();
		var totalCount = set.ValidCardNumbers.Count;

		if (totalCount > 0)
		{
			return (ownedCount, totalCount, ownedCount >= totalCount);
		}

		return (ownedCount, totalCount, set.IsOwned);
	}

	public static HashSet<int> ParseIds(string input)
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

	public static string FormatOwnership(Card card)
	{
		if (!card.IsOwned)
		{
			return "MISSING";
		}

		return card.Quantity > 1 ? $"OWNED (x{card.Quantity})" : "OWNED (x1)";
	}

	public static (bool Changed, string Message) ApplyHave(Card card)
	{
		if (card.IsOwned)
		{
			return (false, $"Already owned (x{Math.Max(card.Quantity, 1)}). Use dup {card.Id} to add extras.");
		}

		card.IsOwned = true;
		return (true, $"Owned #{card.Id}  {card.PlayerName} (x1).");
	}

	public static (bool Changed, string Message) ApplyDup(Card card)
	{
		if (!card.IsOwned)
		{
			return (false, $"#{card.Id}  {card.PlayerName} is not owned. Use have {card.Id} first.");
		}

		if (card.Quantity < 1)
		{
			card.Quantity = 1;
		}

		card.Quantity++;
		return (true, $"Extra copy of #{card.Id}  {card.PlayerName} (now x{card.Quantity}).");
	}

	public static (bool Changed, string Message) ApplyUnhave(Card card)
	{
		if (!card.IsOwned)
		{
			return (false, $"#{card.Id}  {card.PlayerName} is already missing.");
		}

		if (card.Quantity > 1)
		{
			card.Quantity--;
			return (true, $"Removed 1 extra copy of #{card.Id}  {card.PlayerName} (now x{card.Quantity}).");
		}

		card.IsOwned = false;
		return (true, $"Marked missing: #{card.Id}  {card.PlayerName}");
	}

	public static List<Card> FindCards(IEnumerable<Card> cards, string query)
	{
		var q = query.Trim();
		if (q.Length == 0)
		{
			return new List<Card>();
		}

		if (int.TryParse(q, out var id))
		{
			return cards.Where(c => c.Id == id).OrderBy(c => c.Id).ToList();
		}

		return cards
			.Where(c => c.PlayerName.Contains(q, StringComparison.OrdinalIgnoreCase))
			.OrderBy(c => c.Id)
			.ToList();
	}

	public static List<Card> FilterBySeriesArg(IEnumerable<Card> cards, string? seriesArg, out string seriesLabel, out string fileTag)
	{
		var argTrim = (seriesArg ?? "all").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(argTrim) || argTrim is "all" or "-a")
		{
			seriesLabel = "All Series (#1 - #700)";
			fileTag = "all";
			return cards.ToList();
		}

		if (argTrim is "s1" or "series1" or "series 1")
		{
			seriesLabel = "Series 1 (#1 - #350)";
			fileTag = "series1";
			return cards.Where(c => c.Id >= 1 && c.Id <= 350).ToList();
		}

		if (argTrim is "s2" or "series2" or "series 2")
		{
			seriesLabel = "Series 2 (#351 - #700)";
			fileTag = "series2";
			return cards.Where(c => c.Id >= 351 && c.Id <= 700).ToList();
		}

		if (ConsoleUi.TryParseRange(seriesArg!.Trim(), out var startId, out var endId))
		{
			seriesLabel = $"Cards #{startId} - #{endId}";
			fileTag = $"{startId}_{endId}";
			return cards.Where(c => c.Id >= startId && c.Id <= endId).ToList();
		}

		seriesLabel = "All Series (#1 - #700)";
		fileTag = "all";
		return cards.ToList();
	}
}
