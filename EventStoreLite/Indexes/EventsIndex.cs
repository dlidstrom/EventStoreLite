using System.Collections.Generic;
using System.Linq;
using Raven.Client.Indexes;

namespace EventStoreLite.Indexes
{
    internal class EventsIndex : AbstractIndexCreationTask<EventStream, EventsIndex.Result>
    {
        public EventsIndex()
        {
            Map = streams => from stream in streams
                             from @event in stream.History
                             select new
                                    {
                                        @event.ChangeSequence,
                                        Id = Enumerable.Repeat(new { stream.Id }, 1),
                                    };

            Reduce = sequences => from sequence in sequences
                                  group sequence by sequence.ChangeSequence into g
                                  select new
                                         {
                                             ChangeSequence = g.Key,
                                             Id = g.SelectMany(x => x.Id).Distinct()
                                         };
        }

        public class StreamId
        {
            public string Id { get; set; }
        }

        public class Result
        {
            public int ChangeSequence { get; set; }

            public IEnumerable<StreamId> Id { get; set; }
        }
    }
}