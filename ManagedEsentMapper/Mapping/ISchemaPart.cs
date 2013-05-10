using Microsoft.Isam.Esent.Interop;

namespace EsentMapper.Mapping
{
	public interface ISchemaPart : IEsentPart
	{
		void ApplyToTable(Session session, JET_TABLEID table);
	}
}