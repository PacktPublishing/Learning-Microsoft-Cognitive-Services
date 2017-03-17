using System.Runtime.Serialization;

namespace Chapter7.Contracts
{
    [DataContract]
    public class Recommendations
    {
        [DataMember]
        public Recommendeditem[] recommendedItems { get; set; }
    }

    [DataContract]
    public class Recommendeditem
    {
        [DataMember]
        public Item[] items { get; set; }
        [DataMember]
        public float rating { get; set; }
        [DataMember]
        public string[] reasoning { get; set; }
    }

    [DataContract]
    public class Item
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string name { get; set; }
    }

}