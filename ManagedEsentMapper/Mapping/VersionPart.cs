using System;
using System.Linq;
using EsentMapper.Utility;
using Microsoft.Isam.Esent.Interop;

namespace EsentMapper.Mapping
{
	public class VersionPart : ISchemaPart, IMappingPart
	{
		internal VersionPart(Type entityType, Member partMember, string partName)
		{
			if (!EsentHelper.MemberIsSupported(partMember))
				throw new ArgumentException(string.Format("{0} is not mappable type.", partMember.PropertyType));

			Member = partMember;
			EntityType = entityType;
			Name = partName;

			var mappers = EsentHelper.GetColumnMappers(partMember);
			Getter = mappers.Item1;
		}

		internal string Name { get; private set; }
		internal Member Member { get; private set; }
		internal Type EntityType { get; private set; }
		private ColumnGetter Getter { get; set; }

		void ISchemaPart.ApplyToTable(Session session, JET_TABLEID table)
		{
			if (!Api.GetTableColumns(session, table).Any(ci => ci.Name == Name))
			{
				JET_COLUMNID columnid;
				Api.JetAddColumn(session, table, Name,
				                 new JET_COLUMNDEF {coltyp = JET_coltyp.Long, grbit = ColumndefGrbit.ColumnVersion}, null, 0,
				                 out columnid);
			}
		}

		void IMappingPart.MapObjectToColumn(Session session, JET_TABLEID table, object sourceObj)
		{
			// Nothing to do
		}

		void IMappingPart.MapColumnToObject(Session session, JET_TABLEID table, object targetObj)
		{
			Latebound wrapper = new Latebound(targetObj);
			wrapper[Name] = Getter(session, table, Name);
		}
	}
}