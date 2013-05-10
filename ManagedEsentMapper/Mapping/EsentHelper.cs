using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace EsentMapper.Mapping
{
	internal delegate JET_COLUMNDEF ColumnFactory();

	internal delegate void ColumnSetter(Session session, JET_TABLEID tableId, string columnName, object columnValue);

	internal delegate object ColumnGetter(Session session, JET_TABLEID tableId, string columnName);

	internal delegate void MakeKeySetter(Session session, JET_TABLEID tableId, object keyValue, MakeKeyGrbit grbit = MakeKeyGrbit.NewKey);

	internal class MappingEntry
	{
		public MappingEntry(ColumnFactory factory, ColumnGetter getColumn, ColumnSetter setColumn, MakeKeySetter keySetter)
		{
			DefineColumn = factory;
			SetColumn = setColumn;
			GetColumn = getColumn;
			MakeKeySetter = keySetter;
		}

		public MakeKeySetter MakeKeySetter { get; private set; }
		public ColumnFactory DefineColumn { get; private set; }
		public ColumnSetter SetColumn { get; private set; }
		public ColumnGetter GetColumn { get; private set; }
	}

	internal class EsentHelper
	{
		public static bool MemberIsSupported(Member member)
		{
			return maps.Any(kvp => kvp.Key.IsAssignableFrom(member.PropertyType));
		}

		public static Tuple<ColumnGetter, ColumnSetter> GetColumnMappers(Member member)
		{
			var entry = SelectMappingEntry(member, mapping => new Tuple<ColumnGetter, ColumnSetter>(mapping.GetColumn, mapping.SetColumn)).ToArray();
			return entry.Any() ? entry.First() : null;
		}

		public static ColumnFactory GetColumnFactory(Member member)
		{
			var entry = SelectMappingEntry(member, mapping => mapping.DefineColumn).ToArray();
			return entry.Any() ? entry.First() : null;
		}

		public static MakeKeySetter GetMakeKeySetter(Member member)
		{
			var entry = SelectMappingEntry(member, mapping => mapping.MakeKeySetter).ToArray();
			return entry.Any() ? entry.First() : null;
		}

		private static IEnumerable<TResult> SelectMappingEntry<TResult>(Member member, Func<MappingEntry, TResult> projection)
		{
			var entry = from kvp in maps
			            where kvp.Key.IsAssignableFrom(member.PropertyType)
			            select projection(kvp.Value);
			return entry;
		}

		#region Float

		private static JET_COLUMNDEF BuildFloatColumn()
		{
			return new JET_COLUMNDEF
			       {
			       	coltyp = JET_coltyp.IEEESingle,
			       	grbit = ColumndefGrbit.ColumnNotNULL
			       };
		}

		private static void SetFloat(Session session, JET_TABLEID tableId, string columnName, object columnValue)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			Api.SetColumn(session, tableId, columnId, (float) columnValue);
		}

		private static object GetFloat(Session session, JET_TABLEID tableId, string columnName)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			return Api.RetrieveColumnAsFloat(session, tableId, columnId).GetValueOrDefault();
		}

		private static void FloatMakeKey(Session session, JET_TABLEID tableId, object keyValue, MakeKeyGrbit grbit = MakeKeyGrbit.NewKey)
		{
			Api.MakeKey(session, tableId, (float) keyValue, grbit);
		}

		#endregion

		#region Double

		private static JET_COLUMNDEF BuildDoubleColumn()
		{
			return new JET_COLUMNDEF
			       {
			       	coltyp = JET_coltyp.IEEEDouble,
			       	grbit = ColumndefGrbit.ColumnNotNULL
			       };
		}

		private static void SetDouble(Session session, JET_TABLEID tableId, string columnName, object columnValue)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			Api.SetColumn(session, tableId, columnId, (double) columnValue);
		}

		private static object GetDouble(Session session, JET_TABLEID tableId, string columnName)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			return Api.RetrieveColumnAsDouble(session, tableId, columnId).GetValueOrDefault();
		}

		private static void DoubleMakeKey(Session session, JET_TABLEID tableId, object keyValue, MakeKeyGrbit grbit = MakeKeyGrbit.NewKey)
		{
			Api.MakeKey(session, tableId, (double)keyValue, grbit);
		}

		#endregion

		#region int

		private static JET_COLUMNDEF BuildInt32Column()
		{
			return new JET_COLUMNDEF
			       {
			       	coltyp = JET_coltyp.Long,
			       	grbit = ColumndefGrbit.ColumnNotNULL
			       };
		}

		private static void SetInt32(Session session, JET_TABLEID tableId, string columnName, object columnValue)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			Api.SetColumn(session, tableId, columnId, (int) columnValue);
		}

		private static object GetInt32(Session session, JET_TABLEID tableId, string columnName)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			return Api.RetrieveColumnAsInt32(session, tableId, columnId).GetValueOrDefault();
		}

		private static void Int32MakeKey(Session session, JET_TABLEID tableId, object keyValue, MakeKeyGrbit grbit = MakeKeyGrbit.NewKey)
		{
			Api.MakeKey(session, tableId, (int)keyValue, grbit);
		}

		#endregion

		#region short

		private static JET_COLUMNDEF BuildInt16Column()
		{
			return new JET_COLUMNDEF
			       {
			       	coltyp = JET_coltyp.Short,
			       	grbit = ColumndefGrbit.ColumnNotNULL
			       };
		}

		private static void SetInt16(Session session, JET_TABLEID tableId, string columnName, object columnValue)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			Api.SetColumn(session, tableId, columnId, (short) columnValue);
		}

		private static object GetInt16(Session session, JET_TABLEID tableId, string columnName)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			return Api.RetrieveColumnAsInt16(session, tableId, columnId).GetValueOrDefault();
		}

		private static void Int16MakeKey(Session session, JET_TABLEID tableId, object keyValue, MakeKeyGrbit grbit = MakeKeyGrbit.NewKey)
		{
			Api.MakeKey(session, tableId, (short)keyValue, grbit);
		}

		#endregion

		#region string

		private static JET_COLUMNDEF BuildStringColumn()
		{
			return new JET_COLUMNDEF
			       {
			       	coltyp = JET_coltyp.LongText,
			       	cp = JET_CP.Unicode,
			       	grbit = ColumndefGrbit.None
			       };
		}

		private static void SetString(Session session, JET_TABLEID tableId, string columnName, object columnValue)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			Api.SetColumn(session, tableId, columnId, (string) columnValue, Encoding.Unicode);
		}

		private static object GetString(Session session, JET_TABLEID tableId, string columnName)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			return Api.RetrieveColumnAsString(session, tableId, columnId);
		}

		private static void StringMakeKey(Session session, JET_TABLEID tableId, object keyValue, MakeKeyGrbit grbit = MakeKeyGrbit.NewKey)
		{
			Api.MakeKey(session, tableId, (string)keyValue, Encoding.Unicode, grbit);
		}

		#endregion

		#region bool

		private static JET_COLUMNDEF BuildBoolColumn()
		{
			return new JET_COLUMNDEF
			       {
			       	coltyp = JET_coltyp.Bit,
			       	grbit = ColumndefGrbit.None
			       };
		}

		private static void SetBool(Session session, JET_TABLEID tableId, string columnName, object columnValue)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			Api.SetColumn(session, tableId, columnId, (bool) columnValue);
		}

		private static object GetBool(Session session, JET_TABLEID tableId, string columnName)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			return Api.RetrieveColumnAsBoolean(session, tableId, columnId);
		}

		private static void BoolMakeKey(Session session, JET_TABLEID tableId, object keyValue, MakeKeyGrbit grbit = MakeKeyGrbit.NewKey)
		{
			Api.MakeKey(session, tableId, (bool)keyValue, grbit);
		}

		#endregion

		#region Binary Blob

		private static JET_COLUMNDEF BuildBlobColumn()
		{
			return new JET_COLUMNDEF
			       {
			       	coltyp = JET_coltyp.LongBinary,
			       	grbit = ColumndefGrbit.None
			       };
		}

		private static void SetBlob(Session session, JET_TABLEID tableId, string columnName, object columnValue)
		{
			throw new NotImplementedException();
		}

		private static object GetBlob(Session session, JET_TABLEID tableId, string columnName)
		{
			throw new NotImplementedException();
		}

		private static void BlobMakeKey(Session session, JET_TABLEID tableId, object keyValue, MakeKeyGrbit grbit = MakeKeyGrbit.NewKey)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Guid

		private static JET_COLUMNDEF BuildGuidColumn()
		{
			return new JET_COLUMNDEF
			       {
			       	cbMax = 16,
			       	coltyp = JET_coltyp.Binary,
			       	grbit = ColumndefGrbit.ColumnFixed | ColumndefGrbit.ColumnNotNULL
			       };
		}

		private static void SetGuid(Session session, JET_TABLEID tableId, string columnName, object columnValue)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			Api.SetColumn(session, tableId, columnId, (Guid) columnValue);
		}

		private static object GetGuid(Session session, JET_TABLEID tableId, string columnName)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			return Api.RetrieveColumnAsGuid(session, tableId, columnId);
		}

		private static void GuidMakeKey(Session session, JET_TABLEID tableId, object keyValue, MakeKeyGrbit grbit = MakeKeyGrbit.NewKey)
		{
			Api.MakeKey(session, tableId, (Guid)keyValue, grbit);
		}

		#endregion

		#region DateTime

		private static JET_COLUMNDEF BuildDateTimeColumn()
		{
			return new JET_COLUMNDEF
			{
				coltyp = JET_coltyp.DateTime,
				grbit = ColumndefGrbit.ColumnNotNULL
			};
		}

		private static void SetDateTime(Session session, JET_TABLEID tableId, string columnName, object columnValue)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			Api.SetColumn(session, tableId, columnId, (DateTime)columnValue);
		}

		private static object GetDateTime(Session session, JET_TABLEID tableId, string columnName)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			return Api.RetrieveColumnAsDateTime(session, tableId, columnId).GetValueOrDefault();
		}

		private static void DateTimeMakeKey(Session session, JET_TABLEID tableId, object keyValue, MakeKeyGrbit grbit = MakeKeyGrbit.NewKey)
		{
			Api.MakeKey(session, tableId, (DateTime)keyValue, grbit);
		}

		#endregion

		#region Nullable<DateTime>

		private static JET_COLUMNDEF BuildNullableDateTimeColumn()
		{
			return new JET_COLUMNDEF
			{
				coltyp = JET_coltyp.DateTime,
				grbit = ColumndefGrbit.None
			};
		}

		private static void SetNullableDateTime(Session session, JET_TABLEID tableId, string columnName, object columnValue)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			Api.SetColumn(session, tableId, columnId, (DateTime)columnValue);
		}

		private static object GetNullableDateTime(Session session, JET_TABLEID tableId, string columnName)
		{
			var columnId = Api.GetTableColumnid(session, tableId, columnName);
			return Api.RetrieveColumnAsDateTime(session, tableId, columnId);
		}

		private static void NullableDateTimeMakeKey(Session session, JET_TABLEID tableId, object keyValue, MakeKeyGrbit grbit = MakeKeyGrbit.NewKey)
		{
			Api.MakeKey(session, tableId, (DateTime)keyValue, grbit);
		}

		#endregion

		private static readonly Dictionary<Type, MappingEntry> maps = new Dictionary<Type, MappingEntry>()
		                                                              {
		                                                              	{
		                                                              		typeof (float),
		                                                              		new MappingEntry(BuildFloatColumn, GetFloat, SetFloat, FloatMakeKey)
		                                                              		},
		                                                              	{
		                                                              		typeof (double),
		                                                              		new MappingEntry(BuildDoubleColumn, GetDouble, SetDouble, DoubleMakeKey)
		                                                              		},
		                                                              	{
		                                                              		typeof (int),
		                                                              		new MappingEntry(BuildInt32Column, GetInt32, SetInt32, Int32MakeKey)
		                                                              		},
		                                                              	{
		                                                              		typeof (short),
		                                                              		new MappingEntry(BuildInt16Column, GetInt16, SetInt16, Int16MakeKey)
		                                                              		},
		                                                              	{
		                                                              		typeof (string),
		                                                              		new MappingEntry(BuildStringColumn, GetString, SetString, StringMakeKey)
		                                                              		},
		                                                              	{
		                                                              		typeof (bool),
		                                                              		new MappingEntry(BuildBoolColumn, GetBool, SetBool, BoolMakeKey)
		                                                              		},
		                                                              	{
		                                                              		typeof (IEnumerable<byte>),
		                                                              		new MappingEntry(BuildBlobColumn, GetBlob, SetBlob, BlobMakeKey)
		                                                              		},
		                                                              	{
		                                                              		typeof (Guid),
		                                                              		new MappingEntry(BuildGuidColumn, GetGuid, SetGuid, GuidMakeKey)
		                                                              		},
		                                                              	{
		                                                              		typeof (DateTime),
		                                                              		new MappingEntry(BuildDateTimeColumn, GetDateTime, SetDateTime, DateTimeMakeKey)
		                                                              		},
		                                                              	{
		                                                              		typeof (DateTime?),
		                                                              		new MappingEntry(BuildNullableDateTimeColumn, GetNullableDateTime, SetNullableDateTime, NullableDateTimeMakeKey)
		                                                              		},
		                                                              };
	}
}