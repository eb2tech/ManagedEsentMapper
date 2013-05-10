using System;
using System.Linq;
using EsentMapper.Utility;
using Microsoft.Isam.Esent.Interop;

namespace EsentMapper.Mapping
{
	public class MapPart : IMappingPart, ISchemaPart
	{
		internal MapPart(Type entityType, Member partMember, string partName)
		{
			VerifyTypeSupported(partMember);

			Member = partMember;
			EntityType = entityType;
			Name = partName;
			Length = null;
			ColumndefGrbit = ColumndefGrbit.None;

			var mappers = EsentHelper.GetColumnMappers(Member);
			Getter = mappers.Item1;
			Setter = mappers.Item2;
		}

		internal string Name { get; private set; }
		internal Member Member { get; private set; }
		internal Type EntityType { get; private set; }
		internal int? Length { get; private set; }
		internal ColumndefGrbit ColumndefGrbit { get; private set; }

		private ColumnGetter Getter { get; set; }
		private ColumnSetter Setter { get; set; }

		public MapPart MaxLength(int aLength)
		{
			Length = aLength;
			return this;
		}

		public MapPart Grbit(ColumndefGrbit aGrbit)
		{
			ColumndefGrbit = aGrbit;
			return this;
		}

		void ISchemaPart.ApplyToTable(Session session, JET_TABLEID table)
		{
			if (Api.GetTableColumns(session, table).All(ci => ci.Name != Name))
			{
				var factory = EsentHelper.GetColumnFactory(Member);
				JET_COLUMNID columnid;
				Api.JetAddColumn(session, table, Name, factory(), null, 0, out columnid);
			}
		}

		void IMappingPart.MapColumnToObject(Session session, JET_TABLEID table, object targetObj)
		{
			Latebound binder = new Latebound(targetObj);
			binder[Name] = Getter(session, table, Name);
		}

		void IMappingPart.MapObjectToColumn(Session session, JET_TABLEID table, object sourceObj)
		{
			Latebound binder = new Latebound(sourceObj);
			Setter(session, table, Name, binder[Name]);
		}

		private static void VerifyTypeSupported(Member member)
		{
			bool typeSupported = EsentHelper.MemberIsSupported(member);
			if (!typeSupported)
				throw new ArgumentException(string.Format("{0} is not mappable type.", member.PropertyType));
		}

	}
}