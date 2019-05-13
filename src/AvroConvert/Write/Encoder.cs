﻿namespace EhwarSoft.AvroConvert.Write
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Constants;
    using Exceptions;
    using Generic;
    using Helpers;
    using Helpers.Codec;
    using Resolvers;
    using Schema;
    using Array = System.Array;

    public class Encoder : IDisposable
    {
        private readonly Schema _schema;
        private readonly Codec _codec;
        private readonly Stream _stream;
        private MemoryStream _blockStream;
        private readonly IWriter _encoder;
        private IWriter _blockEncoder;
        private readonly WriteItem _writer;
        private readonly Resolvers.Array _array;
        private readonly Map _map;
        private byte[] _syncData;
        private bool _isOpen;
        private bool _headerWritten;
        private int _blockCount;
        private int _syncInterval;
        private readonly Metadata _metadata;


        public delegate void WriteItem(object value, IWriter encoder);

        private readonly Dictionary<RecordSchema, WriteItem> _recordWriters = new Dictionary<RecordSchema, WriteItem>();


        public Encoder(Schema schema, Stream outStream)
        {
            _codec = Codec.CreateCodec(Codec.Type.Null);
            _stream = outStream;
            _metadata = new Metadata();
            _schema = schema;
            _syncInterval = DataFileConstants.DefaultSyncInterval;

            _blockCount = 0;
            _encoder = new Writer(_stream);
            _blockStream = new MemoryStream();
            _blockEncoder = new Writer(_blockStream);

            _array = new Resolvers.Array();
            _map = new Map();

            _writer = ResolveWriter(schema);

            _isOpen = true;
        }

        public void Append(object datum)
        {
            AssertOpen();
            EnsureHeader();

            long usedBuffer = _blockStream.Position;

            try
            {
                Write(datum, _blockEncoder);
            }
            catch (Exception e)
            {
                _blockStream.Position = usedBuffer;
                throw new AvroRuntimeException("Error appending datum to writer", e);
            }
            _blockCount++;
            WriteIfBlockFull();
        }

        private void EnsureHeader()
        {
            if (!_headerWritten)
            {
                WriteHeader();
                _headerWritten = true;
            }
        }

        public void Flush()
        {
            EnsureHeader();
            Sync();
        }

        public long Sync()
        {
            AssertOpen();
            WriteBlock();
            return _stream.Position;
        }

        private void WriteHeader()
        {
            _encoder.WriteFixed(DataFileConstants.AvroHeader);
            WriteMetaData();
            WriteSyncData();
        }

        private void AssertOpen()
        {
            if (!_isOpen) throw new AvroRuntimeException("Cannot complete operation: avro file/stream not open");
        }

        private void WriteMetaData()
        {
            // Add sync, code & schema to metadata
            GenerateSyncData();
            _metadata.Add(DataFileConstants.CodecMetadataKey, _codec.GetName());
            _metadata.Add(DataFileConstants.SchemaMetadataKey, _schema.ToString());

            // write metadata 
            int size = _metadata.GetSize();
            _encoder.WriteInt(size);

            foreach (KeyValuePair<string, byte[]> metaPair in _metadata.GetValue())
            {
                _encoder.WriteString(metaPair.Key);
                _encoder.WriteBytes(metaPair.Value);
            }
            _encoder.WriteMapEnd();
        }

        private void WriteIfBlockFull()
        {
            if (_blockStream.Position >= _syncInterval)
                WriteBlock();
        }

        private void WriteBlock()
        {
            if (_blockCount > 0)
            {
                byte[] dataToWrite = _blockStream.ToArray();

                // write count 
                _encoder.WriteLong(_blockCount);

                // write data 
                _encoder.WriteBytes(_codec.Compress(dataToWrite));

                // write sync marker 
                _encoder.WriteFixed(_syncData);

                // reset / re-init block
                _blockCount = 0;
                _blockStream = new MemoryStream();
                _blockEncoder = new Writer(_blockStream);
            }
        }

        private void WriteSyncData()
        {
            _encoder.WriteFixed(_syncData);
        }

        private void GenerateSyncData()
        {
            _syncData = new byte[16];

            Random random = new Random();
            random.NextBytes(_syncData);
        }


        public void Write(object datum, IWriter encoder)
        {
            _writer(datum, encoder);
        }

        private WriteItem ResolveWriter(Schema schema)
        {
            switch (schema.Tag)
            {
                case Schema.Type.Null:
                    return WriteNull;
                case Schema.Type.Boolean:
                    return (v, e) => Write<bool>(v, schema.Tag, e.WriteBoolean);
                case Schema.Type.Int:
                    return (v, e) => Write<int>(v, schema.Tag, e.WriteInt);
                case Schema.Type.Long:
                    return (v, e) => Write<long>(v, schema.Tag, e.WriteLong);
                case Schema.Type.Float:
                    return (v, e) => Write<float>(v, schema.Tag, e.WriteFloat);
                case Schema.Type.Double:
                    return (v, e) => Write<double>(v, schema.Tag, e.WriteDouble);
                case Schema.Type.String:
                    return (v, e) => WriteString(v, e.WriteString);
                case Schema.Type.Bytes:
                    return (v, e) => Write<byte[]>(v, schema.Tag, e.WriteBytes);
                case Schema.Type.Error:
                case Schema.Type.Record:
                    return ResolveRecord((RecordSchema)schema);
                case Schema.Type.Enumeration:
                    return ResolveEnum(schema as EnumSchema);
                case Schema.Type.Fixed:
                    return (v, e) => WriteFixed(schema as FixedSchema, v, e);
                case Schema.Type.Array:
                    return ResolveArray((ArraySchema)schema);
                case Schema.Type.Map:
                    return ResolveMap((MapSchema)schema);
                case Schema.Type.Union:
                    return ResolveUnion((UnionSchema)schema);
                default:
                    return (v, e) => throw new AvroTypeMismatchException($"Tried to write against [{schema}] schema, but found [{v.GetType()}] type");
            }
        }

        protected void WriteNull(object value, IWriter encoder)
        {
            if (value != null) throw new AvroTypeMismatchException("[Null] required to write against [Null] schema but found " + value.GetType());
        }

        /// <summary>
        /// A generic method to serialize primitive Avro types.
        /// </summary>
        /// <typeparam name="S">Type of the C# type to be serialized</typeparam>
        /// <param name="value">The value to be serialized</param>
        /// <param name="tag">The schema type tag</param>
        /// <param name="writer">The writer which should be used to write the given type.</param>
        protected void Write<S>(object value, Schema.Type tag, Writer<S> writer)
        {
            if (value == null)
            {
                value = default(S);
            }

            if (!(value is S)) throw new AvroTypeMismatchException($"[{ typeof(S)}] required to write against [{tag.ToString()}] schema but found " + value.GetType());

            writer((S)value);
        }

        protected void WriteString(object value, Writer<string> writer)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (value is Guid)
            {
                value = value.ToString();
            }

            writer((string)value);
        }


        /// <summary>
        /// Serialized a record using the given RecordSchema. It uses GetField method
        /// to extract the field value from the given object.
        /// </summary>
        /// <param name="schema">The RecordSchema to use for serialization</param>
        private WriteItem ResolveRecord(RecordSchema recordSchema)
        {
            WriteItem recordResolver;
            if (_recordWriters.TryGetValue(recordSchema, out recordResolver))
            {
                return recordResolver;
            }
            var writeSteps = new Record[recordSchema.Fields.Count];
            recordResolver = (v, e) => WriteRecordFields(v, writeSteps, e);

            _recordWriters.Add(recordSchema, recordResolver);

            int index = 0;
            foreach (Field field in recordSchema)
            {
                var record = new Record
                {
                    WriteField = ResolveWriter(field.Schema),
                    Field = field
                };
                writeSteps[index++] = record;
            }

            return recordResolver;
        }


        public Dictionary<string, object> SplitKeyValues(object item)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            if (item == null)
            {
                return result;
            }

            Type objType = item.GetType();
            PropertyInfo[] properties = objType.GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                if (typeof(IList).IsAssignableFrom(prop.PropertyType))
                {
                    // We have a List<T> or array
                    dynamic value = null;
                    try
                    {
                        value = prop.GetValue(item);
                    }
                    catch (Exception)
                    {
                        //no value
                    }
                    //TODO make soft get value as method
                    if (value != null)
                    {
                        result.Add(prop.Name, GetSplittedList((IList)value));
                    }
                    else
                    {
                        result.Add(prop.Name, null);
                    }

                }
                else if (prop.PropertyType == typeof(Guid))
                {
                    // We have a simple type
                    dynamic value = null;
                    try
                    {
                        value = prop.GetValue(item);
                    }
                    catch (Exception)
                    {
                        //no value
                    }
                    result.Add(prop.Name, value.ToString());
                }
                else if (prop.PropertyType.GetTypeInfo().IsValueType ||
                         prop.PropertyType == typeof(string))
                {
                    // We have a simple type
                    dynamic value = null;
                    try
                    {
                        value = prop.GetValue(item);
                    }
                    catch (Exception)
                    {
                        //no value
                    }
                    result.Add(prop.Name, value);
                }
                else
                {
                    dynamic value = null;
                    try
                    {
                        value = prop.GetValue(item);
                    }
                    catch (Exception)
                    {
                        //no value
                    }

                    if (value != null)
                    {
                        result.Add(prop.Name, SplitKeyValues(value));
                    }
                    else
                    {
                        result.Add(prop.Name, null);
                    }

                }
            }

            return result;
        }

        IList GetSplittedList(IList list)
        {
            if (list.Count == 0)
            {
                return list;
            }

            var typeToCheck = list.GetType().GetProperties()[2].PropertyType;

            if (typeToCheck.GetTypeInfo().IsValueType ||
                typeToCheck == typeof(string))
            {
                return list;
            }
            else
            {
                List<object> result = new List<object>();

                foreach (var item in list)
                {
                    result.Add(item != null ? SplitKeyValues(item) : null);
                }

                return result;
            }
        }

        public void WriteRecordFields(object recordObj, Record[] writers, IWriter encoder)
        {
            GenericRecord record = new GenericRecord((RecordSchema)_schema);

            if (recordObj is Dictionary<string, object> obj)
            {
                record.contents = obj;
            }

            else
            {
                record.contents = SplitKeyValues(recordObj);
            }

            foreach (var writer in writers)
            {
                writer.WriteField(record[writer.Field.Name], encoder);
            }
        }

        public void EnsureRecordObject(RecordSchema recordSchema, object value)
        {
            if (value == null || !(value is GenericRecord) || !((value as GenericRecord).Schema.Equals(recordSchema)))
            {
                throw new AvroTypeMismatchException("[GenericRecord] required to write against [Record] schema but found " + value.GetType());
            }
        }

        public void WriteField(object record, string fieldName, int fieldPos, WriteItem writer,
            IWriter encoder)
        {
            writer(((GenericRecord)record)[fieldName], encoder);
        }

        public WriteItem ResolveEnum(EnumSchema es)
        {
            return (value, e) =>
            {
                if (value == null || !(value is GenericEnum) || !((value as GenericEnum).Schema.Equals(es)))
                    throw new AvroTypeMismatchException("[GenericEnum] required to write against [Enum] schema but found " + value.GetType());
                e.WriteEnum(es.Ordinal((value as GenericEnum).Value));
            };
        }

        public void WriteFixed(FixedSchema es, object value, IWriter encoder)
        {
            if (value == null || !(value is GenericFixed) || !(value as GenericFixed).Schema.Equals(es))
            {
                throw new AvroTypeMismatchException("[GenericFixed] required to write against [Fixed] schema but found " + value.GetType());
            }

            GenericFixed ba = (GenericFixed)value;
            encoder.WriteFixed(ba.Value);
        }

        /*
         * FIXME: This method of determining the Union branch has problems. If the data is IDictionary<string, object>
         * if there are two branches one with record schema and the other with map, it choose the first one. Similarly if
         * the data is byte[] and there are fixed and bytes schemas as branches, it choose the first one that matches.
         * Also it does not recognize the arrays of primitive types.
         */
        public bool UnionBranchMatches(Schema sc, object obj)
        {
            if (obj == null && sc.Tag != Schema.Type.Null) return false;
            switch (sc.Tag)
            {
                case Schema.Type.Null:
                    return obj == null;
                case Schema.Type.Boolean:
                    return obj is bool;
                case Schema.Type.Int:
                    return obj is int;
                case Schema.Type.Long:
                    return obj is long;
                case Schema.Type.Float:
                    return obj is float;
                case Schema.Type.Double:
                    return obj is double;
                case Schema.Type.Bytes:
                    return obj is byte[];
                case Schema.Type.String:
                    return obj is string;
                case Schema.Type.Error:
                case Schema.Type.Record:
                    //return obj is GenericRecord && (obj as GenericRecord)._schema.Equals(s);
                    return obj is GenericRecord &&
                           (obj as GenericRecord).Schema.SchemaName.Equals((sc as RecordSchema).SchemaName);
                case Schema.Type.Enumeration:
                    //return obj is GenericEnum && (obj as GenericEnum)._schema.Equals(s);
                    return obj is GenericEnum &&
                           (obj as GenericEnum).Schema.SchemaName.Equals((sc as EnumSchema).SchemaName);
                case Schema.Type.Array:
                    return obj is System.Array && !(obj is byte[]);
                case Schema.Type.Map:
                    return obj is IDictionary<string, object>;
                case Schema.Type.Union:
                    return false; // Union directly within another union not allowed!
                case Schema.Type.Fixed:
                    //return obj is GenericFixed && (obj as GenericFixed)._schema.Equals(s);
                    return obj is GenericFixed &&
                           (obj as GenericFixed).Schema.SchemaName.Equals((sc as FixedSchema).SchemaName);
                default:
                    throw new AvroException("Unknown schema type: " + sc.Tag);
            }
        }

        /// <summary>
        /// Serialized an array. The default implementation calls EnsureArrayObject() to ascertain that the
        /// given value is an array. It then calls GetArrayLength() and GetArrayElement()
        /// to access the members of the array and then serialize them.
        /// </summary>
        /// <param name="schema">The ArraySchema for serialization</param>
        /// <param name="value">The value being serialized</param>
        /// <param name="encoder">The encoder for serialization</param>
        protected WriteItem ResolveArray(ArraySchema schema)
        {
            var itemWriter = ResolveWriter(schema.ItemSchema);
            return (d, e) => WriteArray(itemWriter, d, e);
        }

        private void WriteArray(WriteItem itemWriter, object array, IWriter encoder)
        {
            array = _array.EnsureArrayObject(array);
            long l = _array.GetArrayLength(array);
            encoder.WriteArrayStart();
            encoder.SetItemCount(l);
            _array.WriteArrayValues(array, itemWriter, encoder);
            encoder.WriteArrayEnd();
        }

        private WriteItem ResolveMap(MapSchema mapSchema)
        {
            var itemWriter = ResolveWriter(mapSchema.ValueSchema);
            return (v, e) => WriteMap(itemWriter, v, e);
        }

        /// <summary>
        /// Serialized a map. The default implementation first ensure that the value is indeed a map and then uses
        /// GetMapSize() and GetMapElements() to access the contents of the map.
        /// </summary>
        /// <param name="schema">The MapSchema for serialization</param>
        /// <param name="value">The value to be serialized</param>
        /// <param name="encoder">The encoder for serialization</param>
        protected void WriteMap(WriteItem itemWriter, object value, IWriter encoder)
        {
            _map.EnsureMapObject(value);
            encoder.WriteMapStart();
            encoder.SetItemCount(_map.GetMapSize(value));
            _map.WriteMapValues(value, itemWriter, encoder);
            encoder.WriteMapEnd();
        }


        private WriteItem ResolveUnion(UnionSchema unionSchema)
        {
            var branchSchemas = unionSchema.Schemas.ToArray();
            var branchWriters = new WriteItem[branchSchemas.Length];
            int branchIndex = 0;
            foreach (var branch in branchSchemas)
            {
                branchWriters[branchIndex++] = ResolveWriter(branch);
            }


            return (v, e) => WriteUnion(unionSchema, branchSchemas, branchWriters, v, e);
        }

        /// <summary>
        /// Resolves the given value against the given UnionSchema and serializes the object against
        /// the resolved schema member.
        /// </summary>
        /// <param name="us">The UnionSchema to resolve against</param>
        /// <param name="value">The value to be serialized</param>
        /// <param name="encoder">The encoder for serialization</param>
        private void WriteUnion(UnionSchema unionSchema, Schema[] branchSchemas, WriteItem[] branchWriters, object value, IWriter encoder)
        {
            int index = ResolveUnion(unionSchema, branchSchemas, value);
            encoder.WriteUnionIndex(index);
            branchWriters[index](value, encoder);
        }

        /// <summary>
        /// Finds the branch within the given UnionSchema that matches the given object. The default implementation
        /// calls Matches() method in the order of branches within the UnionSchema. If nothing matches, throws
        /// an exception.
        /// </summary>
        /// <param name="us">The UnionSchema to resolve against</param>
        /// <param name="obj">The object that should be used in matching</param>
        /// <returns></returns>
        protected int ResolveUnion(UnionSchema us, Schema[] branchSchemas, object obj)
        {
            for (int i = 0; i < branchSchemas.Length; i++)
            {
                if (UnionBranchMatches(branchSchemas[i], obj)) return i;
            }
            throw new AvroException("Cannot find a match for " + obj.GetType() + " in " + us);
        }


        public void Dispose()
        {
            EnsureHeader();
            Flush();
            _stream.Flush();
            _stream.Dispose();
            _isOpen = false;
        }
    }
}
