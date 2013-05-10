using System;
using System.Linq.Expressions;
using Microsoft.Isam.Esent.Interop;

namespace EsentMapper.Mapping
{
	public interface ISeekRange
	{
		 bool Seek(Session session, JET_TABLEID table);
	}

	public class ExactRange<TEntity> : ISeekRange
	{
		public ExactRange(Expression<Func<TEntity, object>> memberExpression, object exactValue)
		{
			SeekValue = exactValue;
			IndexName = null;
			Member = memberExpression.ToMember();
		}

		public ExactRange(string indexName, Expression<Func<TEntity, object>> memberExpression,  object exactValue)
		{
			SeekValue = exactValue;
			IndexName = indexName;
			Member = memberExpression.ToMember();
		}

		public object SeekValue { get; set; }
		public string IndexName { get; set; }
		internal Member Member { get; set; }

		bool ISeekRange.Seek(Session session, JET_TABLEID table)
		{
			Api.JetSetCurrentIndex(session, table, GetIndexName());
			EsentHelper.GetMakeKeySetter(Member)(session, table, SeekValue);
			bool ret = Api.TrySeek(session, table, SeekGrbit.SeekEQ);
			return ret;
		}

		private string GetIndexName()
		{
			return string.IsNullOrEmpty(IndexName) ? IndexName : IndexName + "_index";
		}
	}

	public class IndexRange : ISeekRange
	{
		public IndexRange(string indexName)
		{
			IndexName = indexName;
		}

		public string IndexName { get; set; }

		bool ISeekRange.Seek(Session session, JET_TABLEID table)
		{
			Api.JetSetCurrentIndex(session, table, GetIndexName());
			bool ret = Api.TryMoveFirst(session, table);
			return ret;
		}

		private string GetIndexName()
		{
			return string.IsNullOrEmpty(IndexName) ? IndexName : IndexName + "_index";
		}
	}

	public class BoundedRange<TEntity> : ISeekRange
	{
		public BoundedRange(Expression<Func<TEntity, object>> memberExpression, object lowerBound, object upperBound)
		{
			LowerBound = lowerBound;
			UpperBound = upperBound;
			Member = memberExpression.ToMember();
		}

		public BoundedRange(string indexName, Expression<Func<TEntity, object>> memberExpression, object lowerBound, object upperBound)
		{
			LowerBound = lowerBound;
			UpperBound = upperBound;
			IndexName = indexName;
			Member = memberExpression.ToMember();
		}

		public object LowerBound { get; set; }
		public object UpperBound { get; set; }
		public string IndexName { get; set; }
		internal Member Member { get; set; }

		public bool Seek(Session session, JET_TABLEID table)
		{
			Api.JetSetCurrentIndex(session, table, GetIndexName());
			EsentHelper.GetMakeKeySetter(Member)(session, table, LowerBound);
			bool ret = Api.TrySeek(session, table, SeekGrbit.SeekGE);
			if (ret)
			{
				EsentHelper.GetMakeKeySetter(Member)(session, table, UpperBound);
				ret = Api.TrySetIndexRange(session, table, SetIndexRangeGrbit.RangeUpperLimit);
			}

			return ret;
		}

		private string GetIndexName()
		{
			return string.IsNullOrEmpty(IndexName) ? IndexName : IndexName + "_index";
		}
	}
}