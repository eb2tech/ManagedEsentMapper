using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Isam.Esent.Interop;

namespace EsentMapper.Mapping
{
	public abstract class EsentMap : IEsentSchemaBuilder, IEsentMapper
	{
		public abstract void SyncSchema(Session session, JET_DBID database);
		public abstract Type EntityType { get; }
		public abstract string TableName { get; }
	}

	public class EsentMap<T> : EsentMap, IEsentMapper<T> where T : class
	{
		private readonly List<IEsentPart> _esentParts = new List<IEsentPart>();

		public override Type EntityType
		{
			get { return typeof (T); }
		}

		public override string TableName
		{
			get { return EntityType.Name; }
		}

		void IEsentMapper<T>.Read(Session session, JET_TABLEID table, T obj)
		{
			foreach (var part in _esentParts.OfType<IMappingPart>())
				part.MapColumnToObject(session, table, obj);
		}

		void IEsentMapper<T>.Insert(Session session, JET_TABLEID table, T obj)
		{
			using (var updater = new Update(session, table, JET_prep.Insert))
			{
				foreach (var part in _esentParts.OfType<IMappingPart>())
					part.MapObjectToColumn(session, table, obj);
				updater.Save();
			}
		}

		void IEsentMapper<T>.Update(Session session, JET_TABLEID table, T obj)
		{
			using (var updater = new Update(session, table, JET_prep.Replace))
			{
				foreach (var part in _esentParts.OfType<IMappingPart>())
					part.MapObjectToColumn(session, table, obj);
				updater.Save();
			}
		}

		void IEsentMapper<T>.Delete(Session session, JET_TABLEID table, T obj)
		{
			IdPart idPart = _esentParts.OfType<IdPart>().First();
			IAccessingPart accessingPart = idPart;

			Api.JetSetCurrentIndex(session, table, null);
			EsentHelper.GetMakeKeySetter(idPart.Member)(session, table, accessingPart.GetValue(obj));
			Api.JetSeek(session, table, SeekGrbit.SeekEQ);

			Api.JetDelete(session, table);
		}

		public IdPart Id(Expression<Func<T, object>> memberExpression)
		{
			return Id(memberExpression, null);
		}

		public IdPart Id(Expression<Func<T, object>> memberExpression, string columnName)
		{
			Member member = memberExpression.ToMember();
			string name = (!string.IsNullOrWhiteSpace(columnName)) ? columnName : member.Name;
			IdPart part = new IdPart(EntityType, member, name);

			_esentParts.Add(part);

			return part;
		}

		public MapPart Map(Expression<Func<T, object>> memberExpression)
		{
			return Map(memberExpression, null);
		}

		public MapPart Map(Expression<Func<T, object>> memberExpression, string columnName)
		{
			Member member = memberExpression.ToMember();
			string name = (!string.IsNullOrWhiteSpace(columnName)) ? columnName : member.Name;
			MapPart part = new MapPart(EntityType, member, name);

			_esentParts.Add(part);

			return part;
		}

		public IndexPart IndexBy(Expression<Func<T, object>> memberExpression, string indexName)
		{
			return IndexBy(memberExpression, indexName, null);
		}

		public IndexPart IndexBy(Expression<Func<T, object>> memberExpression, string indexName, string columnName)
		{
			Member member = memberExpression.ToMember();
			string name = (!string.IsNullOrWhiteSpace(columnName)) ? columnName : member.Name;
			IndexPart part = new IndexPart(indexName, EntityType, member, name);

			_esentParts.Add(part);

			return part;
		}

		public override void SyncSchema(Session session, JET_DBID database)
		{
			using (var transaction = new Transaction(session))
			{
				// Verify table exists for EntityType. Create as needed.
				string suggestedTableName = EntityType.Name;
				JET_TABLEID tableid;
				string table = Api.GetTableNames(session, database).FirstOrDefault(tn => tn == suggestedTableName);

				if (string.IsNullOrEmpty(table))
				{
					Api.JetCreateTable(session, database, suggestedTableName, 1, 100, out tableid);
				}
				else
				{
					Api.JetOpenTable(session, database, table, null, 0, OpenTableGrbit.None, out tableid);
				}

				// Apply columns/indexes as needed
				foreach (var part in _esentParts.OfType<ISchemaPart>())
					part.ApplyToTable(session, tableid);

				transaction.Commit(CommitTransactionGrbit.None);
			}
		}
	}
}