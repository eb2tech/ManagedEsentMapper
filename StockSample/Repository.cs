using System.Collections.Generic;
using EsentMapper;
using EsentMapper.Mapping;
using StockSample.Entities;

namespace StockSample
{
	public class Repository : EsentRepository 
	{
		public Repository(string databasePath, string databaseName)
			: base(databasePath, databaseName)
		{
			
		}

		protected override IEnumerable<EsentMap> GetBuilders()
		{
			yield return new StockMap();
			yield return new EventMap();
		}
	}
}