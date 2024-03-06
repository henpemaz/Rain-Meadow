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
    }
}
