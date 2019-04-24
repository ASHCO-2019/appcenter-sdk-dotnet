﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.AppCenter.Data
{
    public partial class Data
    {
        /// <summary>
        /// Change the base URL used to make API calls.
        /// </summary>
        /// <param name="apiUrl">API base URL.</param>
        public static void SetTokenExchangeUrl(string apiUrl)
        {
            PlatformSetApiUrl(apiUrl);
        }

        /// <summary>
        /// Check whether the Data service is enabled or not.
        /// </summary>
        /// <returns>A task with result being true if enabled, false if disabled.</returns>
        public static Task<bool> IsEnabledAsync()
        {
            return PlatformIsEnabledAsync();
        }

        /// <summary>
        /// Enable or disable the Data service.
        /// </summary>
        /// <param name="enabled">Enabled.</param>
        /// <returns>A task to monitor the operation.</returns>
        public static Task SetEnabledAsync(bool enabled)
        {
            return PlatformSetEnabledAsync(enabled);
        }

        /// <summary>
        /// Read the specified partition and documentId.
        /// </summary>
        /// <returns>A task with DocumentWrapper.</returns>
        /// <param name="partition">The CosmosDB partition key.</param>
        /// <param name="documentId">The CosmosDB document id.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static Task<DocumentWrapper<T>> Read<T>(string partition, string documentId)
        {
            return PlatformRead<T>(partition, documentId);
        }

        /// <summary>
        /// Read a document.
        /// </summary>
        /// <returns>A task with DocumentWrapper</returns>
        /// <param name="partition">The CosmosDB partition key.</param>
        /// <param name="documentId">The CosmosDB document id.</param>
        /// <param name="readOptions">Options for reading and storing the document.</param>
        public static Task<DocumentWrapper<T>> Read<T>(string partition, string documentId, ReadOptions readOptions)
        {
            return PlatformRead<T>(partition, documentId, readOptions);
        }

        /// <summary>
        /// List (need optional signature to configure page size).
        /// </summary>
        /// <returns>A task with PaginatedDocuments</returns>
        /// <param name="partition">The CosmosDB partition key.</param>
        public static Task<PaginatedDocuments<T>> List<T>(string partition)
        {
            return PlatformList<T>(partition);
        }

        /// <summary>
        /// Create a document.
        /// </summary>
        /// <returns>A task with DocumentWrapper</returns>
        /// <param name="partition">The CosmosDB partition key.</param>
        /// <param name="document">The document to be stored in CosmosDB. Must conform to SerializableDocument protocol.</param>
        /// <param name="documentId">The CosmosDB document id.</param>
        public static Task<DocumentWrapper<T>> Create<T>(string partition, string documentId, T document)
        {
            return PlatformCreate(partition, documentId, document);
        }

        /// <summary>
        /// Create a document.
        /// </summary>
        /// <returns>A task with DocumentWrapper</returns>
        /// <param name="partition">The CosmosDB partition key.</param>
        /// <param name="documentId">The CosmosDB document id.</param>
        /// <param name="document">The document to be stored in CosmosDB. Must conform to SerializableDocument protocol.</param>
        /// <param name="writeOptions">Options for writing and storing the document.</param>
        public static Task<DocumentWrapper<T>> Create<T>(string partition, string documentId, T document, WriteOptions writeOptions)
        {
            return PlatformCreate(partition, documentId, document, writeOptions);
        }

        /// <summary>
        /// Delete a document.
        /// </summary>
        /// <returns>A task with DocumentWrapper</returns>
        /// <param name="partition">The CosmosDB partition key.</param>
        /// <param name="documentId">The CosmosDB document id.</param>
        public static Task<DocumentWrapper<object>> Delete(string partition, string documentId)
        {
            return PlatformDelete(partition, documentId);
        }

        /// <summary>
        /// Replace a document.
        /// </summary>
        /// <returns>A task with DocumentWrapper</returns>
        /// <param name="partition">The CosmosDB partition key..</param>
        /// <param name="documentId">The CosmosDB document id.</param>
        /// <param name="document">The document to be stored in CosmosDB. Must conform to SerializableDocument protocol.</param>
        public static Task<DocumentWrapper<T>> Replace<T>(string partition, string documentId, T document)
        {
            return PlatformReplace(partition, documentId, document);
        }

        /// <summary>
        /// Replace a document.
        /// </summary>
        /// <returns>A task with DocumentWrapper</returns>
        /// <param name="partition">The CosmosDB partition key.</param>
        /// <param name="documentId">The CosmosDB document id.</param>
        /// <param name="document">The document to be stored in CosmosDB. Must conform to SerializableDocument protocol.</param>
        /// <param name="writeOptions">Options for writing and storing the document.</param>
        public static Task<DocumentWrapper<T>> Replace<T>(string partition, string documentId, T document, WriteOptions writeOptions)
        {
            return PlatformReplace(partition, documentId, document, writeOptions);
        }
    }
}
