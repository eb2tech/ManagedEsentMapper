using EsentMapper.Mapping;
using Microsoft.Isam.Esent.Interop;

namespace StockSample.Entities
{
	public class StockMap : EsentMap<Stock>
	{
		public StockMap()
		{
			//Id(x => x.Id);
			Id(x => x.Symbol);
			Map(x => x.Name)
				.Grbit(ColumndefGrbit.ColumnNotNULL);
			Map(x => x.Price);
			Map(x => x.Shares);

			IndexBy(s => s.Name, "Name")
				.Grbit(CreateIndexGrbit.IndexDisallowNull);
		}
	}
}