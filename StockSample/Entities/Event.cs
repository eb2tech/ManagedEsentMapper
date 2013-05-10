using System;

namespace StockSample.Entities
{
	public class Event
	{
		public Guid Id { get; set; }
		public int InstanceNum { get; set; }
		public string Description { get; set; }
		public double Price { get; set; }
		public DateTime StartTime { get; set; }
	}
}