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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Org.Apache.REEF.IO.PartitionedData;
using Org.Apache.REEF.IO.PartitionedData.FileSystem;
using Org.Apache.REEF.IO.PartitionedData.FileSystem.Parameters;
using Org.Apache.REEF.IO.TempFileCreation;
using Org.Apache.REEF.Tang.Annotations;
using Org.Apache.REEF.Tang.Formats;
using Org.Apache.REEF.Tang.Implementations.Tang;
using Org.Apache.REEF.Tang.Util;
using Org.Apache.REEF.Utilities.Logging;
using Xunit;

namespace Org.Apache.REEF.IO.Tests
{
    /// <summary>
    /// Tests for Org.Apache.REEF.IO.PartitionedData.FileSystem.
    /// </summary>
    public class TestFilePartitionInputDataSet
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(TestFilePartitionInputDataSet));

        const string tempFileName1 = "REEF.TestLocalFileSystem1.tmp";
        const string tempFileName2 = "REEF.TestLocalFileSystem2.tmp";
        string sourceFilePath1 = Path.Combine(Path.GetTempPath(), tempFileName1);
        string sourceFilePath2 = Path.Combine(Path.GetTempPath(), tempFileName2);

        [Fact]
        public void TestDataSetId()
        {
            string filePaths = string.Format(CultureInfo.CurrentCulture, "{0};{1};{2};{3}", "/tmp/abc", "tmp//cde.txt", "efg", "tmp\\hhh");

            var dataSet = TangFactory.GetTang()
                .NewInjector(FileSystemInputPartitionConfiguration<IEnumerable<byte>>.ConfigurationModule
                    .Set(FileSystemInputPartitionConfiguration<IEnumerable<byte>>.FilePathForPartitions, filePaths)
                    .Set(FileSystemInputPartitionConfiguration<IEnumerable<byte>>.CopyToLocal, "true")
                    .Set(FileSystemInputPartitionConfiguration<IEnumerable<byte>>.FileSerializerConfig,
                        GetByteSerializerConfigString())
                    .Build())
                .GetInstance<IPartitionedInputDataSet>();

            Assert.Equal(dataSet.Id, "FileSystemDataSet-hhh");
        }

        /// <remarks>
        /// This test creates IPartitionDataSet with FileSystemInputPartitionConfiguration module.
        /// It then instantiates each IInputPartition using the IConfiguration provided by the IPartitionDescriptor.
        /// </remarks>
        [Fact]
        public void TestEvaluatorSideWithMultipleFilesOnePartition()
        {
            MakeLocalTestFile(sourceFilePath1, new byte[] { 111, 112, 113 });
            MakeLocalTestFile(sourceFilePath2, new byte[] { 114, 115, 116, 117 });

            var dataSet = TangFactory.GetTang()
                .NewInjector(FileSystemInputPartitionConfiguration<IEnumerable<byte>>.ConfigurationModule
                    .Set(FileSystemInputPartitionConfiguration<IEnumerable<byte>>.FilePathForPartitions,
                        sourceFilePath1 + ";" + sourceFilePath2)
                    .Set(FileSystemInputPartitionConfiguration<IEnumerable<byte>>.FileSerializerConfig,
                        GetByteSerializerConfigString())
                    .Set(FileSystemInputPartitionConfiguration<IEnumerable<byte>>.CopyToLocal, "true")
                    .Build())
                .GetInstance<IPartitionedInputDataSet>();

            Assert.Equal(dataSet.Count, 1);

            foreach (var partitionDescriptor in dataSet)
            {
                var partition =
                    TangFactory.GetTang()
                        .NewInjector(partitionDescriptor.GetPartitionConfiguration())
                        .GetInstance<IInputPartition<IEnumerable<byte>>>();

                using (partition as IDisposable)
                {
                    Assert.NotNull(partition);
                    Assert.NotNull(partition.Id);
                    int count = 0;
                    var e = partition.GetPartitionHandle();
                    foreach (var v in e)
                    {
                        Logger.Log(Level.Info, string.Format(CultureInfo.CurrentCulture, "Data read {0}: ", v));
                        count++;
                    }
                    Assert.Equal(count, 7);
                }
            }
        }

        /// <remarks>
        /// This test creates IPartitionDataSet using the configuration build directly.
        /// It sets multiple files in each Partition
        /// </remarks>
        [Fact]
        public void TestWithoutConfigurationModuleWithTwoPartitions()
        {
            MakeLocalTestFile(sourceFilePath1, new byte[] { 111, 112, 113 });
            MakeLocalTestFile(sourceFilePath2, new byte[] { 114, 115, 116, 117 });

            var partitionConfig = TangFactory.GetTang().NewConfigurationBuilder()
                .BindImplementation(GenericType<IPartitionedInputDataSet>.Class,
                    GenericType<FileSystemPartitionInputDataSet<IEnumerable<byte>>>.Class)
                .BindStringNamedParam<FileDeSerializerConfigString>(GetByteSerializerConfigString())
                .BindSetEntry<FilePathsForInputPartitions, string>(GenericType<FilePathsForInputPartitions>.Class, sourceFilePath1)
                .BindSetEntry<FilePathsForInputPartitions, string>(GenericType<FilePathsForInputPartitions>.Class, sourceFilePath2)
                .Build();

            var dataSet = TangFactory.GetTang()
                .NewInjector(partitionConfig)
                .GetInstance<IPartitionedInputDataSet>();

            Assert.Equal(dataSet.Count, 2);

            foreach (var partitionDescriptor in dataSet)
            {
                var partition =
                    TangFactory.GetTang()
                        .NewInjector(partitionDescriptor.GetPartitionConfiguration())
                        .GetInstance<IInputPartition<IEnumerable<byte>>>();
                using (partition as IDisposable)
                {
                    Assert.NotNull(partition);
                    Assert.NotNull(partition.Id);
                }
            }
        }

        /// <remarks>
        /// This test is to use a ByteSerializer.
        /// </remarks>
        [Fact]
        public void TestWithByteDeserializer()
        {
            MakeLocalTestFile(sourceFilePath1, new byte[] { 111, 112, 113 });
            MakeLocalTestFile(sourceFilePath2, new byte[] { 114, 115, 116, 117 });

            var c = TangFactory.GetTang().NewConfigurationBuilder()
                .BindImplementation(GenericType<IPartitionedInputDataSet>.Class, GenericType<FileSystemPartitionInputDataSet<IEnumerable<byte>>>.Class)
                .BindStringNamedParam<FileDeSerializerConfigString>(GetByteSerializerConfigString())
                .BindSetEntry<FilePathsForInputPartitions, string>(GenericType<FilePathsForInputPartitions>.Class, sourceFilePath1)
                .BindSetEntry<FilePathsForInputPartitions, string>(GenericType<FilePathsForInputPartitions>.Class, sourceFilePath2)
                .Build();

            var dataSet = TangFactory.GetTang()
                .NewInjector(c)
                .GetInstance<IPartitionedInputDataSet>();
            
            int count = 0;
            foreach (var partitionDescriptor in dataSet)
            {
                var partition =
                    TangFactory.GetTang()
                        .NewInjector(partitionDescriptor.GetPartitionConfiguration())
                        .GetInstance<IInputPartition<IEnumerable<byte>>>();
                using (partition as IDisposable)
                {
                    var e = partition.GetPartitionHandle();
                    foreach (var v in e)
                    {
                        Logger.Log(Level.Info, string.Format(CultureInfo.CurrentCulture, "Data read {0}: ", v));
                        count++;
                    }
                }
            }
            Assert.Equal(count, 7);
        }

        /// <remarks>
        /// This test is to use a RowSerializer. Row is a class at client side. 
        /// </remarks>
        [Fact]
        public void TestWithRowDeserializer()
        {
            MakeLocalTestFile(sourceFilePath1, new byte[] { 111, 112, 113 });
            MakeLocalTestFile(sourceFilePath2, new byte[] { 114, 115 });

            var dataSet = TangFactory.GetTang()
                .NewInjector(FileSystemInputPartitionConfiguration<IEnumerable<Row>>.ConfigurationModule
                    .Set(FileSystemInputPartitionConfiguration<IEnumerable<Row>>.FilePathForPartitions, sourceFilePath1)
                    .Set(FileSystemInputPartitionConfiguration<IEnumerable<Row>>.FilePathForPartitions, sourceFilePath2)
                    .Set(FileSystemInputPartitionConfiguration<IEnumerable<Row>>.CopyToLocal, "true")
                    .Set(FileSystemInputPartitionConfiguration<IEnumerable<Row>>.FileSerializerConfig, GetRowSerializerConfigString())
                    .Build())
                .GetInstance<IPartitionedInputDataSet>();

            int count = 0;
            foreach (var partitionDescriptor in dataSet)
            {
                var partition =
                    TangFactory.GetTang()
                        .NewInjector(partitionDescriptor.GetPartitionConfiguration())
                        .GetInstance<IInputPartition<IEnumerable<Row>>>();
                using (partition as IDisposable)
                {
                    IEnumerable<Row> e = partition.GetPartitionHandle();

                    foreach (var row in e)
                    {
                        Logger.Log(Level.Info, string.Format(CultureInfo.CurrentCulture, "Data read {0}: ", row.GetValue()));
                        count++;
                    }
                }
            }
            Assert.Equal(count, 5);
        }

        /// <remarks>
        /// This test is to test injected IPartition with TempFileFolerParameter
        /// </remarks>
        [Fact]
        public void TestTempFileFolderWithRowDeserializer()
        {
            MakeLocalTestFile(sourceFilePath1, new byte[] { 111, 112, 113 });
            MakeLocalTestFile(sourceFilePath2, new byte[] { 114, 115 });

            var c1 = FileSystemInputPartitionConfiguration<IEnumerable<Row>>.ConfigurationModule
                .Set(FileSystemInputPartitionConfiguration<IEnumerable<Row>>.FilePathForPartitions, sourceFilePath1)
                .Set(FileSystemInputPartitionConfiguration<IEnumerable<Row>>.FilePathForPartitions, sourceFilePath2)
                .Set(FileSystemInputPartitionConfiguration<IEnumerable<Row>>.FileSerializerConfig,
                    GetRowSerializerConfigString())
                .Build();

            var c2 = TempFileConfigurationModule.ConfigurationModule
                .Set(TempFileConfigurationModule.TempFileFolerParameter, @".\test2\abc\")
                .Build();

            var dataSet = TangFactory.GetTang()
                .NewInjector(c1)
                .GetInstance<IPartitionedInputDataSet>();

            int count = 0;
            foreach (var partitionDescriptor in dataSet)
            {
                var partition =
                    TangFactory.GetTang()
                        .NewInjector(partitionDescriptor.GetPartitionConfiguration(), c2)
                        .GetInstance<IInputPartition<IEnumerable<Row>>>();
                using (partition as IDisposable)
                {
                    IEnumerable<Row> e = partition.GetPartitionHandle();

                    foreach (var row in e)
                    {
                        Logger.Log(Level.Info, string.Format(CultureInfo.CurrentCulture, "Data read {0}: ", row.GetValue()));
                        count++;
                    }
                }
            }
            Assert.Equal(count, 5);
        }

        private void MakeLocalTestFile(string filePath, byte[] bytes)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (var s = File.Create(filePath))
            {
                foreach (var b in bytes)
                {
                    s.WriteByte(b);
                }
            }
        }

        private string GetByteSerializerConfigString()
        {
            var serializerConf = TangFactory.GetTang().NewConfigurationBuilder()
                .BindImplementation<IFileDeSerializer<IEnumerable<byte>>, ByteSerializer>(
                    GenericType<IFileDeSerializer<IEnumerable<byte>>>.Class,
                    GenericType<ByteSerializer>.Class)
                .Build();
            return (new AvroConfigurationSerializer()).ToString(serializerConf);
        }

        private string GetRowSerializerConfigString()
        {
            var serializerConf = TangFactory.GetTang().NewConfigurationBuilder()
                .BindImplementation<IFileDeSerializer<IEnumerable<Row>>, RowSerializer>(
                    GenericType<IFileDeSerializer<IEnumerable<Row>>>.Class,
                    GenericType<RowSerializer>.Class)
                .Build();
            return (new AvroConfigurationSerializer()).ToString(serializerConf);
        }
    }

    public class ByteSerializer : IFileDeSerializer<IEnumerable<byte>>
    {
        [Inject]
        public ByteSerializer()
        { 
        }

        /// <summary>
        /// Enumerate all the files in the file foder and return each byte read
        /// </summary>
        /// <param name="fileFolder"></param>
        /// <returns></returns>
        [Obsolete("Remove after 0.14")]
        public IEnumerable<byte> Deserialize(string fileFolder)
        {
            var files = new HashSet<string>();
            foreach (var f in Directory.GetFiles(fileFolder))
            {
                files.Add(f);
            }
            return Deserialize(files);
        }

        /// <summary>
        /// Enumerate all the files in the set and return each byte read
        /// </summary>
        /// <param name="filePaths"></param>
        /// <returns></returns>
        public IEnumerable<byte> Deserialize(ISet<string> filePaths)
        {
            foreach (var f in filePaths)
            {
                using (FileStream stream = File.Open(f, FileMode.Open))
                {
                    BinaryReader reader = new BinaryReader(stream);
                    while (reader.PeekChar() != -1)
                    {
                        yield return reader.ReadByte();
                    }
                }
            }
        }
    }

    public class Row
    {
        private byte _b;

        public Row(byte b)
        {
            _b = b;
        }

        public byte GetValue()
        {
            return _b;
        }
    }

    internal class RowSerializer : IFileDeSerializer<IEnumerable<Row>>
    {
        [Inject]
        private RowSerializer()
        { 
        }

        /// <summary>
        /// read all the files in the fileFolder cand return byte read one by one
        /// </summary>
        /// <param name="fileFolder"></param>
        /// <returns></returns>
        [Obsolete("Remove after 0.14")]
        public IEnumerable<Row> Deserialize(string fileFolder)
        {
            ISet<string> files = new HashSet<string>();
            foreach (var f in Directory.GetFiles(fileFolder))
            {
                files.Add(f);
            }
            return Deserialize(files);
        }

        /// <summary>
        /// read all the files in the set and return byte read one by one
        /// </summary>
        /// <param name="filePaths"></param>
        /// <returns></returns>
        public IEnumerable<Row> Deserialize(ISet<string> filePaths)
        {
            foreach (var f in filePaths)
            {
                using (FileStream stream = File.Open(f, FileMode.Open))
                {
                    BinaryReader reader = new BinaryReader(stream);
                    while (reader.PeekChar() != -1)
                    {
                        yield return new Row(reader.ReadByte());
                    }
                }
            }
        }
    }
}
