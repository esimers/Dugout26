using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

public static class OddsPdfParser
{
	public static List<OddsEntry> LoadOddsFromPdf(string path)
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
					Rarity = CardLogic.ClassifyRarity(name, oneIn)
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

	public static double ParseOneInOdds(string odds)
	{
		var match = Regex.Match(odds, @"(?<left>\d+(?:\.\d+)?)\s*:\s*(?<right>\d+(?:\.\d+)?)");
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

	public static OddsEntry? FindBestOddsMatch(string variantName, List<OddsEntry> oddsEntries)
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
}
