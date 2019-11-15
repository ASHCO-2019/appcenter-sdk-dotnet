// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AppCenter.Utils;
using Microsoft.AppCenter.Utils.Files;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AppCenter.Storage
{
    internal class StorageAdapter : IStorageAdapter
    {
        private SQLiteAsyncConnection _dbConnection;
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

        public async Task CreateTableAsync<T>() where T : new()
        {
            try
            {
                // In SQLite-net 1.5 return type was changed.
                // Using reflection to accept newer library version.
                var task = (Task)_dbConnection.GetType()
                    .GetMethod("CreateTableAsync", new[] { typeof(CreateFlags) })
                    .MakeGenericMethod(typeof(T))
                    .Invoke(_dbConnection, new object[] { CreateFlags.None });
                await task.ConfigureAwait(false);
            }
            catch (SQLiteException e)
            {
                throw ToStorageException(e);
            }
        }

        public async Task<List<T>> GetAsync<T>(Expression<Func<T, bool>> pred, int limit) where T : new()
        {
            try
            {
                var table = _dbConnection.Table<T>();
                return await table.Where(pred).Take(limit).ToListAsync().ConfigureAwait(false);
            }
            catch (SQLiteException e)
            {
                throw ToStorageException(e);
            }
        }

        public Task<int> CountAsync<T>(Expression<Func<T, bool>> pred) where T : new()
        {
            var table = _dbConnection.Table<T>();
            return table.Where(pred).CountAsync();
        }

        public Task<int> InsertAsync<T>(T val) where T : new()
        {
            try
            {
                return _dbConnection.InsertAsync(val);
            }
            catch (SQLiteException e)
            {
                throw ToStorageException(e);
            }
        }

        private static StorageException ToStorageException(SQLiteException e)
        {
            return new StorageException($"SQLite errorCode={e.Result}", e);
        }

        public async Task<int> DeleteAsync<T>(Expression<Func<T, bool>> pred) where T : new()
        {
            try
            {
                var numDeleted = 0;
                var table = _dbConnection.Table<T>();
                var entries = await table.Where(pred).ToListAsync().ConfigureAwait(false);
                foreach (var entry in entries)
                {
                    numDeleted += await _dbConnection.DeleteAsync(entry).ConfigureAwait(false);
                }
                return numDeleted;
            }
            catch (SQLiteException e)
            {
                throw ToStorageException(e);
            }
        }

        public Task InitializeStorageAsync()
        {
            return Task.Run(() =>
            {
                // Create the directory in case it does not exist.
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

                // In SQLite-net 1.5.231 constructor parameters were changed.
                // Using reflection to accept newer library version.
                _dbConnection = (SQLiteAsyncConnection)typeof(SQLiteAsyncConnection)
                    .GetConstructor(new[] { typeof(string), typeof(bool) })
                    ?.Invoke(new object[] { _databasePath, true });
                if (_dbConnection == null)
                {
                    _dbConnection = (SQLiteAsyncConnection)typeof(SQLiteAsyncConnection)
                        .GetConstructor(new[] { typeof(string), typeof(bool), typeof(object) })
                        ?.Invoke(new object[] { _databasePath, true, null });
                }
                if (_dbConnection == null)
                {
                    throw new StorageException("Cannot initialize SQLite library.");
                }
            });
        }

        public Task DeleteDatabaseFileAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    // We can't delete the file and recreate without invalidating the connection pool.
                    // This is explained in details at https://chrisriesgo.com/sqlite-net-async-connections-keep-it-clean/.
                    SQLiteAsyncConnection.ResetPool();
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
