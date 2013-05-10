using Microsoft.Isam.Esent.Interop;

namespace EsentMapper.Mapping
{
	public interface IEsentSchemaBuilder
	{
		void SyncSchema(Session session, JET_DBID database);
	}
}