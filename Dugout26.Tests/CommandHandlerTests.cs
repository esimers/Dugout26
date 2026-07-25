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
}
