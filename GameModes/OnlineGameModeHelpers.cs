using System.Collections.Generic;
using MoreSlugcats;

namespace RainMeadow
{
    public partial class OnlineGameMode
    {
        public HashSet<PlacedObject.Type> cosmeticItems = new()
        {
            PlacedObject.Type.None,
            PlacedObject.Type.LightSource,
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
            PlacedObject.Type.SSLightRod,
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
            //PlacedObject.Type.ScavTradeInstruction,
            PlacedObject.Type.CustomDecal,
            PlacedObject.Type.InsectGroup,
            PlacedObject.Type.PlayerPushback,
            MoreSlugcatsEnums.PlacedObjectType.MSArteryPush,
            PlacedObject.Type.GoldToken,
            PlacedObject.Type.BlueToken,
            MoreSlugcatsEnums.PlacedObjectType.GreenToken,
            MoreSlugcatsEnums.PlacedObjectType.WhiteToken,
            MoreSlugcatsEnums.PlacedObjectType.RedToken,
            MoreSlugcatsEnums.PlacedObjectType.DevToken,
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
            PlacedObject.Type.BlinkingFlower,
            MoreSlugcatsEnums.PlacedObjectType.OEsphere,
            MoreSlugcatsEnums.PlacedObjectType.KarmaShrine,
        };

        public HashSet<PlacedObject.Type> creatureRelatedItems = new()
        {
            MoreSlugcatsEnums.PlacedObjectType.BigJellyFish,
            MoreSlugcatsEnums.PlacedObjectType.RotFlyPaper,
            MoreSlugcatsEnums.PlacedObjectType.Stowaway,
            //PlacedObject.Type.TempleGuard,
            //MoreSlugcatsEnums.PlacedObjectType.HRGuard,
            //PlacedObject.Type.StuckDaddy,
            //PlacedObject.Type.CentipedeAttractor,
        };

        public HashSet<PlacedObject.Type> playerGrabbableItems = new()
        {
            PlacedObject.Type.FlareBomb,
            PlacedObject.Type.PuffBall,          //Weird behavior between thrower and everyone else when throwing 
            PlacedObject.Type.DangleFruit,
            PlacedObject.Type.DataPearl,
            PlacedObject.Type.UniqueDataPearl,
            PlacedObject.Type.SeedCob,
            PlacedObject.Type.DeadSeedCob,
            PlacedObject.Type.WaterNut,
            PlacedObject.Type.JellyFish,
            PlacedObject.Type.KarmaFlower,
            PlacedObject.Type.Mushroom,
            PlacedObject.Type.SlimeMold,
            PlacedObject.Type.FlyLure,
            PlacedObject.Type.FirecrackerPlant,
            PlacedObject.Type.VultureGrub,
            PlacedObject.Type.DeadVultureGrub,
            PlacedObject.Type.ScavengerTreasury,  //Spear, Explosive spear, Scav bomb, Pearl, & Laterns
            PlacedObject.Type.MultiplayerItem,    //ARENA_SPAWNS for Rock, Spear, ExplosiveSpear, Bomb, SporePlant data
            //PlacedObject.Type.SporePlant,         //abstractConsumable HARD. Need to sync bee's and attached Bee's still
            PlacedObject.Type.ReliableSpear,        //Spears have MSC elements
            PlacedObject.Type.NeedleEgg,
            PlacedObject.Type.BubbleGrass,
            PlacedObject.Type.Hazer,
            PlacedObject.Type.DeadHazer,
            PlacedObject.Type.VultureMask,          //MSC
            //PlacedObject.Type.HangingPearls,      //MSC
            PlacedObject.Type.Lantern,
            MoreSlugcatsEnums.PlacedObjectType.GooieDuck,
            MoreSlugcatsEnums.PlacedObjectType.LillyPuck,
            MoreSlugcatsEnums.PlacedObjectType.GlowWeed,
            MoreSlugcatsEnums.PlacedObjectType.MoonCloak,
            MoreSlugcatsEnums.PlacedObjectType.DandelionPeach,
        };
    }
}
