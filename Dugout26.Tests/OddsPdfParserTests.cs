using Xunit;

namespace Dugout26.Tests;

public class OddsPdfParserTests
{
	[Theory]
	[InlineData("1:24", 24.0)]
	[InlineData("1:4", 4.0)]
	[InlineData("1:4.5", 4.5)]
	[InlineData("invalid", double.PositiveInfinity)]
	public void ParseOneInOdds_ReturnsCalculatedProbability(string oddsText, double expected)
	{
		var result = OddsPdfParser.ParseOneInOdds(oddsText);
		Assert.Equal(expected, result);
	}

	[Fact]
	public void FindBestOddsMatch_MatchesExactOrPartialName()
	{
		var entries = new List<OddsEntry>
		{
			new OddsEntry { Name = "Gold Foil", OddsText = "1:2", OneIn = 2, Rarity = "Common" },
			new OddsEntry { Name = "Rainbow Foil", OddsText = "1:24", OneIn = 24, Rarity = "Uncommon" }
		};

		var matchExact = OddsPdfParser.FindBestOddsMatch("Gold Foil", entries);
		Assert.NotNull(matchExact);
		Assert.Equal("Gold Foil", matchExact!.Name);

		var matchPartial = OddsPdfParser.FindBestOddsMatch("Rainbow", entries);
		Assert.NotNull(matchPartial);
		Assert.Equal("Rainbow Foil", matchPartial!.Name);
	}
}
