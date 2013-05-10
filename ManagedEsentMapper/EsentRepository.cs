using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EsentMapper.Mapping;
using Microsoft.Isam.Esent.Interop;

namespace EsentMapper
{
	public abstract class EsentRepository
	{
		protected EsentRepository(string databasePath, string databaseName)
		{
			BasePath = databasePath;
			DatabasePath = Path.Combine(databasePath, databaseName);
			Instance = new Instance(databasePath);

			Initialize();
		}

		private void Initialize()
		{
			Builders = GetBuilders().ToArray();

			Instance.Parameters.CreatePathIfNotExist = true;
			Instance.Parameters.Recovery = true;
			Instance.Parameters.CircularLog = true;
			Instance.Parameters.CleanupMismatchedLogFiles = true;
			Instance.Parameters.LogFileDirectory = Path.Combine(BasePath, "logs");
			Instance.Parameters.TempDirectory = Path.Combine(BasePath, "temp");
			Instance.Parameters.SystemDirectory = Path.Combine(BasePath, "system");
			Instance.Init();

			BegetDatabase();

			using (var session = new Session(Instance))
			{
				JET_DBID dbId;
				Api.JetAttachDatabase(session, DatabasePath, AttachDatabaseGrbit.None);
				Api.JetOpenDatabase(session, DatabasePath, string.Empty, out dbId, OpenDatabaseGrbit.None);
				BuildForDatabase(session, dbId);
			}
		}

		protected Instance Instance { get; private set; }
		protected string BasePath { get; private set; }
		protected string DatabasePath { get; private set; }

		public void Add<T>(T item) where T : class
		{
			ExecuteInTransaction<T>((session, table, mapper) => mapper.Insert(session, table, item));
		}

		public void Update<T>(T item) where T : class
		{
			ExecuteInTransaction<T>((session, table, mapper) => mapper.Update(session, table, item));
		}

		public void Delete<T>(T item) where T : class
		{
			ExecuteInTransaction<T>((session, table, mapper) => mapper.Delete(session, table, item));
		}

		public void ReadAll<T>(Func<T, bool> visitor) where T : class, new()
		{
			ExecuteInTransaction<T>((session, table, mapper) =>
			                               {
			                               	if (Api.TryMoveFirst(session, table))
			                               	{
			                               		bool shouldContinue;
			                               		do
			                               		{
													T instance = new T();
			                               			mapper.Read(session, table, instance);

			                               			shouldContinue = visitor(instance);
			                               		} while (shouldContinue && Api.TryMoveNext(session, table));
			                               	}
			                               });
		}

		public T ReadFirst<T>() where T : class, new()
		{
			T instance = default(T);

			ExecuteInTransaction<T>((session, table, mapper) =>
			                        {
										if (Api.TryMoveFirst(session, table))
										{
											instance = new T();
											mapper.Read(session, table, instance);
										}
			                        });

			return instance;
		}

		public IEnumerable<T> Iterate<T>() where T : class, new()
		{
			using (var session = new Session(Instance))
			{
				JET_DBID dbid;
				Api.JetAttachDatabase(session, DatabasePath, AttachDatabaseGrbit.None);
				Api.JetOpenDatabase(session, DatabasePath, string.Empty, out dbid, OpenDatabaseGrbit.None);

				var mapper = Builders.OfType<IEsentMapper<T>>().First();
				
				using (var table = new Table(session, dbid, mapper.TableName, OpenTableGrbit.None))
				{
					if (Api.TryMoveFirst(session, table))
					{
						do
						{
							T instance = new T();
							mapper.Read(session, table, instance);
							yield return instance;
						} while (Api.TryMoveNext(session, table));
					}
				}
			}
		}

		public IEnumerable<T> IterateOver<T>(ISeekRange range) where T : class, new()
		{
			using (var session = new Session(Instance))
			{
				JET_DBID dbid;
				Api.JetAttachDatabase(session, DatabasePath, AttachDatabaseGrbit.None);
				Api.JetOpenDatabase(session, DatabasePath, string.Empty, out dbid, OpenDatabaseGrbit.None);

				var mapper = Builders.OfType<IEsentMapper<T>>().First();

				using (var table = new Table(session, dbid, mapper.TableName, OpenTableGrbit.None))
				{
					if (range.Seek(session, table))
					{
						do
						{
							T instance = new T();
							mapper.Read(session, table, instance);
							yield return instance;
						} while (Api.TryMoveNext(session, table));
					}
				}
			}
		}

		protected abstract IEnumerable<EsentMap> GetBuilders();

		protected void ExecuteInTransaction<TEntity>(Action<Session, Table, IEsentMapper<TEntity>> dataFunc) where TEntity : class
		{
			using (var session = new Session(Instance))
			{
				JET_DBID dbid;
				Api.JetAttachDatabase(session, DatabasePath, AttachDatabaseGrbit.None);
				Api.JetOpenDatabase(session, DatabasePath, string.Empty, out dbid, OpenDatabaseGrbit.None);

				var mapper = Builders.OfType<EsentMap<TEntity>>().First();

				using (var transaction = new Transaction(session))
				{
					using (var table = new Table(session, dbid, mapper.TableName, OpenTableGrbit.None))
					{
						dataFunc(session, table, mapper);
					}

					transaction.Commit(CommitTransactionGrbit.None);
				}
			}
		}

		private EsentMap[] Builders { get; set; }

		private void BuildForDatabase(Session session, JET_DBID database)
		{
			foreach (var builder in Builders)
				builder.SyncSchema(session, database);
		}

		private void BegetDatabase()
		{
			if (!File.Exists(DatabasePath))
			{
				using (var session = new Session(Instance))
				{
					JET_DBID dbId;
					Api.JetCreateDatabase(session, DatabasePath, null, out dbId, CreateDatabaseGrbit.None);
				}
			}
		}
	}
}