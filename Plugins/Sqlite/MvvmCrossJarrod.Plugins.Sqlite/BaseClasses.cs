using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MvvmCrossJarrod.Plugins.Sqlite
{
    public interface ISQLiteConnectionFactory
    {
        ISQLiteConnection Create(string address);

        ISQLiteAsyncConnection AsyncCreate(string address);
    }

    [Flags]
    public enum CreateFlags
    {
        None = 0,
        ImplicitPK = 1,    // create a primary key for field called 'Id' (Orm.ImplicitPkName)
        ImplicitIndex = 2, // create an index for fields ending in 'Id' (Orm.ImplicitIndexSuffix)
        AllImplicit = 3,   // do both above

        AutoIncPK = 4      // force PK field to be auto inc
    }

    public class ColumnInfo
    {
        //			public int cid { get; set; }

        [Column("name")]
        public string Name { get; set; }

        //			[Column ("type")]
        //			public string ColumnType { get; set; }

        //			public int notnull { get; set; }

        //			public string dflt_value { get; set; }

        //			public int pk { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class CreateTablesResult
    {
        public Dictionary<Type, int> Results { get; private set; }

        public CreateTablesResult()
        {
            this.Results = new Dictionary<Type, int>();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public string Name { get; set; }

        public TableAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public string Name { get; set; }

        public ColumnAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class AutoIncrementAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class IndexedAttribute : Attribute
    {
        public string Name { get; set; }
        public int Order { get; set; }
        public virtual bool Unique { get; set; }

        public IndexedAttribute()
        {
        }

        public IndexedAttribute(string name, int order)
        {
            Name = name;
            Order = order;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class UniqueAttribute : IndexedAttribute
    {
        public override bool Unique
        {
            get { return true; }
            set { /* throw?  */ }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class MaxLengthAttribute : Attribute
    {
        public int Value { get; private set; }

        public MaxLengthAttribute(int length)
        {
            Value = length;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CollationAttribute : Attribute
    {
        public string Value { get; private set; }

        public CollationAttribute(string collation)
        {
            Value = collation;
        }
    }

    public interface ITableMapping
    {
        string TableName { get; }
    }

    public interface ITableQuery<T> : IEnumerable<T> where T : new()
    {
        ISQLiteConnection Connection { get; }

        ITableQuery<T> Where(Expression<Func<T, bool>> predExpr);

        ITableQuery<T> Take(int n);

        ITableQuery<T> Skip(int n);

        T ElementAt(int index);

        ITableQuery<T> Deferred();

        ITableQuery<T> OrderBy<U>(Expression<Func<T, U>> orderExpr);

        ITableQuery<T> OrderByDescending<U>(Expression<Func<T, U>> orderExpr);

        ITableQuery<TResult> Join<TInner, TKey, TResult>(
            ITableQuery<TInner> inner,
            Expression<Func<T, TKey>> outerKeySelector,
            Expression<Func<TInner, TKey>> innerKeySelector,
            Expression<Func<T, TInner, TResult>> resultSelector) 
            where TResult : new()
            where TInner : new();

        ITableQuery<TResult> Select<TResult>(Expression<Func<T, TResult>> selector) where TResult : new();

        int Count();

        int Count(Expression<Func<T, bool>> predExpr);

        IEnumerator<T> GetEnumerator();

        T First();

        T FirstOrDefault();
    }

    public interface IAsyncTableQuery<T>
        where T : new()
    {
        IAsyncTableQuery<T> Where(Expression<Func<T, bool>> predExpr);

        IAsyncTableQuery<T> Skip(int n);

        IAsyncTableQuery<T> Take(int n);

        IAsyncTableQuery<T> OrderBy<U>(Expression<Func<T, U>> orderExpr);

        IAsyncTableQuery<T> OrderByDescending<U>(Expression<Func<T, U>> orderExpr);

        Task<List<T>> ToListAsync();

        Task<int> CountAsync();

        Task<T> ElementAtAsync(int index);

        Task<T> FirstAsync();

        Task<T> FirstOrDefaultAsync();
    }

    public interface ISQLiteCommand 
    {
        string CommandText { get; set; }
        
        int ExecuteNonQuery();

        IEnumerable<T> ExecuteDeferredQuery<T>();

        List<T> ExecuteQuery<T>();

        List<T> ExecuteQuery<T>(ITableMapping map);

        IEnumerable<T> ExecuteDeferredQuery<T>(ITableMapping map);

        T ExecuteScalar<T>();

        void Bind(string name, object val);
        
        void Bind(object val);
    }

    /// <summary>
    /// Represents an open connection to a SQLite database.
    /// </summary>
    public interface ISQLiteConnection : IDisposable
    {
        string DatabasePath { get; }

        bool TimeExecution { get; set; }

        bool Trace { get; set; }

        bool StoreDateTimeAsTicks { get; }

        /// <summary>
        /// Sets a busy handler to sleep the specified amount of time when a table is locked.
        /// The handler will sleep multiple times until a total time of <see cref="BusyTimeout"/> has accumulated.
        /// </summary>
        TimeSpan BusyTimeout { get; set; }

        /// <summary>
        /// Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <param name="type">
        /// The type whose mapping to the database is returned.
        /// </param>         
        /// <param name="createFlags">
        /// Optional flags allowing implicit PK and indexes based on naming conventions
        /// </param>     
        /// <returns>
        /// The mapping represents the schema of the columns of the database and contains 
        /// methods to set and get properties of objects.
        /// </returns>
        ITableMapping GetMapping(Type type, CreateFlags createFlags = CreateFlags.None);

        /// <summary>
        /// Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <returns>
        /// The mapping represents the schema of the columns of the database and contains 
        /// methods to set and get properties of objects.
        /// </returns>
        ITableMapping GetMapping<T>();

        /// <summary>
        /// Executes a "drop table" on the database.  This is non-recoverable.
        /// </summary>
        int DropTable<T>();

        /// <summary>
        /// Executes a "create table if not exists" on the database. It also
        /// creates any specified indexes on the columns of the table. It uses
        /// a schema automatically generated from the specified type. You can
        /// later access this schema by calling GetMapping.
        /// </summary>
        /// <returns>
        /// The number of entries added to the database schema.
        /// </returns>
        int CreateTable<T>(CreateFlags createFlags = CreateFlags.None);

        /// <summary>
        /// Executes a "create table if not exists" on the database. It also
        /// creates any specified indexes on the columns of the table. It uses
        /// a schema automatically generated from the specified type. You can
        /// later access this schema by calling GetMapping.
        /// </summary>
        /// <param name="ty">Type to reflect to a database table.</param>
        /// <param name="createFlags">Optional flags allowing implicit PK and indexes based on naming conventions.</param>  
        /// <returns>
        /// The number of entries added to the database schema.
        /// </returns>
        int CreateTable(Type ty, CreateFlags createFlags = CreateFlags.None);

        /// <summary>
        /// Creates an index for the specified table and column.
        /// </summary>
        /// <param name="indexName">Name of the index to create</param>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnName">Name of the column to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        int CreateIndex(string indexName, string tableName, string columnName, bool unique = false);

        /// <summary>
        /// Creates an index for the specified table and column.
        /// </summary>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnName">Name of the column to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        int CreateIndex(string tableName, string columnName, bool unique = false);

        /// <summary>
        /// Creates an index for the specified object property.
        /// e.g. CreateIndex<Client>(c => c.Name);
        /// </summary>
        /// <typeparam name="T">Type to reflect to a database table.</typeparam>
        /// <param name="property">Property to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        void CreateIndex<T>(Expression<Func<T, object>> property, bool unique = false);

        List<ColumnInfo> GetTableInfo(string tableName);

        /// <summary>
        /// Creates a new SQLiteCommand given the command text with arguments. Place a '?'
        /// in the command text for each of the arguments.
        /// </summary>
        /// <param name="cmdText">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the command text.
        /// </param>
        /// <returns>
        /// A <see cref="SQLiteCommand"/>
        /// </returns>
        ISQLiteCommand CreateCommand(string cmdText, params object[] ps);

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// Use this method instead of Query when you don't expect rows back. Such cases include
        /// INSERTs, UPDATEs, and DELETEs.
        /// You can set the Trace or TimeExecution properties of the connection
        /// to profile execution.
        /// </summary>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// The number of rows modified in the database as a result of this execution.
        /// </returns>
        int Execute(string query, params object[] args);

        T ExecuteScalar<T>(string query, params object[] args);

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// It returns each row of the result using the mapping automatically generated for
        /// the given type.
        /// </summary>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// An enumerable with one result for each row returned by the query.
        /// </returns>
        List<T> Query<T>(string query, params object[] args) where T : new();

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// It returns each row of the result using the mapping automatically generated for
        /// the given type.
        /// </summary>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// An enumerable with one result for each row returned by the query.
        /// The enumerator will call sqlite3_step on each call to MoveNext, so the database
        /// connection must remain open for the lifetime of the enumerator.
        /// </returns>
        IEnumerable<T> DeferredQuery<T>(string query, params object[] args) where T : new();

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// It returns each row of the result using the specified mapping. This function is
        /// only used by libraries in order to query the database via introspection. It is
        /// normally not used.
        /// </summary>
        /// <param name="map">
        /// A <see cref="TableMapping"/> to use to convert the resulting rows
        /// into objects.
        /// </param>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// An enumerable with one result for each row returned by the query.
        /// </returns>
        List<object> Query(ITableMapping map, string query, params object[] args);

        /// <summary>
        /// Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// in the command text for each of the arguments and then executes that command.
        /// It returns each row of the result using the specified mapping. This function is
        /// only used by libraries in order to query the database via introspection. It is
        /// normally not used.
        /// </summary>
        /// <param name="map">
        /// A <see cref="TableMapping"/> to use to convert the resulting rows
        /// into objects.
        /// </param>
        /// <param name="query">
        /// The fully escaped SQL.
        /// </param>
        /// <param name="args">
        /// Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// An enumerable with one result for each row returned by the query.
        /// The enumerator will call sqlite3_step on each call to MoveNext, so the database
        /// connection must remain open for the lifetime of the enumerator.
        /// </returns>
        IEnumerable<object> DeferredQuery(ITableMapping map, string query, params object[] args);

        /// <summary>
        /// Returns a queryable interface to the table represented by the given type.
        /// </summary>
        /// <returns>
        /// A queryable object that is able to translate Where, OrderBy, and Take
        /// queries into native SQL.
        /// </returns>
        ITableQuery<T> Table<T>() where T : new();

        /// <summary>
        /// Attempts to retrieve an object with the given primary key from the table
        /// associated with the specified type. Use of this method requires that
        /// the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        /// The primary key.
        /// </param>
        /// <returns>
        /// The object with the given primary key. Throws a not found exception
        /// if the object is not found.
        /// </returns>
        T Get<T>(object pk) where T : new();

        /// <summary>
        /// Attempts to retrieve the first object that matches the predicate from the table
        /// associated with the specified type. 
        /// </summary>
        /// <param name="predicate">
        /// A predicate for which object to find.
        /// </param>
        /// <returns>
        /// The object that matches the given predicate. Throws a not found exception
        /// if the object is not found.
        /// </returns>
        T Get<T>(Expression<Func<T, bool>> predicate) where T : new();

        /// <summary>
        /// Attempts to retrieve an object with the given primary key from the table
        /// associated with the specified type. Use of this method requires that
        /// the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        /// The primary key.
        /// </param>
        /// <returns>
        /// The object with the given primary key or null
        /// if the object is not found.
        /// </returns>
        T Find<T>(object pk) where T : new();

        /// <summary>
        /// Attempts to retrieve an object with the given primary key from the table
        /// associated with the specified type. Use of this method requires that
        /// the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        /// The primary key.
        /// </param>
        /// <param name="map">
        /// The TableMapping used to identify the object type.
        /// </param>
        /// <returns>
        /// The object with the given primary key or null
        /// if the object is not found.
        /// </returns>
        object Find(object pk, ITableMapping map);

        /// <summary>
        /// Attempts to retrieve the first object that matches the predicate from the table
        /// associated with the specified type. 
        /// </summary>
        /// <param name="predicate">
        /// A predicate for which object to find.
        /// </param>
        /// <returns>
        /// The object that matches the given predicate or null
        /// if the object is not found.
        /// </returns>
        T Find<T>(Expression<Func<T, bool>> predicate) where T : new();

        /// <summary>
        /// Whether <see cref="BeginTransaction"/> has been called and the database is waiting for a <see cref="Commit"/>.
        /// </summary>
        bool IsInTransaction { get; }

        /// <summary>
        /// Begins a new transaction. Call <see cref="Commit"/> to end the transaction.
        /// </summary>
        /// <example cref="System.InvalidOperationException">Throws if a transaction has already begun.</example>
        void BeginTransaction();

        /// <summary>
        /// Creates a savepoint in the database at the current point in the transaction timeline.
        /// Begins a new transaction if one is not in progress.
        /// 
        /// Call <see cref="RollbackTo"/> to undo transactions since the returned savepoint.
        /// Call <see cref="Release"/> to commit transactions after the savepoint returned here.
        /// Call <see cref="Commit"/> to end the transaction, committing all changes.
        /// </summary>
        /// <returns>A string naming the savepoint.</returns>
        string SaveTransactionPoint();

        /// <summary>
        /// Rolls back the transaction that was begun by <see cref="BeginTransaction"/> or <see cref="SaveTransactionPoint"/>.
        /// </summary>
        void Rollback();

        /// <summary>
        /// Rolls back the savepoint created by <see cref="BeginTransaction"/> or SaveTransactionPoint.
        /// </summary>
        /// <param name="savepoint">The name of the savepoint to roll back to, as returned by <see cref="SaveTransactionPoint"/>.  If savepoint is null or empty, this method is equivalent to a call to <see cref="Rollback"/></param>
        void RollbackTo(string savepoint);

        /// <summary>
        /// Releases a savepoint returned from <see cref="SaveTransactionPoint"/>.  Releasing a savepoint 
        ///    makes changes since that savepoint permanent if the savepoint began the transaction,
        ///    or otherwise the changes are permanent pending a call to <see cref="Commit"/>.
        /// 
        /// The RELEASE command is like a COMMIT for a SAVEPOINT.
        /// </summary>
        /// <param name="savepoint">The name of the savepoint to release.  The string should be the result of a call to <see cref="SaveTransactionPoint"/></param>
        void Release(string savepoint);

        /// <summary>
        /// Commits the transaction that was begun by <see cref="BeginTransaction"/>.
        /// </summary>
        void Commit();

        /// <summary>
        /// Executes <param name="action"> within a (possibly nested) transaction by wrapping it in a SAVEPOINT. If an
        /// exception occurs the whole transaction is rolled back, not just the current savepoint. The exception
        /// is rethrown.
        /// </summary>
        /// <param name="action">
        /// The <see cref="Action"/> to perform within a transaction. <param name="action"> can contain any number
        /// of operations on the connection but should never call <see cref="BeginTransaction"/> or
        /// <see cref="Commit"/>.
        /// </param>
        void RunInTransaction(Action action);

        /// <summary>
        /// Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        /// An <see cref="IEnumerable"/> of the objects to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        int InsertAll(System.Collections.IEnumerable objects);

        /// <summary>
        /// Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        /// An <see cref="IEnumerable"/> of the objects to insert.
        /// </param>
        /// <param name="extra">
        /// Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        int InsertAll(System.Collections.IEnumerable objects, string extra);

        /// <summary>
        /// Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        /// An <see cref="IEnumerable"/> of the objects to insert.
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        int InsertAll(System.Collections.IEnumerable objects, Type objType);

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        int Insert(object obj);

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// If a UNIQUE constraint violation occurs with
        /// some pre-existing object, this function deletes
        /// the old object.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <returns>
        /// The number of rows modified.
        /// </returns>
        int InsertOrReplace(object obj);

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        int Insert(object obj, Type objType);

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// If a UNIQUE constraint violation occurs with
        /// some pre-existing object, this function deletes
        /// the old object.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows modified.
        /// </returns>
        int InsertOrReplace(object obj, Type objType);

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <param name="extra">
        /// Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        int Insert(object obj, string extra);

        /// <summary>
        /// Inserts the given object and retrieves its
        /// auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        /// The object to insert.
        /// </param>
        /// <param name="extra">
        /// Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows added to the table.
        /// </returns>
        int Insert(object obj, string extra, Type objType);

        /// <summary>
        /// Updates all of the columns of a table using the specified object
        /// except for its primary key.
        /// The object is required to have a primary key.
        /// </summary>
        /// <param name="obj">
        /// The object to update. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        /// The number of rows updated.
        /// </returns>
        int Update(object obj);

        /// <summary>
        /// Updates all of the columns of a table using the specified object
        /// except for its primary key.
        /// The object is required to have a primary key.
        /// </summary>
        /// <param name="obj">
        /// The object to update. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <param name="objType">
        /// The type of object to insert.
        /// </param>
        /// <returns>
        /// The number of rows updated.
        /// </returns>
        int Update(object obj, Type objType);

        /// <summary>
        /// Updates all specified objects.
        /// </summary>
        /// <param name="objects">
        /// An <see cref="IEnumerable"/> of the objects to insert.
        /// </param>
        /// <returns>
        /// The number of rows modified.
        /// </returns>
        int UpdateAll(System.Collections.IEnumerable objects);

        /// <summary>
        /// Deletes the given object from the database using its primary key.
        /// </summary>
        /// <param name="objectToDelete">
        /// The object to delete. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        /// The number of rows deleted.
        /// </returns>
        int Delete(object objectToDelete);

        /// <summary>
        /// Deletes the object with the specified primary key.
        /// </summary>
        /// <param name="primaryKey">
        /// The primary key of the object to delete.
        /// </param>
        /// <returns>
        /// The number of objects deleted.
        /// </returns>
        /// <typeparam name='T'>
        /// The type of object.
        /// </typeparam>
        int Delete<T>(object primaryKey);

        /// <summary>
        /// Deletes all the objects from the specified table.
        /// WARNING WARNING: Let me repeat. It deletes ALL the objects from the
        /// specified table. Do you really want to do that?
        /// </summary>
        /// <returns>
        /// The number of objects deleted.
        /// </returns>
        /// <typeparam name='T'>
        /// The type of objects to delete.
        /// </typeparam>
        int DeleteAll<T>();

        void Close();
    }

    public interface ISQLiteAsyncConnection
    {
        Task<CreateTablesResult> CreateTableAsync<T>()
            where T : new();

        Task<CreateTablesResult> CreateTablesAsync<T, T2>()
            where T : new()
            where T2 : new();

        Task<CreateTablesResult> CreateTablesAsync<T, T2, T3>()
            where T : new()
            where T2 : new()
            where T3 : new();

        Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4>()
            where T : new()
            where T2 : new()
            where T3 : new()
            where T4 : new();

        Task<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4, T5>()
            where T : new()
            where T2 : new()
            where T3 : new()
            where T4 : new()
            where T5 : new();

        Task<CreateTablesResult> CreateTablesAsync(params Type[] types);

        Task<int> DropTableAsync<T>()
            where T : new();

        Task<int> InsertAsync(object item);

        Task<int> UpdateAsync(object item);

        Task<int> DeleteAsync(object item);

        Task<T> GetAsync<T>(object pk)
            where T : new();

        Task<T> FindAsync<T>(object pk)
            where T : new();

        Task<T> GetAsync<T>(Expression<Func<T, bool>> predicate)
            where T : new();

        Task<T> FindAsync<T>(Expression<Func<T, bool>> predicate)
            where T : new();

        Task<int> ExecuteAsync(string query, params object[] args);

        Task<int> InsertAllAsync(IEnumerable items);

        [Obsolete("Will cause a deadlock if any call in action ends up in a different thread. Use RunInTransactionAsync(Action<SQLiteConnection>) instead.")]
        Task RunInTransactionAsync(Action<ISQLiteAsyncConnection> action);

        Task RunInTransactionAsync(Action<ISQLiteConnection> action);

        IAsyncTableQuery<T> Table<T>()
            where T : new();

        Task<T> ExecuteScalarAsync<T>(string sql, params object[] args);

        Task<List<T>> QueryAsync<T>(string sql, params object[] args)
            where T : new();
    }
}