public class Card
{
	private bool _isOwned;
	private int _quantity;

	public int Id { get; set; }
	public string PlayerName { get; set; } = string.Empty;

	public bool IsOwned
	{
		get => _quantity > 0 || _isOwned;
		set
		{
			_isOwned = value;
			if (value)
			{
				if (_quantity == 0) _quantity = 1;
			}
			else
			{
				_quantity = 0;
			}
		}
	}

	public int Quantity
	{
		get => _quantity;
		set
		{
			_quantity = value;
			if (_quantity > 0)
			{
				_isOwned = true;
			}
		}
	}

	public List<CardVariant> Variants { get; set; } = new();
}
