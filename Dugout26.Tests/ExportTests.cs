using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Dugout26.Tests;

public class ExportTests
{
	[Fact]
	public void ExportMissingCards_Series1Text_CreatesFileWithCorrectHeaderAndContent()
	{
		var cards = new List<Card>
		{
			new Card { Id = 1, PlayerName = "Shohei Ohtani", IsOwned = false },
			new Card { Id = 5, PlayerName = "Aaron Judge", IsOwned = true },
			new Card { Id = 355, PlayerName = "Mike Trout", IsOwned = false }
		};

		var exportedPath = StorageService.ExportMissingCards(cards, "s1", "txt");
		Assert.True(File.Exists(exportedPath));

		var content = File.ReadAllText(exportedPath);
		Assert.Contains("DUGOUT '26 - MISSING BASE CARDS REPORT (Series 1 (#1 - #350))", content);
		Assert.Contains("#  1  Shohei Ohtani", content);
		Assert.DoesNotContain("Aaron Judge", content);
		Assert.DoesNotContain("Mike Trout", content);

		if (File.Exists(exportedPath))
		{
			File.Delete(exportedPath);
		}
	}

	[Fact]
	public void ExportMissingCards_Series2Csv_CreatesValidCsvContent()
	{
		var cards = new List<Card>
		{
			new Card { Id = 1, PlayerName = "Shohei Ohtani", IsOwned = false },
			new Card { Id = 355, PlayerName = "Mike Trout", IsOwned = false },
			new Card { Id = 400, PlayerName = "Mookie Betts", IsOwned = true }
		};

		var exportedPath = StorageService.ExportMissingCards(cards, "s2", "csv");
		Assert.True(File.Exists(exportedPath));

		var lines = File.ReadAllLines(exportedPath);
		Assert.Equal("CardId,PlayerName,Series", lines[0]);
		Assert.Equal("355,Mike Trout,Series 2", lines[1]);
		Assert.Single(lines, l => l.StartsWith("355,"));

		if (File.Exists(exportedPath))
		{
			File.Delete(exportedPath);
		}
	}

	[Fact]
	public void ExportDuplicateCards_Csv_IncludesQuantityAndExtras()
	{
		var cards = new List<Card>
		{
			new Card { Id = 1, PlayerName = "Shohei Ohtani", Quantity = 1 },
			new Card { Id = 5, PlayerName = "Aaron Judge", Quantity = 3 },
			new Card { Id = 355, PlayerName = "Mike Trout", Quantity = 2 }
		};

		var exportedPath = StorageService.ExportDuplicateCards(cards, "s1", "csv");
		Assert.True(File.Exists(exportedPath));

		var lines = File.ReadAllLines(exportedPath);
		Assert.Equal("CardId,PlayerName,Series,Quantity,Extras", lines[0]);
		Assert.Equal("5,Aaron Judge,Series 1,3,2", lines[1]);
		Assert.DoesNotContain(lines, l => l.StartsWith("355,"));
		Assert.DoesNotContain(lines, l => l.StartsWith("1,"));

		if (File.Exists(exportedPath))
		{
			File.Delete(exportedPath);
		}
	}

	[Fact]
	public void ExportDuplicateCards_Text_WritesTradeListHeader()
	{
		var cards = new List<Card>
		{
			new Card { Id = 502, PlayerName = "Player B", Quantity = 4 }
		};

		var exportedPath = StorageService.ExportDuplicateCards(cards, "500-559", "txt");
		Assert.True(File.Exists(exportedPath));

		var content = File.ReadAllText(exportedPath);
		Assert.Contains("DUPLICATE BASE CARDS / TRADE LIST", content);
		Assert.Contains("#502", content);
		Assert.Contains("extra +3", content);
		Assert.Contains("Trade copies: 3", content);

		if (File.Exists(exportedPath))
		{
			File.Delete(exportedPath);
		}
	}
}
