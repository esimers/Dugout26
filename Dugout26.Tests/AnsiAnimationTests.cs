using Xunit;

namespace Dugout26.Tests;

public class AnsiAnimationTests
{
	[Fact]
	public void PlayTypewriterHeader_ExecutesWithoutException()
	{
		var exception = Record.Exception(() => AnsiAnimation.PlayTypewriterHeader(colDelayMs: 0));
		Assert.Null(exception);
	}

	[Fact]
	public void PlayBatterSwinging_ExecutesWithoutException()
	{
		// Test that animation frame delays & Spectre markup render without throwing
		var exception = Record.Exception(() => AnsiAnimation.PlayBatterSwinging(frameDelayMs: 0));
		Assert.Null(exception);
	}

	[Fact]
	public void DrawStatsPanel_ExecutesWithoutException()
	{
		var cards = new List<Card>
		{
			new Card { Id = 1, PlayerName = "Aaron Judge", IsOwned = true },
			new Card { Id = 2, PlayerName = "Shohei Ohtani", IsOwned = false }
		};
		var odds = new List<OddsEntry>();
		var inserts = new List<InsertSet>();

		var exception = Record.Exception(() => StatsRenderer.DrawStatsPanel(cards, odds, inserts));
		Assert.Null(exception);
	}
}
