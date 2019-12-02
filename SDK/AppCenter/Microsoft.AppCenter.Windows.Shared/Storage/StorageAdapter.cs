// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AppCenter.Utils;
using Microsoft.AppCenter.Utils.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SQLitePCL;
using Microsoft.AppCenter.Windows.Shared.Storage;

namespace Microsoft.AppCenter.Storage
{
    internal class StorageAdapter : IStorageAdapter
    {
        private sqlite3 _db;
        internal Directory _databaseDirectory;
        private readonly string _databasePath;

        public StorageAdapter(string databasePath)
        {
            _databasePath = databasePath;
            var databaseDirectoryPath = System.IO.Path.GetDirectoryName(databasePath);
            if (databaseDirectoryPath != string.Empty)
            {
                _databaseDirectory = new Directory(databaseDirectoryPath);
            }
        }

        private int SqlQueryCreateTable(sqlite3 db, string tableName, List<ColumnMap> scheme)
        {
            var columnsList = new List<string>();
            foreach (var column in scheme)
            {
                var columnData = $"{column.ColumnName} ";
                switch (column.ColumnType)
                {
                    case raw.SQLITE_TEXT:
                    {
                        columnData += "TEXT ";
                        break;
                    }
                    case raw.SQLITE_INTEGER:
                    {
                        columnData += "INTEGER ";
                        break;
                    }
                    case raw.SQLITE_FLOAT:
                    {
                        columnData += "FLOAT ";
                        break;
                    }
                }
                if (column.IsPrimarykey)
                {
                    columnData += "PRIMARY KEY ";
                }
                if (column.IsAutoIncrement)
                {
                    columnData += "AUTOINCREMENT";
                }
                columnsList.Add(columnData);
            }
            var tableClause = string.Join(",", columnsList.ToArray());
            var queryString = $"CREATE TABLE IF NOT EXISTS {tableName} ({tableClause});";
            return ExecuteNonSelectionSqlQuery(db, queryString);
        }

        public Task CreateTableAsync(string tableName, List<ColumnMap> columnMaps)
        {
            return Task.Run(() =>
            {
                int result = SqlQueryCreateTable(_db, tableName, columnMaps);
                if (result != raw.SQLITE_DONE)
                {
                    throw new StorageException($"Failed to create table: {result}");
                }
            });

        }

        private int ExecuteNonSelectionSqlQuery(sqlite3 db, string query)
        {
            int result = raw.sqlite3_prepare_v2(db, query, out var stmt);
            if (result != raw.SQLITE_OK)
            {
                return result;
            }
            result = raw.sqlite3_step(stmt);
            raw.sqlite3_finalize(stmt);
            return result;
        }

        private List<Dictionary<string, object>> ExecuteSelectionSqlQuery(sqlite3 db, string query)
        {
            var resultList = new List<Dictionary<string, object>>();
            int queryResult = raw.sqlite3_prepare_v2(db, query, out var stmt);
            if (queryResult != raw.SQLITE_OK)
            {
                AppCenterLog.Error(AppCenterLog.LogTag, $"Failed to prepare SQL query, result={queryResult}\t{raw.sqlite3_errmsg(_db)}");
                return null;
            }
            while (raw.sqlite3_step(stmt) == raw.SQLITE_ROW)
            {
                Dictionary<string, object> rowData = new Dictionary<string, object>();
                var count = raw.sqlite3_column_count(stmt);
                for (var i = 0; i < count; i++)
                {
                    var nameCol = raw.sqlite3_column_table_name(stmt, i);
                    var typeCol = raw.sqlite3_column_type(stmt, i);
                    object valCol;
                    switch (typeCol)
                    {
                        case raw.SQLITE_FLOAT:
                            valCol = raw.sqlite3_column_double(stmt, i);
                            break;
                        case raw.SQLITE_INTEGER:
                            valCol = raw.sqlite3_column_int(stmt, i);
                            break;
                        case raw.SQLITE_TEXT:
                            // TODO add reflection here
                            valCol = raw.sqlite3_column_text(stmt, i);
                            break;
                        default:
                            valCol = null;
                            break;
                    }
                    rowData.Add(nameCol, valCol);
                }
                resultList.Add(rowData);
            }
            raw.sqlite3_finalize(stmt);
            return resultList;
        }

        public Task<List<Dictionary<string, object>>> GetAsync(string tableName, string whereClause, int? limit = null)
        {
            string limitClause = limit != null ? $"LIMIT {limit}" : String.Empty;
            string query = $"SELECT * FROM {tableName} WHERE {whereClause} {limitClause};";
            List<Dictionary<string, object>> executeResult;
            return Task.FromResult(ExecuteSelectionSqlQuery(_db, query));
        }

        private Task<int> ExecuteCountSqlQuery(sqlite3 db, string tableName, string whereClause)
        {
            return Task.FromResult(ExecuteNonSelectionSqlQuery(db, $"SELECT COUNT(*) FROM {tableName} WHERE {whereClause};"));
        }

        public Task<int> CountAsync(string tableName, string whereClause)
        {
            return ExecuteCountSqlQuery(_db, tableName, whereClause);
        }

       
        private int SqlQueryInsert(sqlite3 db, string tableName, string columnsClause, string valuesClause)
        {
            return ExecuteNonSelectionSqlQuery(db, $"INSERT INTO {tableName}{columnsClause} VALUES {valuesClause};");
        }

        public Task<int> InsertAsync(string tableName, List<List<ColumnValueMap>> valueMaps)
        {
            List<string> stringValues = new List<string>();
            HashSet<string> columnsHashSet = new HashSet<string>();
            foreach (var entry in valueMaps)
            {
                var stringValue = string.Join(",", entry.Select(x =>
                {
                    columnsHashSet.Add(x.ColumnName);
                    if (x.ColumnType == raw.SQLITE_TEXT) return $"\"{x.ColumnValue}\"";
                    return x.ColumnValue;
                }));
                stringValues.Add($"({stringValue})");
            }
            var valuesClause = string.Join(",", stringValues);
            var columnsClause = $"({string.Join(".", columnsHashSet)})";
            return Task.FromResult(SqlQueryInsert(_db, tableName, columnsClause, valuesClause));
        }

        private static StorageException ToStorageException(int errorCode)
        {
            return new StorageException($"SQLitePCLRaw errorCode={errorCode}");
        }

        private int SqlQueryDelete(sqlite3 db, string tableName, string whereClause)
        {
            var numDeleted = ExecuteCountSqlQuery(db, tableName, whereClause).GetAwaiter().GetResult();
            int result = ExecuteNonSelectionSqlQuery(db, $"DELETE FROM {tableName} WHERE {whereClause};");
            if (result == raw.SQLITE_DONE)
            {
               // todo
            }
            return numDeleted;
        }
        
        public Task<int> DeleteAsync(string tableName, string whereClause)
        {
            return Task.FromResult(SqlQueryDelete(_db, tableName, whereClause));
        }

        public Task InitializeStorageAsync()
        {
            return Task.Run(() =>
            {
                if (_databaseDirectory != null)
                {
                    try
                    {
                        _databaseDirectory.Create();
                    }
                    catch (Exception e)
                    {
                        throw new StorageException("Cannot initialize SQLite library.", e);
                    }
                }
                raw.SetProvider(new SQLite3Provider_e_sqlite3());
                if (raw.sqlite3_open(_databasePath, out _db) != raw.SQLITE_OK)
                {
                    throw new StorageException("Failed to open database connection");
                }
            });
        }

        public Task DeleteDatabaseFileAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    raw.sqlite3_close(_db);
                    _db.Dispose();
                    var prefix = _databaseDirectory == null ? Constants.LocalAppData : "";
                    new File(System.IO.Path.Combine(prefix, _databasePath)).Delete();
                }
                catch (Exception e)
                {
                    throw new StorageException(e);
                }
            });
        }
    }
}
