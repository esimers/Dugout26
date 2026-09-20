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
}
