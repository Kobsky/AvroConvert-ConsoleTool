#region license
/**Copyright (c) 2021 Adrian Struga�a
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
#endregion

using System;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.AvroObjectServices.Schemas
{

    internal sealed class TimestampMicrosecondsSchema : LogicalTypeSchema
    {
        public TimestampMicrosecondsSchema() : this(typeof(DateTime))
        {
        }
        public TimestampMicrosecondsSchema(Type runtimeType) : base(runtimeType)
        {
            BaseTypeSchema = new LongSchema();
        }

        internal override AvroType Type => AvroType.Logical;
        internal override TypeSchema BaseTypeSchema { get; set; }
        internal override string LogicalTypeName => LogicalTypeEnum.TimestampMicroseconds;

        internal override object ConvertToLogicalValue(object baseValue, LogicalTypeSchema schema, Type readType)
        {
            var noMicroseconds = (long)baseValue;
            var result = DateTimeExtensions.UnixEpochDateTime.AddMilliseconds(noMicroseconds / 1000);

            if (readType == typeof(DateTimeOffset) || readType == typeof(DateTimeOffset?))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(noMicroseconds / 1000);
            }
            else
            {
                return result;
            }
        }
    }
}
