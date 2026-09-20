using System.Text.RegularExpressions;

public static class VariantParser
{
	public static CardVariant Parse(string input, string? oddsText = null, string rarity = "Unknown")
	{
		var text = input.Trim();
		string? serialNum = null;
		int? printRun = null;

		// 1. Match specific serial number: e.g. #45/2026, 45/2026, 1/1
		var serialMatch = Regex.Match(text, @"(?<full>(?:#\s*)?(?<curr>\d+)\s*/\s*(?<total>\d+))", RegexOptions.IgnoreCase);
		if (serialMatch.Success)
		{
			var curr = int.Parse(serialMatch.Groups["curr"].Value);
			var total = int.Parse(serialMatch.Groups["total"].Value);

			serialNum = $"{curr}/{total}";
			printRun = total;

			// Remove serial number text from variant clean name
			text = text.Replace(serialMatch.Groups["full"].Value, string.Empty).Trim();
			text = Regex.Replace(text, @"#\s*$", string.Empty).Trim();
		}
		else
		{
			// 2. Match unnumbered print run: e.g. /2026, /75, /50, /1
			var printRunMatch = Regex.Match(text, @"/\s*(?<total>\d+)", RegexOptions.IgnoreCase);
			if (printRunMatch.Success)
			{
				printRun = int.Parse(printRunMatch.Groups["total"].Value);
				text = text.Replace(printRunMatch.Value, string.Empty).Trim();
			}
			else if (Regex.IsMatch(text, @"\b1\s*of\s*1\b", RegexOptions.IgnoreCase))
			{
				printRun = 1;
				serialNum = "1/1";
				text = Regex.Replace(text, @"\b1\s*of\s*1\b", string.Empty, RegexOptions.IgnoreCase).Trim();
			}
		}

		// Clean up leftover symbols or trailing spaces
		text = Regex.Replace(text, @"\s+", " ").Trim();
		if (text.Length == 0)
		{
			text = input.Trim();
		}

		var isAuto = Regex.IsMatch(input, @"\b(auto|autograph|sig|signature)\b", RegexOptions.IgnoreCase);
		var isRelic = Regex.IsMatch(input, @"\b(relic|patch|jersey|bat|memorabilia)\b", RegexOptions.IgnoreCase);

		return new CardVariant
		{
			Name = text,
			IsOwned = true,
			Odds = oddsText,
			Rarity = rarity,
			PrintRun = printRun,
			SerialNum = serialNum,
			IsAuto = isAuto,
			IsRelic = isRelic
		};
	}

	public static bool NameMatches(CardVariant variant, string input)
	{
		var raw = input.Trim();
		if (raw.Length == 0)
		{
			return false;
		}

		if (variant.Name.Equals(raw, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		var parsedName = Parse(raw).Name;
		return variant.Name.Equals(parsedName, StringComparison.OrdinalIgnoreCase);
	}
}
