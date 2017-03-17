using System.Runtime.Serialization;

namespace Chapter6.Contracts
{
    [DataContract]
    public class WebLmConditionalProbRequest
    {
        [DataMember]
        public WebLmCondProbQueries[] queries { get; set; }
    }

    [DataContract]
    public class WebLmCondProbQueries
    {
        [DataMember]
        public string words { get; set; }

        [DataMember]
        public string word { get; set; }
    }

    [DataContract]
    public class WebLmJointProbRequest
    {
        [DataMember]
        public string[] queries { get; set; }
    }
}
