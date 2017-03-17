using System.Runtime.Serialization;

namespace Chapter6.Contracts
{
    [DataContract]
    public class WebLmWordBreakResponse
    {
        [DataMember]
        public WebLmCandidates[] candidates { get; set; }
    }

    [DataContract]
    public class WebLmCandidates
    {
        [DataMember]
        public string words { get; set; }

        [DataMember]
        public double probability { get; set; }
    }

    [DataContract]
    public class WebLmCondProbResponse
    {
        [DataMember]
        public WebLmCondProbResult[] results { get; set; }
    }

    [DataContract]
    public class WebLmCondProbResult
    {
        [DataMember]
        public string words { get; set; }

        [DataMember]
        public string word { get; set; }

        [DataMember]
        public double probability { get; set; }
    }

    [DataContract]
    public class WebLmJointProbResponse
    {
        [DataMember]
        public WebLmJointProbResults[] results { get; set; }
    }

    [DataContract]
    public class WebLmJointProbResults
    {
        [DataMember]
        public string words { get; set; }

        [DataMember]
        public double probability { get; set; }
    }

    [DataContract]
    public class WebLmNextWordResults
    {
        [DataMember]
        public WebLmNextWordCandidates[] candidates { get; set; }
    }

    [DataContract]
    public class WebLmNextWordCandidates
    {
        [DataMember]
        public string word { get; set; }

        [DataMember]
        public double probability { get; set; }
    }
}
