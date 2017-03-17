using System;
using System.Runtime.Serialization;

namespace Chapter7.Contracts
{
    [DataContract]
    public class RecommandationModels
    {
        [DataMember]
        public RecommandationModel[] models { get; set; }
    }

    [DataContract]
    public class RecommandationModel
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string description { get; set; }
        [DataMember]
        public DateTime createdDateTime { get; set; }
        [DataMember]
        public int activeBuildId { get; set; }
        [DataMember]
        public string catalogDisplayName { get; set; }
    }

}