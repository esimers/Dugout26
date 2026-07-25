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
}
