﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AppCenter.Data.iOS.Bindings;

namespace Microsoft.AppCenter.Data
{
    public partial class PaginatedDocuments<T>
    {
        internal MSPaginatedDocuments internalDocuments { get; }

        public PaginatedDocuments(MSPaginatedDocuments iosDocuments)
        {
            internalDocuments = iosDocuments;
        }  

        bool HasNextPage
        {
            get { return internalDocuments.HasNextPage(); }
        }

        Page<T> CurrentPage
        {
            get
            {
                var page = new Page<T>();
                foreach (var item in internalDocuments.CurrentPage().Items)
                {
                    var doc = new DocumentWrapper<T>
                    {
                        Partition = item.Partition,
                        Id = item.DocumentId,
                        DeserializedValue = JsonConvert.DeserializeObject<T>(item.DeserializedValue),
                        ETag = item.ETag,
                        LastUpdatedDate = (DateTime)item.LastUpdatedDate,
                        FromDeviceCache = item.FromDeviceCache,
                        Error = Data.ConvertErrorToException(item.Error)
                    };

                    page.Items.Add(doc);
                }
                return page;
            }
        }

        Task<Page<T>> GetNextPage()
        {
            var taskCompletionSource = new TaskCompletionSource<Page<T>>();
            internalDocuments.NextPage((internalPage) =>
            {
                var page = new Page<T>();
                foreach (var item in internalPage.Items)
                {
                    var doc = new DocumentWrapper<T>
                    {
                        Partition = item.Partition,
                        Id = item.DocumentId,
                        DeserializedValue = JsonConvert.DeserializeObject<T>(item.DeserializedValue),
                        ETag = item.ETag,
                        LastUpdatedDate = (DateTime)item.LastUpdatedDate,
                        FromDeviceCache = item.FromDeviceCache,
                        Error = Data.ConvertErrorToException(item.Error)
                    };

                    page.Items.Add(doc);
                }
                taskCompletionSource.TrySetResult(page);
            });
            return taskCompletionSource.Task;
        }
    }
}
