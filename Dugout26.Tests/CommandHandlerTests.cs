using Xunit;

namespace Dugout26.Tests;

public class CommandHandlerTests
{
	[Theory]
	[InlineData("t-91-c", "T91C")]
	[InlineData("8bb", "8BB")]
	[InlineData("  tog 1 ", "TOG1")]
	[InlineData("", "")]
	public void NormalizeInsertCode_RemovesSpecialCharsAndUppercase(string input, string expected)
	{
		var result = CommandHandler.NormalizeInsertCode(input);
		Assert.Equal(expected, result);
	}

	[Fact]
	public void GenerateInsertCode_GeneratesCodeFromInitials()
	{
		var existing = new List<string> { "SMLB" };
		var code = CommandHandler.GenerateInsertCode("First Pitch", existing);
		Assert.Equal("FP", code);
	}

	[Fact]
	public void GenerateInsertCode_AppendsNumberIfCodeExists()
	{
		var existing = new List<string> { "FP" };
		var code = CommandHandler.GenerateInsertCode("First Pitch", existing);
		Assert.Equal("FP2", code);
	}

	[Fact]
	public void TryResolveInsertTarget_ResolvesCodeAndNumber()
	{
		var sets = new List<InsertSet>
		{
			new InsertSet { Name = "Titans of the Game", Code = "TOG" },
			new InsertSet { Name = "Stars of MLB", Code = "SMLB" }
		};

		var resolved = CommandHandler.TryResolveInsertTarget(sets, "TOG14", out var set, out var number);
		Assert.True(resolved);
		Assert.NotNull(set);
		Assert.Equal("TOG", set!.Code);
		Assert.Equal(14, number);
	}

	[Fact]
	public void TryExpandInsertTarget_ExpandsRangeTargets()
	{
		var sets = new List<InsertSet>
		{
			new InsertSet { Name = "Titans of the Game", Code = "TOG" }
		};

		var expanded = CommandHandler.TryExpandInsertTarget(sets, "TOG1-4", out var set, out var numbers, out var error);
		Assert.True(expanded);
		Assert.Null(error);
		Assert.NotNull(set);
		Assert.Equal(new[] { 1, 2, 3, 4 }, numbers);
	}

	[Fact]
	public void TryParseInsertFilters_ParsesKeywordsCorrectly()
	{
		var parsed = CommandHandler.TryParseInsertFilters("owned cat:Retail find:Titans", out var ownershipFilter, out var categoryFilter, out var nameFilter);
		Assert.True(parsed);
		Assert.True(ownershipFilter);
		Assert.Equal("Retail", categoryFilter);
		Assert.Equal("Titans", nameFilter);
	}

	[Fact]
	public void FindUniqueInsertSet_ResolvesExactCodeAndPartialName()
	{
		var sets = new List<InsertSet>
		{
			new InsertSet { Name = "Titans of the Game", Code = "TOG" },
			new InsertSet { Name = "Stars of MLB", Code = "SMLB" }
		};

		Assert.Equal("TOG", CommandHandler.FindUniqueInsertSet(sets, "TOG")!.Code);
		Assert.Equal("TOG", CommandHandler.FindUniqueInsertSet(sets, "Titans")!.Code);
		Assert.Null(CommandHandler.FindUniqueInsertSet(sets, "of"));
	}

	[Fact]
	public void GetInsertCardRows_ListsMissingWithPlayerNames()
	{
		var set = new InsertSet
		{
			Name = "Titans of the Game",
			Code = "TOG",
			ValidCardNumbers = new List<int> { 1, 2, 3 },
			OwnedCards = new List<int> { 1 },
			ValidCards = new List<InsertCardInfo>
			{
				new InsertCardInfo { Number = 1, Name = "Aaron Judge" },
				new InsertCardInfo { Number = 2, Name = "Shohei Ohtani" },
				new InsertCardInfo { Number = 3, Name = "Elly De La Cruz" }
			}
		};

		var missing = CommandHandler.GetInsertCardRows(set, ownedFilter: false);
		Assert.Equal(2, missing.Count);
		Assert.Equal(2, missing[0].Number);
		Assert.Equal("Shohei Ohtani", missing[0].Name);
		Assert.False(missing[0].IsOwned);

		var full = CommandHandler.GetInsertCardRows(set, ownedFilter: null);
		Assert.Equal(3, full.Count);
		Assert.True(full[0].IsOwned);
		Assert.Equal("Aaron Judge", full[0].Name);
	}

	[Fact]
	public void GetInsertCardRows_MissingWithoutChecklist_ReturnsEmpty()
	{
		var set = new InsertSet
		{
			Name = "Custom Set",
			Code = "CS",
			OwnedCards = new List<int> { 1 }
		};

		Assert.False(CommandHandler.HasInsertChecklist(set));
		Assert.Empty(CommandHandler.GetInsertCardRows(set, ownedFilter: false));
	}

	[Fact]
	public void ResetCollectionData_ResetsCardsAndSetsOnDoubleConfirmation()
	{
		var cards = new List<Card>
		{
			new Card { Id = 1, PlayerName = "Judge", IsOwned = true, Variants = new List<CardVariant> { new CardVariant { Name = "Gold", IsOwned = true } } }
		};
		var insertSets = new List<InsertSet>
		{
			new InsertSet { Name = "Titans", Code = "TOG", IsOwned = true, OwnedCards = new List<int> { 1, 2 } }
		};

		var tmpCards = Path.GetTempFileName();
		var tmpSets = Path.GetTempFileName();
		var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };

		try
		{
			var result = CommandHandler.ResetCollectionData(
				cards,
				insertSets,
				tmpCards,
				tmpSets,
				options,
				confirmPrompt: prompt => true,
				stringPrompt: prompt => "RESET");

			Assert.True(result);
			Assert.False(cards[0].IsOwned);
			Assert.Empty(cards[0].Variants);
			Assert.False(insertSets[0].IsOwned);
			Assert.Empty(insertSets[0].OwnedCards);
		}
		finally
		{
			if (File.Exists(tmpCards)) File.Delete(tmpCards);
			if (File.Exists(tmpSets)) File.Delete(tmpSets);
		}
	}

	[Fact]
	public void ResetCollectionData_AbortsIfSecondConfirmationFails()
	{
		var cards = new List<Card>
		{
			new Card { Id = 1, PlayerName = "Judge", IsOwned = true }
		};
		var insertSets = new List<InsertSet>();
		var tmpCards = Path.GetTempFileName();
		var tmpSets = Path.GetTempFileName();
		var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };

		try
		{
			var result = CommandHandler.ResetCollectionData(
				cards,
				insertSets,
				tmpCards,
				tmpSets,
				options,
				confirmPrompt: prompt => true,
				stringPrompt: prompt => "wrong");

			Assert.False(result);
			Assert.True(cards[0].IsOwned);
		}
		finally
		{
			if (File.Exists(tmpCards)) File.Delete(tmpCards);
			if (File.Exists(tmpSets)) File.Delete(tmpSets);
		}
	}
}

