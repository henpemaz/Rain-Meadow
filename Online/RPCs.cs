using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public static class RPCs
    {
        [RPCMethod]
        public static void DeltaReset(RPCEvent rpcEvent, OnlineResource onlineResource, OnlineEntity.EntityId entity)
        {
            RainMeadow.Debug($"from {rpcEvent.from} resource {onlineResource} entity {entity}");
            if (entity != null)
            {
                foreach (var feed in OnlineManager.feeds)
                {
                    if (feed.player == rpcEvent.from && feed.entity.id == entity && feed.resource == onlineResource)
                    {
                        feed.ResetDeltas();
                        return;
                    }
                }
            }
            else
            {
                foreach (var subscription in OnlineManager.subscriptions)
                {
                    if (subscription.player == rpcEvent.from && subscription.resource == onlineResource)
                    {
                        subscription.ResetDeltas();
                        return;
                    }
                }
            }
        }

        [RPCMethod]
        public static void HitSomething(OnlinePhysicalObject objectHitting, OnlinePhysicalObject objectHit, short chunkID, short appendageID, bool hitSomething, Vector2 collisionPoint, bool eu) {
            var realizedObject = objectHitting.apo.realizedObject;
            var realizedObjectHit = objectHit?.apo.realizedObject;



            var result = new SharedPhysics.CollisionResult(realizedObjectHit,null,null,hitSomething,collisionPoint);
            (realizedObject as Weapon).HitSomething(result, eu);
            //this.obj = obj;                       object hit
            //this.chunk = chunk;                   body part hit
            //this.hitSomething = hitSomething;     didhit
            //this.collisionPoint = collisionPoint; point in space hit
            //this.onAppendagePos = onAppendagePos; appendage hit
        }

        [RPCMethod]
        public static void AddFood(short add)
        {
            ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0].realizedCreature as Player).AddFood(add);
        }

        [RPCMethod]
        public static void AddQuarterFood()
        {
            ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0].realizedCreature as Player).AddQuarterFood();
        }

        [RPCMethod]
        public static void AddMushroomCounter()
        {
            ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0].realizedCreature as Player).mushroomCounter += 320;
        }
        [RPCMethod]
        public static void ReinforceKarma()
        {
            ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.session as StoryGameSession).saveState.deathPersistentSaveData.reinforcedKarma = true;
        }
        [RPCMethod]
        public static void PlayReinforceKarmaAnimation() 
        {
            (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame).cameras[0].hud.karmaMeter.reinforceAnimation = 0;
        }
        [RPCMethod]
        public static void InitGameOver()
        {
            var player = ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0]);
            (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.cameras[0].hud.InitGameOverMode(null, 0, player.pos.room, new UnityEngine.Vector2(0f, 0f));
        }
        [RPCMethod]
        public static void MovePlayersToDeathScreen() {
            foreach (OnlinePlayer player in OnlineManager.players)
            {
                player.InvokeRPC(RPCs.GoToDeathScreen);
            }
        }
        [RPCMethod]
        public static void GoToDeathScreen()
        {
            var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
            if (game.manager.upcomingProcess != null)
            {
                return;
            }
            if (game.IsStorySession && game.GetStorySession.RedIsOutOfCycles && !game.rainWorld.ExpeditionMode)
            {
                game.GoToRedsGameOver();
                return;
            }
            game.GetStorySession.saveState.SessionEnded(game, false, false);
            game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.DeathScreen);
        }

    }
}
