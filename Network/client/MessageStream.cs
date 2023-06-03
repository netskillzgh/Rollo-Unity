namespace Rollo.Client
{
    public readonly struct MessageStream
    {
        public readonly EventType EventType;
        public readonly Packet Data;
        public MessageStream(EventType eventType, Packet data)
        {
            EventType = eventType;
            Data = data;
        }
    }

    public enum EventType
    {
        Connected,
        Data,
        Disconnected
    }
}