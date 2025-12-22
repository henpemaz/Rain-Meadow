
using UnityEngine;
namespace RainMeadow {
public partial class RainMeadowOptions 
{
    public  Configurable<int> MaxCreatureCount;
    public  Configurable<int> PointsForSpear;
    public  Configurable<int> PointsForExplSpear;
    public  Configurable<int> PointsForBomb;
    public  Configurable<int> PointsForElectricSpear;
    public  Configurable<int> PointsForBoomerang;

    public  Configurable<int> PointsForRespawn;
    public  Configurable<int> PointsForDenOpen;
    public  Configurable<int> CreatureCleanup;
    public  Configurable<int> PointsForRock;

    public  Configurable<KeyCode> StoreItem1;
    public  Configurable<KeyCode> StoreItem2;
    public  Configurable<KeyCode> StoreItem3;
    public  Configurable<KeyCode> StoreItem4;
    public  Configurable<KeyCode> StoreItem5;
    public  Configurable<KeyCode> StoreItem6;
    public  Configurable<KeyCode> StoreItem7;
    public  Configurable<KeyCode> StoreItem8;

    public  Configurable<KeyCode> OpenStore;
    public void InitializeDrownOptions(OptionInterface.ConfigHolder config)
    {
        MaxCreatureCount = config.Bind("DrownMaxCreatures", 10);
        PointsForSpear = config.Bind("DrownPointsForSpear", 1);
        PointsForExplSpear = config.Bind("DrownPointsForExplSpear", 10);
        PointsForBomb = config.Bind("DrownPointsForBomb", 10);
        PointsForElectricSpear = config.Bind("PointsForElectricSpear", 12);
        PointsForBoomerang = config.Bind("PointsForBoomerang", 15);

        PointsForRespawn = config.Bind("DrownPointsForRespawn", 25);
        PointsForDenOpen = config.Bind("DrownPointsForDenOpen", 100);
        CreatureCleanup = config.Bind("DrownCreatureCleanup", 3);
        PointsForRock = config.Bind("PointsForRock", 0);

        StoreItem1 = config.Bind("DrownStoreItem1", KeyCode.Alpha1);
        StoreItem2 = config.Bind("DrownStoreItem2", KeyCode.Alpha2);
        StoreItem3 = config.Bind("DrownStoreItem3", KeyCode.Alpha3);
        StoreItem4 = config.Bind("DrownStoreItem4", KeyCode.Alpha4);
        StoreItem5 = config.Bind("DrownStoreItem5", KeyCode.Alpha5);
        StoreItem6 = config.Bind("DrownStoreItem6", KeyCode.Alpha6);
        StoreItem7 = config.Bind("DrownStoreItem7", KeyCode.Alpha7);
        StoreItem8 = config.Bind("DrownStoreItem8", KeyCode.Alpha8);

        OpenStore = config.Bind("DrownStoreAccess", KeyCode.Tab);
    }
  }
}