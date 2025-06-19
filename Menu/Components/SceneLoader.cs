using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMeadow.UI.Components
{
    public class SceneLoader : MenuObject
    {
        public List<MenuScene.SceneID> sceneIDsToLoad = [], loadedSceneIDs = [];
        public InteractiveMenuScene menuSceneLoader;
        public bool startLoadingScenes = false;
        public int sceneLoadDelay, desiredSceneLoadDelay;
        public SceneLoader(Menu.Menu menu, MenuObject? owner, int desiredSceneLoadDelay = 3, bool startLoadingScenes = true) : base(menu, owner)
        {
            this.desiredSceneLoadDelay = desiredSceneLoadDelay;
            this.startLoadingScenes = startLoadingScenes;
            (owner?.Container ?? menu.container).AddChild(myContainer = new());
            myContainer.isVisible = false;
            menuSceneLoader = new(menu, this, MenuScene.SceneID.Empty);
        }
        public void AddScenesToLoad(params MenuScene.SceneID[] sceneIDs) => sceneIDsToLoad.AddDistinctRange(sceneIDs.Where(x => !loadedSceneIDs.Contains(x)).Distinct());
        public void BumpUpSceneLoad(MenuScene.SceneID? sceneID)
        {
            if (sceneID == null) return;
            if (sceneIDsToLoad.Count > 0 && sceneIDsToLoad[0] == sceneID) return;
            if (loadedSceneIDs.Contains(sceneID)) return;
            sceneIDsToLoad.RemoveAll(id => id == sceneID);
            sceneIDsToLoad.Insert(0, sceneID);
        }
        public override void Update()
        {
            base.Update();
            if (!startLoadingScenes) return;
            if (sceneLoadDelay > 0) sceneLoadDelay--;
            if (sceneIDsToLoad.Count == 0 || sceneLoadDelay > 0) return;
            if (!loadedSceneIDs.Contains(sceneIDsToLoad[0]))
            {
                menuSceneLoader.RemoveSprites();
                menuSceneLoader = new(menu, this, sceneIDsToLoad[0]);
                loadedSceneIDs.Add(sceneIDsToLoad[0]);
            }
            sceneIDsToLoad.RemoveAt(0);
            sceneLoadDelay = desiredSceneLoadDelay;
        }
    }
}
