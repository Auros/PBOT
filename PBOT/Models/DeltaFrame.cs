using ProtoBuf;

namespace PBOT.Models
{
    [ProtoContract]
    internal class DeltaFrame
    {
        [ProtoMember(1)]
        public float Time { get; set; }

        [ProtoMember(2)]
        public float Current { get; set; }
    }
}