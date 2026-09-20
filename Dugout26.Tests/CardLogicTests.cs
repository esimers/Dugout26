using Xunit;

namespace Dugout26.Tests;

public class CardLogicTests
{
	[Theory]
	[InlineData("Base Gold", 10, "Common/Base")]
	[InlineData("Gold Foil", 4, "Common")]
	[InlineData("Rainbow Foil", 24, "Uncommon")]
	[InlineData("Vintage Stock", 90, "Rare")]
	[InlineData("Platinum 1/1", 500, "Ultra Rare")]
	public void ClassifyRarity_ReturnsExpectedRarity(string name, double oneIn, string expectedRarity)
	{
		var result = CardLogic.ClassifyRarity(name, oneIn);
		Assert.Equal(expectedRarity, result);
	}

	[Fact]
	public void ParseIds_SingleId_ReturnsSingleItem()
	{
		var ids = CardLogic.ParseIds("5");
		Assert.Single(ids);
		Assert.Contains(5, ids);
	}

	[Fact]
	public void ParseIds_MultipleCommaSeparated_ReturnsAllIds()
	{
		var ids = CardLogic.ParseIds("1, 5, 10");
		Assert.Equal(3, ids.Count);
		Assert.Contains(1, ids);
		Assert.Contains(5, ids);
		Assert.Contains(10, ids);
	}

	[Fact]
	public void ParseIds_RangeString_ExpandsRange()
	{
		var ids = CardLogic.ParseIds("10-14");
		Assert.Equal(5, ids.Count);
		Assert.Equal(new[] { 10, 11, 12, 13, 14 }, ids.OrderBy(i => i));
	}

	[Fact]
	public void ParseIds_ReversedRangeString_HandlesGracefully()
	{
		var ids = CardLogic.ParseIds("14-10");
		Assert.Equal(5, ids.Count);
		Assert.Equal(new[] { 10, 11, 12, 13, 14 }, ids.OrderBy(i => i));
	}

	[Fact]
	public void ParseIds_MixedInput_ParsesCorrectly()
	{
		var ids = CardLogic.ParseIds("1, 5, 10-12, 20");
		Assert.Equal(6, ids.Count);
		Assert.Equal(new[] { 1, 5, 10, 11, 12, 20 }, ids.OrderBy(i => i));
	}

	[Fact]
	public void GetInsertProgress_CalculatesOwnedAndTotalCorrectly()
	{
		var set = new InsertSet
		{
			Name = "Titans of the Game",
			Code = "TOG",
			ValidCardNumbers = new List<int> { 1, 2, 3, 4, 5 },
			OwnedCards = new List<int> { 1, 3 }
		};

		var progress = CardLogic.GetInsertProgress(set);
		Assert.Equal(2, progress.OwnedCount);
		Assert.Equal(5, progress.TotalCount);
		Assert.False(progress.IsComplete);

		set.OwnedCards = new List<int> { 1, 2, 3, 4, 5 };
		var completeProgress = CardLogic.GetInsertProgress(set);
		Assert.True(completeProgress.IsComplete);
	}

	[Fact]
	public void ApplyHave_MarksMissingCardOwnedWithoutCreatingDuplicate()
	{
		var card = new Card { Id = 24, PlayerName = "Aaron Judge" };
		var first = CardLogic.ApplyHave(card);
		Assert.True(first.Changed);
		Assert.True(card.IsOwned);
		Assert.Equal(1, card.Quantity);

		var second = CardLogic.ApplyHave(card);
		Assert.False(second.Changed);
		Assert.Equal(1, card.Quantity);
		Assert.Contains("dup 24", second.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ApplyDup_RequiresOwnedThenIncrementsQuantity()
	{
		var card = new Card { Id = 24, PlayerName = "Aaron Judge" };
		var missing = CardLogic.ApplyDup(card);
		Assert.False(missing.Changed);
		Assert.Equal(0, card.Quantity);

		CardLogic.ApplyHave(card);
		var extra = CardLogic.ApplyDup(card);
		Assert.True(extra.Changed);
		Assert.Equal(2, card.Quantity);
	}

	[Fact]
	public void ApplyUnhave_DecrementsThenMarksMissing()
	{
		var card = new Card { Id = 24, PlayerName = "Aaron Judge", Quantity = 2 };
		var extraRemoved = CardLogic.ApplyUnhave(card);
		Assert.True(extraRemoved.Changed);
		Assert.True(card.IsOwned);
		Assert.Equal(1, card.Quantity);

		var lastRemoved = CardLogic.ApplyUnhave(card);
		Assert.True(lastRemoved.Changed);
		Assert.False(card.IsOwned);
		Assert.Equal(0, card.Quantity);

		var alreadyMissing = CardLogic.ApplyUnhave(card);
		Assert.False(alreadyMissing.Changed);
	}

	[Fact]
	public void FindCards_MatchesPlayerNameCaseInsensitive()
	{
		var cards = new List<Card>
		{
			new Card { Id = 1, PlayerName = "Aaron Judge" },
			new Card { Id = 17, PlayerName = "Shohei Ohtani" },
			new Card { Id = 24, PlayerName = "Aaron Nola" }
		};

		var judge = CardLogic.FindCards(cards, "judge");
		Assert.Single(judge);
		Assert.Equal(1, judge[0].Id);

		var aaron = CardLogic.FindCards(cards, "AARON");
		Assert.Equal(2, aaron.Count);

		var byId = CardLogic.FindCards(cards, "17");
		Assert.Single(byId);
		Assert.Equal("Shohei Ohtani", byId[0].PlayerName);
	}

	[Fact]
	public void FilterBySeriesArg_FiltersSeriesAndRange()
	{
		var cards = new List<Card>
		{
			new Card { Id = 1, PlayerName = "A" },
			new Card { Id = 355, PlayerName = "B" },
			new Card { Id = 500, PlayerName = "C" }
		};

		var s1 = CardLogic.FilterBySeriesArg(cards, "s1", out var s1Label, out var s1Tag);
		Assert.Single(s1);
		Assert.Equal(1, s1[0].Id);
		Assert.Equal("series1", s1Tag);
		Assert.Contains("Series 1", s1Label);

		var range = CardLogic.FilterBySeriesArg(cards, "400-559", out _, out var rangeTag);
		Assert.Single(range);
		Assert.Equal(500, range[0].Id);
		Assert.Equal("400_559", rangeTag);
	}

	[Fact]
	public void FormatOwnership_ReportsQuantity()
	{
		var missing = new Card { Id = 1 };
		Assert.Equal("MISSING", CardLogic.FormatOwnership(missing));

		var owned = new Card { Id = 1, Quantity = 1 };
		Assert.Equal("OWNED (x1)", CardLogic.FormatOwnership(owned));

		var dups = new Card { Id = 1, Quantity = 3 };
		Assert.Equal("OWNED (x3)", CardLogic.FormatOwnership(dups));
	}
}
