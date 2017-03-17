using System;
using System.Runtime.Serialization;

namespace Chapter6.Contracts
{
    [DataContract]
    public class Analyzer
    {
        [DataMember]
        public Guid id { get; set; }

        [DataMember]
        public string[] languages { get; set; }

        [DataMember]
        public string kind { get; set; }

        [DataMember]
        public string specification { get; set; }

        [DataMember]
        public string implementation { get; set; }
    }

    [DataContract]
    public class AnalyzerResults
    {
        [DataMember]
        public Guid analyzerId { get; set; }

        [DataMember]
        public object result { get; set; }
    }
}
