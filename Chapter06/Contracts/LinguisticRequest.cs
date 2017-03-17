using System;
using System.Runtime.Serialization;

namespace Chapter6.Contracts
{
    [DataContract]
    public class LinguisticRequest
    {
        [DataMember]
        public string language { get; set; }

        [DataMember]
        public Guid[] analyzerIds { get; set; }

        [DataMember]
        public string text { get; set; }
    }
}
