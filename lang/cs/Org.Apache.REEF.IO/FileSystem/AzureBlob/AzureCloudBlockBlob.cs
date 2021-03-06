﻿// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.

using System;
using System.IO;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Org.Apache.REEF.IO.FileSystem.AzureBlob
{
    /// <summary>
    /// A proxy class for <see cref="CloudBlockBlob"/>, created mainly
    /// for unit testing purposes.
    /// </summary>
    internal sealed class AzureCloudBlockBlob : ICloudBlockBlob
    {
        private readonly CloudBlockBlob _blob;

        public ICloudBlob Blob
        {
            get
            {
                return _blob;
            }
        }

        public BlobProperties Properties
        {
            get
            {
                return _blob.Properties;
            }
        }

        public CopyState CopyState
        {
            get
            {
                return _blob.CopyState;
            }
        }

        public AzureCloudBlockBlob(Uri uri, StorageCredentials credentials)
        {
            _blob = new CloudBlockBlob(uri, credentials);
        }

        public bool Exists()
        {
            return _blob.Exists();
        }

        public void Delete()
        {
            _blob.Delete();
        }

        public void DeleteIfExists()
        {
            _blob.DeleteIfExists();
        }

        public string StartCopy(Uri source)
        {
            return _blob.StartCopy(source);
        }

        public void DownloadToFile(string path, FileMode mode)
        {
            _blob.DownloadToFile(path, mode);
        }

        public void UploadFromFile(string path, FileMode mode)
        {
            _blob.UploadFromFile(path, mode);
        }

        public void FetchAttributes()
        {
            _blob.FetchAttributes();
        }
    }
}
