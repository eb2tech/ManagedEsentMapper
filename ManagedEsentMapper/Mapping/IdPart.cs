using System;
using System.Linq;
using EsentMapper.Utility;
using Microsoft.Isam.Esent.Interop;

namespace EsentMapper.Mapping
{
	public class IdPart : IMappingPart, ISchemaPart, IAccessingPart
	{
		private int _density = 100;

		internal IdPart(Type entityType, Member partMember, string partName)
		{
			VerifyTypeSupported(partMember);

			Member = partMember;
			EntityType = entityType;
			Name = partName;

			var mappers = EsentHelper.GetColumnMappers(Member);
			Getter = mappers.Item1;
			Setter = mappers.Item2;
		}

		internal string Name { get; private set; }
		internal Member Member { get; private set; }
		internal Type EntityType { get; private set; }

		private ColumnGetter Getter { get; set; }
		private ColumnSetter Setter { get; set; }

		public IdPart Density(int aDensity)
		{
			_density = aDensity;
			return this;
		}

		void ISchemaPart.ApplyToTable(Session session, JET_TABLEID table)
		{
			var indexDef = IndexHelper.BuildIndexDefinition(Name);

			if (Api.GetTableColumns(session, table).All(ci => ci.Name != Name))
			{
				JET_COLUMNID columnid;
				var factory = EsentHelper.GetColumnFactory(Member);
				Api.JetAddColumn(session, table, Name, factory(), null, 0, out columnid);
				Api.JetCreateIndex(session, table, Name + "_index", CreateIndexGrbit.IndexPrimary, indexDef, indexDef.Length, _density);
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

		object IAccessingPart.GetValue(object sourceObj)
		{
			Latebound binder = new Latebound(sourceObj);
			return binder[Name];
		}

		private static void VerifyTypeSupported(Member member)
		{
			bool typeSupported = EsentHelper.MemberIsSupported(member);
			if (!typeSupported)
				throw new ArgumentException(string.Format("{0} is not mappable type.", member.PropertyType));
		}
	}
}