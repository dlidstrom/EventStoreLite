using System.Collections.Generic;
using EventStoreLite;

namespace SampleDomain.ViewModels
{
    public class NamesViewModel : IReadModel
    {
        public const string DatabaseId = "CustomerNames";

        public NamesViewModel()
        {
            Id = DatabaseId;
            Names = new List<string>();
        }

        public List<string> Names { get; set; }

        public string Id { get; set; }
    }
}