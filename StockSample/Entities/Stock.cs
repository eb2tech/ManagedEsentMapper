namespace StockSample.Entities
{
	public class Stock
	{
		// ToDo: Need auto incrementing ID public int Id { get; set; }
		public string Symbol { get; set; }
		public string Name { get; set; }
		public int Price { get; set; }
		public int Shares { get; set; }
	}
}