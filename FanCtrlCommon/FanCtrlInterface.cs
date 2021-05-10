using System.ServiceModel;
using System.Runtime.Serialization;

namespace FanCtrlCommon
{
    [ServiceContract]
    public interface IFanCtrlInterface
    {
        [OperationContract]
        FanCtrlData GetData();
        [OperationContract]
        bool Level2IsForced();
        [OperationContract]
        void SetLevel2IsForced(bool forced);
    }

    [DataContract]
    public struct FanCtrlData
    {
        [DataMember]
        public uint SystemTemperature { get; private set; }
        [DataMember]
        public sbyte FanLevel { get; private set; }
        public FanCtrlData(uint temp, sbyte fan)
        {
            SystemTemperature = temp;
            FanLevel = fan;
        }
    }
}
