using System.Text.Json;
using Xunit;

namespace Dugout26.Tests;

public class BackupImportTests
{
	private static JsonSerializerOptions Options() => new() { WriteIndented = true };

	[Fact]
	public void CreateBackup_WritesCollectionAndInsertSets()
	{
		var root = NewTempDir();
		try
		{
			var cards = new List<Card> { new Card { Id = 1, PlayerName = "Aaron Judge", Quantity = 2 } };
			var sets = new List<InsertSet> { new InsertSet { Name = "Titans of the Game", Code = "TOG", OwnedCards = new List<int> { 1 } } };

			var dir = StorageService.CreateBackup(cards, sets, Options(), root);
			Assert.True(Directory.Exists(dir));
			Assert.True(File.Exists(Path.Combine(dir, "collection.json")));
			Assert.True(File.Exists(Path.Combine(dir, "insert_sets.json")));

			Assert.True(StorageService.TryReadSnapshot(dir, Options(), out var loadedCards, out var loadedSets, out var error));
			Assert.Equal(string.Empty, error);
			Assert.NotNull(loadedCards);
			Assert.NotNull(loadedSets);
			Assert.Equal("Aaron Judge", loadedCards![0].PlayerName);
			Assert.Equal(2, loadedCards[0].Quantity);
			Assert.Equal("TOG", loadedSets![0].Code);
			Assert.Equal(new[] { 1 }, loadedSets[0].OwnedCards);
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void TryReadSnapshot_MissingPath_ReturnsError()
	{
		var ok = StorageService.TryReadSnapshot(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), Options(), out _, out _, out var error);
		Assert.False(ok);
		Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void TryReadSnapshot_File_LoadsSiblingInsertSets()
	{
		var root = NewTempDir();
		try
		{
			var cards = new List<Card> { new Card { Id = 24, PlayerName = "Elly De La Cruz", IsOwned = true } };
			var sets = new List<InsertSet> { new InsertSet { Name = "First Pitch", Code = "FP" } };
			var dir = StorageService.CreateBackup(cards, sets, Options(), root);
			var collectionPath = Path.Combine(dir, "collection.json");

			Assert.True(StorageService.TryReadSnapshot(collectionPath, Options(), out var loadedCards, out var loadedSets, out _));
			Assert.NotNull(loadedCards);
			Assert.Single(loadedCards);
			Assert.Equal(24, loadedCards[0].Id);
			Assert.NotNull(loadedSets);
			Assert.Equal("FP", loadedSets![0].Code);
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void ListBackups_ReturnsNewestFirst()
	{
		var root = NewTempDir();
		try
		{
			var cards = new List<Card> { new Card { Id = 1, PlayerName = "A", IsOwned = true } };
			var sets = new List<InsertSet>();
			Directory.CreateDirectory(Path.Combine(root, "2026-01-01_010101"));
			File.WriteAllText(Path.Combine(root, "2026-01-01_010101", "collection.json"), "{\"cards\":[]}");
			Directory.CreateDirectory(Path.Combine(root, "2026-09-20_150000"));
			File.WriteAllText(Path.Combine(root, "2026-09-20_150000", "collection.json"), "{\"cards\":[]}");

			var listed = StorageService.ListBackups(root);
			Assert.Equal(2, listed.Count);
			Assert.Contains("2026-09-20_150000", listed[0]);
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void ImportCollection_ReplacesLiveDataAfterConfirm()
	{
		var root = NewTempDir();
		var liveCards = Path.Combine(root, "live-collection.json");
		var liveSets = Path.Combine(root, "live-inserts.json");
		try
		{
			var currentCards = new List<Card> { new Card { Id = 1, PlayerName = "Old Player", Quantity = 1 } };
			var currentSets = new List<InsertSet> { new InsertSet { Name = "Old Set", Code = "OLD" } };
			var incomingCards = new List<Card> { new Card { Id = 7, PlayerName = "New Player", Quantity = 3 } };
			var incomingSets = new List<InsertSet>
			{
				new InsertSet { Name = "Titans of the Game", Code = "TOG", OwnedCards = new List<int> { 2, 4 } }
			};

			var snapshot = StorageService.CreateBackup(incomingCards, incomingSets, Options(), Path.Combine(root, "incoming"));
			var imported = CommandHandler.ImportCollection(
				snapshot,
				currentCards,
				currentSets,
				liveCards,
				liveSets,
				Options(),
				confirmPrompt: _ => true,
				backupRoot: Path.Combine(root, "safety"));

			Assert.True(imported);
			Assert.Single(currentCards);
			Assert.Equal("New Player", currentCards[0].PlayerName);
			Assert.Equal(3, currentCards[0].Quantity);
			Assert.Contains(currentSets, s => s.Code == "TOG" && s.OwnedCards.Contains(2));
			Assert.True(File.Exists(liveCards));
			Assert.True(StorageService.ListBackups(Path.Combine(root, "safety")).Count >= 1);
		}
		finally
		{
			Cleanup(root);
		}
	}

	[Fact]
	public void ImportCollection_CancelLeavesDataUnchanged()
	{
		var root = NewTempDir();
		try
		{
			var cards = new List<Card> { new Card { Id = 1, PlayerName = "Keep Me", Quantity = 1 } };
			var sets = new List<InsertSet> { new InsertSet { Name = "Keep", Code = "KEEP" } };
			var snapshot = StorageService.CreateBackup(
				new List<Card> { new Card { Id = 9, PlayerName = "Other", Quantity = 1 } },
				new List<InsertSet>(),
				Options(),
				Path.Combine(root, "incoming"));

			var imported = CommandHandler.ImportCollection(
				snapshot,
				cards,
				sets,
				Path.Combine(root, "live-collection.json"),
				Path.Combine(root, "live-inserts.json"),
				Options(),
				confirmPrompt: _ => false,
				backupRoot: Path.Combine(root, "safety"));

			Assert.False(imported);
			Assert.Equal("Keep Me", cards[0].PlayerName);
			Assert.Equal("KEEP", sets[0].Code);
			Assert.Empty(StorageService.ListBackups(Path.Combine(root, "safety")));
		}
		finally
		{
			Cleanup(root);
		}
	}

	private static string NewTempDir()
	{
		var dir = Path.Combine(Path.GetTempPath(), "dugout-backup-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		return dir;
	}

	private static void Cleanup(string dir)
	{
		if (Directory.Exists(dir))
		{
			Directory.Delete(dir, recursive: true);
		}
	}
}
