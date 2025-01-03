
namespace RainMeadow {
    public static class EventMath {
            public static bool IsNewer(ulong eventId, ulong lastIncomingEvent)
            {
                ulong delta = eventId - lastIncomingEvent;
                return delta != 0 && delta < ulong.MaxValue / 2;
            }

            public static bool IsNewerOrEqual(ulong eventId, ulong lastIncomingEvent)
            {
                ulong delta = eventId - lastIncomingEvent;
                return delta < ulong.MaxValue / 2;
            }

            public static bool IsNewer(uint eventId, uint lastIncomingEvent)
            {
                uint delta = eventId - lastIncomingEvent;
                return delta != 0 && delta < uint.MaxValue / 2;
            }

            public static bool IsNewerOrEqual(uint eventId, uint lastIncomingEvent)
            {
                uint delta = eventId - lastIncomingEvent;
                return delta < uint.MaxValue / 2;
            }

            public static bool IsNewer(ushort eventId, ushort lastIncomingEvent)
            {
                ushort delta = (ushort)(eventId - lastIncomingEvent);
                return delta != 0 && delta < ushort.MaxValue / 2;
            }

            public static bool IsNewerOrEqual(ushort eventId, ushort lastIncomingEvent)
            {
                ushort delta = (ushort)(eventId - lastIncomingEvent);
                return delta < ushort.MaxValue / 2;
            }
    }
}