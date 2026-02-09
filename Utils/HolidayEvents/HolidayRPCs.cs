using System;
using System.Linq;

namespace RainMeadow
{
    public static class HolidayRPCs
    {
        [RPCMethod]
        public static void MeadowCoinGiveth(bool holiday, int coinGift)
        {
           HolidayEvents.GainedMeadowCoin(holiday, coinGift);
        }

    }

}