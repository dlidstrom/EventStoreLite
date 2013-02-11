using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace EventStoreLite.Indexes
{
    internal class WriteModelIndex : AbstractIndexCreationTask
    {
        public override IndexDefinition CreateIndexDefinition()
        {
            return new IndexDefinition
                   {
                       Map = @"from doc in docs where doc[""@metadata""][""AggregateRoot""] != null select new { doc.Id }"
                   };
        }
    }
}