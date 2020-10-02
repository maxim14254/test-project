
using System.Runtime.Serialization;

namespace ConsoleApp1
{
    [DataContract]
    class Results
    {
        [DataMember]
        public string artistName { get; set; }

        [DataMember]
        public int artistId { get; set; }
        [DataMember]
        public string collectionName { get; set; }
        [DataMember]
        public string[] collectionNames { get; set; }

    }
}
