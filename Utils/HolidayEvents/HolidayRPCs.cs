using System;
using System.Linq;

namespace RainMeadow
{
    public static class HolidayRPCs
    {
        [RPCMethod]
        public static void MeadowCoinGiveth(int coinGift)
        {
            HolidayEvents.GainedMeadowCoin(coinGift);
        }
    }
}
