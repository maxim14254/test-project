using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ConsoleApp1
{
    [DataContract]
     class Object
    {
        [DataMember]
        public  int resultCount { get; set; }
        [DataMember]
        public List<Results> results { get; set; }
    }
}
