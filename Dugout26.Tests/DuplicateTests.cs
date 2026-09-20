using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Dugout26.Tests;

public class DuplicateTests
{
	[Fact]
	public void Card_IsOwned_SynchronizesWithQuantity()
	{
		var card = new Card { Id = 1, PlayerName = "Shohei Ohtani" };
		Assert.False(card.IsOwned);
		Assert.Equal(0, card.Quantity);

		card.IsOwned = true;
		Assert.True(card.IsOwned);
		Assert.Equal(1, card.Quantity);

		card.Quantity = 3;
		Assert.True(card.IsOwned);
		Assert.Equal(3, card.Quantity);

		card.IsOwned = false;
		Assert.False(card.IsOwned);
		Assert.Equal(0, card.Quantity);
	}

	[Fact]
	public void StorageService_NormalizeCards_MigratesLegacyIsOwnedToQuantityOne()
	{
		var cards = new List<Card>
		{
			new Card { Id = 1, IsOwned = true, Quantity = 0 },
			new Card { Id = 2, IsOwned = false, Quantity = 0 },
			new Card { Id = 3, IsOwned = true, Quantity = 2 }
		};

		StorageService.NormalizeCards(cards);

		Assert.Equal(1, cards[0].Quantity);
		Assert.True(cards[0].IsOwned);

		Assert.Equal(0, cards[1].Quantity);
		Assert.False(cards[1].IsOwned);

		Assert.Equal(2, cards[2].Quantity);
		Assert.True(cards[2].IsOwned);
	}

	[Fact]
	public void DuplicateFilter_IdentifiesDuplicatesCorrectly()
	{
		var cards = new List<Card>
		{
			new Card { Id = 501, PlayerName = "Player A", Quantity = 1 },
			new Card { Id = 502, PlayerName = "Player B", Quantity = 3 },
			new Card { Id = 505, PlayerName = "Player C", Quantity = 2 },
			new Card { Id = 560, PlayerName = "Player D", Quantity = 4 }
		};

		var rangeDups = cards.Where(c => c.Quantity > 1 && c.Id >= 500 && c.Id <= 559).ToList();
		Assert.Equal(2, rangeDups.Count);
		Assert.Equal(502, rangeDups[0].Id);
		Assert.Equal(2, rangeDups[0].Quantity - 1); // 3 total, 2 extra

		Assert.Equal(505, rangeDups[1].Id);
		Assert.Equal(1, rangeDups[1].Quantity - 1); // 2 total, 1 extra

		var totalExtra = rangeDups.Sum(c => c.Quantity - 1);
		Assert.Equal(3, totalExtra);
	}
}
