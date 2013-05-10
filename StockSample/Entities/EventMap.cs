using EsentMapper.Mapping;

namespace StockSample.Entities
{
	public class EventMap : EsentMap<Event>
	{
		public EventMap()
		{
			Id(x => x.Id);
			Map(x => x.InstanceNum);
			Map(x => x.Price);
			Map(x => x.StartTime);
			IndexBy(x => x.StartTime, "StartTime");
		}
	}
}