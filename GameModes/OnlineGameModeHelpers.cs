using System.Collections.Generic;

namespace RainMeadow
{
    public static class OnlineGameModeHelpers
    {
        public static HashSet<PlacedObject.Type> cosmeticItems = new()
        {
            PlacedObject.Type.None,
            PlacedObject.Type.LightSource,
            //PlacedObject.Type.TempleGuard,
            PlacedObject.Type.LightFixture,
            PlacedObject.Type.CoralStem,
            PlacedObject.Type.CoralStemWithNeurons,
            PlacedObject.Type.CoralNeuron,
            PlacedObject.Type.CoralCircuit,
            PlacedObject.Type.WallMycelia,
            PlacedObject.Type.ProjectedStars,
            PlacedObject.Type.ZapCoil,
            PlacedObject.Type.SuperStructureFuses,
            PlacedObject.Type.GravityDisruptor,
            PlacedObject.Type.SpotLight,
            PlacedObject.Type.DeepProcessing,
            PlacedObject.Type.Corruption,
            PlacedObject.Type.CorruptionTube,
            PlacedObject.Type.CorruptionDarkness,
            //PlacedObject.Type.StuckDaddy,
            PlacedObject.Type.SSLightRod,
            //PlacedObject.Type.CentipedeAttractor,
            PlacedObject.Type.DandelionPatch,
            PlacedObject.Type.GhostSpot,
            PlacedObject.Type.CosmeticSlimeMold,
            PlacedObject.Type.CosmeticSlimeMold2,
            PlacedObject.Type.VoidSpawnEgg,
            //PlacedObject.Type.SuperJumpInstruction,       //Charge pounce tutorial
            PlacedObject.Type.ProjectedImagePosition,
            PlacedObject.Type.ExitSymbolShelter,
            PlacedObject.Type.ExitSymbolHidden,
            PlacedObject.Type.NoSpearStickZone,
            PlacedObject.Type.LanternOnStick,
            PlacedObject.Type.ScavengerOutpost,
            PlacedObject.Type.TradeOutpost,
            //PlacedObject.Type.ScavengerTreasury,
            //PlacedObject.Type.ScavTradeInstruction,
            PlacedObject.Type.CustomDecal,
            PlacedObject.Type.InsectGroup,
            PlacedObject.Type.PlayerPushback,
            PlacedObject.Type.GoldToken,
            PlacedObject.Type.BlueToken,
            PlacedObject.Type.DeadTokenStalk,
            PlacedObject.Type.BrokenShelterWaterLevel,
            PlacedObject.Type.Filter,
            PlacedObject.Type.ReliableIggyDirection,
            PlacedObject.Type.Rainbow,
            PlacedObject.Type.LightBeam,
            PlacedObject.Type.NoLeviathanStrandingZone,
            PlacedObject.Type.FairyParticleSettings,
            PlacedObject.Type.DayNightSettings,
            PlacedObject.Type.EnergySwirl,
            PlacedObject.Type.LightningMachine,
            PlacedObject.Type.SteamPipe,
            PlacedObject.Type.WallSteamer,
            PlacedObject.Type.Vine,
            PlacedObject.Type.SnowSource,
            PlacedObject.Type.DeathFallFocus,
            PlacedObject.Type.CellDistortion,
            PlacedObject.Type.LocalBlizzard,
            PlacedObject.Type.NeuronSpawner,
            PlacedObject.Type.ExitSymbolAncientShelter,
            PlacedObject.Type.BlinkingFlower
         };
        public static HashSet<PlacedObject.Type> PlayerGrablableItems = new()
        {
            //PlacedObject.Type.FlareBomb,          
            //PlacedObject.Type.PuffBall,          //EDIBLE NOT FOOD
            PlacedObject.Type.DangleFruit, //still causes a lot of crashes
            //PlacedObject.Type.DataPearl,          
            //PlacedObject.Type.UniqueDataPearl,    
            //PlacedObject.Type.SeedCob,            //FOOD
            //PlacedObject.Type.DeadSeedCob,        //Saint only food
            //PlacedObject.Type.WaterNut,           //FOOD
            //PlacedObject.Type.JellyFish,          //FOOD
            //PlacedObject.Type.KarmaFlower,        //TODO
            PlacedObject.Type.Mushroom,           //TODO
            //PlacedObject.Type.SlimeMold,          //FOOD
            //PlacedObject.Type.FlyLure,            //TODO
            PlacedObject.Type.FirecrackerPlant,
            //PlacedObject.Type.VultureGrub,        //PREREQ
            //PlacedObject.Type.DeadVultureGrub,    //FOOD
            //PlacedObject.Type.MultiplayerItem,
            //PlacedObject.Type.SporePlant,         //bee granade
            //PlacedObject.Type.ReliableSpear,
            //PlacedObject.Type.NeedleEgg,          //Noodle egg DANGER SYNC
            //PlacedObject.Type.BubbleGrass,
            //PlacedObject.Type.Hazer,              //PREREQ
            //PlacedObject.Type.DeadHazer,          //FOOD
            //PlacedObject.Type.VultureMask,
            //PlacedObject.Type.HangingPearls,      //TODO?
            //PlacedObject.Type.Lantern,
        };
    }
}