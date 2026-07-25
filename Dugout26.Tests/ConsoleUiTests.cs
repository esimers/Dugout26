using Xunit;

namespace Dugout26.Tests;

public class ConsoleUiTests
{
	[Fact]
	public void PrintSeriesReport_ExecutesWithoutException()
	{
		var cards = new List<Card>
		{
			new Card { Id = 1, PlayerName = "Aaron Judge", IsOwned = true },
			new Card { Id = 351, PlayerName = "Shohei Ohtani", IsOwned = false }
		};

		var exception = Record.Exception(() => ConsoleUi.PrintSeriesReport(cards));
		Assert.Null(exception);
	}

	[Fact]
	public void RenderTopFrame_ExecutesWithoutException()
	{
		var cards = new List<Card>
		{
			new Card { Id = 1, PlayerName = "Aaron Judge", IsOwned = true }
		};
		var odds = new List<OddsEntry>();
		var inserts = new List<InsertSet>();

		var exception = Record.Exception(() => ConsoleUi.RenderTopFrame(cards, odds, inserts));
		Assert.Null(exception);
	}

	[Fact]
	public void RenderCommandFrameHeader_ExecutesWithoutException()
	{
		var exception = Record.Exception(() => ConsoleUi.RenderCommandFrameHeader());
		Assert.Null(exception);
	}

	[Fact]
	public void TryParseRange_ParsesValidRange()
	{
		var result = ConsoleUi.TryParseRange("25-80", out var start, out var end);
		Assert.True(result);
		Assert.Equal(25, start);
		Assert.Equal(80, end);
	}

	[Fact]
	public void TryParseRange_HandlesReverseRange()
	{
		var result = ConsoleUi.TryParseRange("80-25", out var start, out var end);
		Assert.True(result);
		Assert.Equal(25, start);
		Assert.Equal(80, end);
	}

	[Fact]
	public void TryParseParallelFilters_ParsesRangeAndRarity()
	{
		var result = ConsoleUi.TryParseParallelFilters("1-100 rare", out var startId, out var endId, out var rarityFilter);
		Assert.True(result);
		Assert.Equal(1, startId);
		Assert.Equal(100, endId);
		Assert.Equal("rare", rarityFilter);
	}

	[Theory]
	[InlineData("Rare", "rare", true)]
	[InlineData("Common/Base", "common", true)]
	[InlineData("Ultra Rare", "ultra rare", true)]
	[InlineData("Rare", "uncommon", false)]
	public void MatchesRarityFilter_ValidatesFilters(string rarity, string filter, bool expectedMatch)
	{
		var matches = ConsoleUi.MatchesRarityFilter(rarity, filter);
		Assert.Equal(expectedMatch, matches);
	}
}
