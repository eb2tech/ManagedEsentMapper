using System;
using Microsoft.Isam.Esent.Interop;

namespace EsentMapper.Mapping
{
	public interface IEsentMapper
	{
		Type EntityType { get; }
		string TableName { get; }
	}

	public interface IEsentMapper<in T> : IEsentMapper where T : class
	{
		void Read(Session session, JET_TABLEID table, T obj);
		void Insert(Session session, JET_TABLEID table, T obj);
		void Update(Session session, JET_TABLEID table, T obj);
		void Delete(Session session, JET_TABLEID table, T obj);
	}
}