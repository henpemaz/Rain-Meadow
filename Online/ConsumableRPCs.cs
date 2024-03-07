namespace RainMeadow
{
    public static class ConsumableRPCs
    {
        [RPCMethod]
        public static void SetOxygenLevel(OnlinePhysicalObject onlineBubbleGrass, float oxygenLeft)
        {
            if (onlineBubbleGrass is OnlineBubbleGrass obg) 
            {
                if (obg.AbstractBubbleGrass.oxygenLeft > oxygenLeft) return;
                obg.AbstractBubbleGrass.oxygenLeft = oxygenLeft;
            }
        }
        [RPCMethod]
        public static void swellWaterNut(OnlineConsumable onlineWaterNut) {
            (onlineWaterNut.apo.realizedObject as WaterNut).Swell();
        }
    }
}
