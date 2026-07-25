using Xunit;

namespace Dugout26.Tests;

public class VariantParserTests
{
	[Fact]
	public void Parse_NumberedSerial_ParsesSerialAndPrintRun()
	{
		var variant = VariantParser.Parse("Gold #45/2026", "1:10", "Common");
		Assert.Equal("Gold", variant.Name);
		Assert.Equal("45/2026", variant.SerialNum);
		Assert.Equal(2026, variant.PrintRun);
		Assert.False(variant.IsAuto);
		Assert.False(variant.Is1Of1);
	}

	[Fact]
	public void Parse_PrintRunOnly_ParsesPrintRun()
	{
		var variant = VariantParser.Parse("Independence Day /75");
		Assert.Equal("Independence Day", variant.Name);
		Assert.Null(variant.SerialNum);
		Assert.Equal(75, variant.PrintRun);
		Assert.False(variant.IsAuto);
	}

	[Fact]
	public void Parse_OneOfOne_Identifies1Of1()
	{
		var variant = VariantParser.Parse("Superfractor 1/1");
		Assert.Equal("Superfractor", variant.Name);
		Assert.Equal("1/1", variant.SerialNum);
		Assert.Equal(1, variant.PrintRun);
		Assert.True(variant.Is1Of1);
	}

	[Fact]
	public void Parse_AutographAndRelic_FlagsProperties()
	{
		var variant = VariantParser.Parse("Red Autograph Relic /10");
		Assert.Equal("Red Autograph Relic", variant.Name);
		Assert.Equal(10, variant.PrintRun);
		Assert.True(variant.IsAuto);
		Assert.True(variant.IsRelic);
	}
}
