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
}
