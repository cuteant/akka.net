using System;
using System.Collections.Generic;
using System.Text;
using Akka;

namespace SimpleExample
{
    public sealed class ReadTemperature
    {
        public ReadTemperature(long requestId)
        {
            RequestId = requestId;
        }

        public long RequestId { get; }
    }

    public sealed class RespondTemperature
    {
        public RespondTemperature(long requestId, double? value)
        {
            RequestId = requestId;
            Value = value;
        }

        public long RequestId { get; }
        public double? Value { get; }
    }
}
