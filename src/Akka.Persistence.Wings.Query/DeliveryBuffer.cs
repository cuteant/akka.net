using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Akka.Persistence.Wings.Query
{
    internal class DeliveryBuffer<T>
    {
        private ImmutableArray<T> _buffer = ImmutableArray<T>.Empty;
        public ImmutableArray<T> Buffer => _buffer;
        public bool IsEmpty => _buffer.IsEmpty;
        public int Length => _buffer.Length;

        private readonly Action<T> _onNext;

        public DeliveryBuffer(Action<T> onNext)
        {
            _onNext = onNext;
        }

        public void Add(T element)
        {
            _buffer = _buffer.Add(element);
        }
        public void AddRange(IEnumerable<T> elements)
        {
            _buffer = _buffer.AddRange(elements);
        }

        public void DeliverBuffer(long demand)
        {
            if (!_buffer.IsEmpty && demand > 0)
            {
                var totalDemand = Math.Min((int)demand, _buffer.Length);
                if (1 == _buffer.Length)
                {
                    // optimize for this common case
                    _onNext(_buffer[0]);
                    _buffer = ImmutableArray<T>.Empty;
                }
                else if (demand <= int.MaxValue)
                {
                    for (var i = 0; i < totalDemand; i++)
                        _onNext(_buffer[i]);

                    _buffer = _buffer.RemoveRange(0, totalDemand);
                }
                else
                {
                    foreach (var element in _buffer)
                        _onNext(element);

                    _buffer = ImmutableArray<T>.Empty;
                }
            }
        }

    }
}
