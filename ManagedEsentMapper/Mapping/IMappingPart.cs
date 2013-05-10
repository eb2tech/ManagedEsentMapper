using Microsoft.Isam.Esent.Interop;

namespace EsentMapper.Mapping
{
	internal interface IMappingPart : IEsentPart
	{
		void MapObjectToColumn(Session session, JET_TABLEID table, object sourceObj);
		void MapColumnToObject(Session session, JET_TABLEID table, object targetObj);
	}
}