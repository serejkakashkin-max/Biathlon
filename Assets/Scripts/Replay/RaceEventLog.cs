using System.Collections.Generic;
using Biathlon.Domain;
namespace Biathlon.Replay
{
    public sealed class RaceEventLog : IRaceEventSink
    {
        private readonly List<RaceEvent> events;
        public int Count => events.Count;
        public RaceEvent this[int index] => events[index];
        public RaceEventLog(int capacity = 0) { events = new List<RaceEvent>(capacity); }
        public void Record(RaceEvent raceEvent) => events.Add(raceEvent);
    }
}
