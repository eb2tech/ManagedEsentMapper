using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace EsentMapper.Mapping
{
	public class IndexPart : ISchemaPart
	{
		private readonly List<IndexSegmentPart> _otherParts = new List<IndexSegmentPart>();
		private IndexSegmentPart _currentPart;
		private int _density = 100;

		internal IndexPart(string indexName, Type entityType, Member partMember, string partName)
		{
			Name = indexName;
			Member = partMember;
			EntityType = entityType;
			IndexGrbit = CreateIndexGrbit.None;

			IndexSegmentPart segment = new IndexSegmentPart() {Name = partName};
			_otherParts.Add(segment);
			_currentPart = segment;
		}

		internal Type EntityType { get; set; }
		internal Member Member { get; set; }
		internal string Name { get; private set; }
		internal CreateIndexGrbit IndexGrbit { get; set; }


		public IndexPart ThenBy<T>(Expression<Func<T, object>> memberExpression)
		{
			return ThenBy(memberExpression, null);
		}

		public IndexPart ThenBy<T>(Expression<Func<T, object>> memberExpression, string columnName)
		{
			Member member = memberExpression.ToMember();
			string name = (!string.IsNullOrWhiteSpace(columnName)) ? columnName : member.Name;
			IndexSegmentPart segment = new IndexSegmentPart() {Name = name};

			_otherParts.Add(segment);
			_currentPart = segment;

			return this;
		}

		public IndexPart Ascending()
		{
			_currentPart.IsAscending = true;
			return this;
		}

		public IndexPart Descending()
		{
			_currentPart.IsAscending = false;
			return this;
		}

		public IndexPart Grbit(CreateIndexGrbit aGrbit)
		{
			IndexGrbit = aGrbit;
			return this;
		}

		public IndexPart Density(int aDensity)
		{
			_density = aDensity;
			return this;
		}

		void ISchemaPart.ApplyToTable(Session session, JET_TABLEID table)
		{
			var indexSegments = _otherParts.Select(segment => new Tuple<string, bool>(segment.Name, segment.IsAscending)).ToArray();
			var indexDef = IndexHelper.BuildIndexDefinition(indexSegments);
			var indexName = Name + "_index";

			if (!Api.GetTableIndexes(session, table).Any(ii => ii.Name == indexName))
			{
				Api.JetCreateIndex(session, table, indexName, IndexGrbit, indexDef, indexDef.Length, _density);
			}
		}

		private class IndexSegmentPart
		{
			public IndexSegmentPart()
			{
				IsAscending = true;
			}

			public string Name { get; set; }
			public bool IsAscending { get; set; }
		}
	}

	internal static class IndexHelper
	{
		private const string terminator = "\0\0";
		private const string ascendingIndicator = "+";
		private const string descendingIndicator = "-";

		public static string BuildIndexDefinition(string columnName, bool isAscending = true)
		{
			string indexDef = string.Format("{0}{1}", BuildSingleDefinition(columnName, isAscending), terminator);
			return indexDef;
		}

		public static string BuildIndexDefinition(IEnumerable<Tuple<string, bool>> indexSegments)
		{
			StringBuilder sb = new StringBuilder();

			foreach (var segment in indexSegments)
			{
				sb.Append(BuildSingleDefinition(segment.Item1, segment.Item2));
			}

			sb.Append(terminator);

			string indexDef = sb.ToString();
			return indexDef;
		}

		private static string BuildSingleDefinition(string columnName, bool isAscending)
		{
			string indexDef = string.Format("{0}{1}", isAscending ? ascendingIndicator : descendingIndicator, columnName);
			return indexDef;
		}
	}
}