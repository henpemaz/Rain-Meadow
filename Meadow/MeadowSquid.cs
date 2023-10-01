using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using RWCustom;
using MonoMod.Cil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RainMeadow
{
    public partial class MeadowCustomization
    {

        public static void Enable()
        {
            On.Cicada.Update += Cicada_Update; // input, sync, player things
            On.Cicada.Act += Cicada_Act; // movement
            On.Cicada.Swim += Cicada_Swim; // prevent loss of control
            On.Cicada.GrabbedByPlayer += Cicada_GrabbedByPlayer; // prevent loss of control
            On.Cicada.CarryObject += Cicada_CarryObject; // more realistic grab pos, pointy stick
            On.Cicada.Collide += Cicada_Collide; // charging on creature, attack chunk
            On.Cicada.TerrainImpact += Cicada_TerrainImpact; // fall damage death, embed

            On.CicadaAI.Update += CicadaAI_Update; // dont let AI interfere on squiddy

            IL.Cicada.Act += Cicada_Act1; // cicada pather gets confused about entering shortcuts, let our code handle that instead
                                          // also fix zerog
        }

        private static ConditionalWeakTable<AbstractCreature, CicadaController> player = new();
        class CicadaController : CreatureController
        {
            public CicadaController(Cicada creature, int playerNumber) : base(creature, playerNumber)
            {
            }
        }

        public class CreatureController
        {
            public Creature creature;

            public CreatureController(Creature creature, int playerNumber)
            {
                this.creature = creature;
                this.playerNumber = playerNumber;

                input = new Player.InputPackage[10];
                rainWorld = creature.abstractCreature.world.game.rainWorld;
            }

            public int playerNumber = 0;

            public Player.InputPackage[] input;

            private RainWorld rainWorld;
            public int wantToJump;
            public int wantToPickUp;
            public int dontEatExternalFoodSourceCounter;
            public int eatExternalFoodSourceCounter;
            public int touchedNoInputCounter;
            public bool readyForWin;
            internal PhysicalObject pickUpCandidate;
            internal int dontGrabStuff;
            internal int eatCounter;
            private int flipDirection;

            public void checkInput()
            {
                for (int i = this.input.Length - 1; i > 0; i--)
                {
                    this.input[i] = this.input[i - 1];
                }
                
                if (creature.stun == 0 && !creature.dead)
                {
                    this.input[0] = RWInput.PlayerInput(playerNumber, creature.room.game.rainWorld);
                }
                else
                {
                    this.input[0] = new Player.InputPackage(rainWorld.options.controls[playerNumber].gamePad, rainWorld.options.controls[playerNumber].GetActivePreset(), 0, 0, false, false, false, false, false);
                }
                //this.mapInput = this.input[0];
                //if ((this.standStillOnMapButton && this.input[0].mp) || this.Sleeping)
                //{
                //    this.input[0].x = 0;
                //    this.input[0].y = 0;
                //    Player.InputPackage[] input = this.input;
                //    int num2 = 0;
                //    input[num2].analogueDir = input[num2].analogueDir * 0f;
                //    this.input[0].jmp = false;
                //    this.input[0].thrw = false;
                //    this.input[0].pckp = false;
                //    this.Blink(5);
                //}
            }
            internal virtual void Update(bool eu)
            {
                // Input
                this.checkInput();

                // a lot of things copypasted from from p.update
                if (this.wantToJump > 0) this.wantToJump--;
                if (this.wantToPickUp > 0) this.wantToPickUp--;

                // eat popcorn
                if (this.dontEatExternalFoodSourceCounter > 0)
                {
                    this.dontEatExternalFoodSourceCounter--;
                }
                if (creature.Consious && this.eatExternalFoodSourceCounter > 0)
                {
                    this.eatExternalFoodSourceCounter--;
                    if (this.eatExternalFoodSourceCounter < 1)
                    {
                        if (creature.grasps[0]?.grabbed is SeedCob) creature.grasps[0].Release();
                        this.dontEatExternalFoodSourceCounter = 45;
                        creature.room.PlaySound(SoundID.Slugcat_Bite_Fly, creature.mainBodyChunk);
                    }
                }

                // no input
                if (this.input[0].x == 0 && this.input[0].y == 0 && !this.input[0].jmp && !this.input[0].thrw && !this.input[0].pckp)
                {
                    this.touchedNoInputCounter++;
                }
                else
                {
                    this.touchedNoInputCounter = 0;
                }

                //// shelter activation
                //this.readyForWin = false;
                //if (this.room.abstractRoom.shelter && this.room.game.IsStorySession && !this.dead && !this.Sleeping && this.room.shelterDoor != null && !this.room.shelterDoor.Broken)
                //{
                //    if (!this.stillInStartShelter && this.FoodInRoom(this.room, false) >= ((!this.abstractCreature.world.game.GetStorySession.saveState.malnourished) ? this.slugcatStats.foodToHibernate : this.slugcatStats.maxFood))
                //    {
                //        this.readyForWin = true;
                //        this.forceSleepCounter = 0;
                //    }
                //    else if (this.room.world.rainCycle.timer > this.room.world.rainCycle.cycleLength)
                //    {
                //        this.readyForWin = true;
                //        this.forceSleepCounter = 0;
                //    }
                //    else if (this.input[0].y < 0 && !this.input[0].jmp && !this.input[0].thrw && !this.input[0].pckp && this.IsTileSolid(1, 0, -1) && !this.abstractCreature.world.game.GetStorySession.saveState.malnourished && this.FoodInRoom(this.room, false) > 0 && this.FoodInRoom(this.room, false) < this.slugcatStats.foodToHibernate && (this.input[0].x == 0 || ((!this.IsTileSolid(1, -1, -1) || !this.IsTileSolid(1, 1, -1)) && this.IsTileSolid(1, this.input[0].x, 0))))
                //    {
                //        this.forceSleepCounter++;
                //    }
                //    else
                //    {
                //        this.forceSleepCounter = 0;
                //    }
                //    if (Custom.ManhattanDistance(this.abstractCreature.pos.Tile, this.room.shortcuts[0].StartTile) > 6)
                //    {
                //        if (this.readyForWin && this.touchedNoInputCounter > 20)
                //        {
                //            this.room.shelterDoor.Close();
                //        }
                //        else if (this.forceSleepCounter > 260)
                //        {
                //            this.sleepCounter = -24;
                //            this.room.shelterDoor.Close();
                //        }
                //    }
                //}

                //// karma flower placement
                //if (this.room.game.IsStorySession)
                //{
                //    if (this.room.game.cameras[0].hud != null && !this.room.game.cameras[0].hud.textPrompt.gameOverMode)
                //    {
                //        this.SessionRecord.time++;
                //    }
                //    if (this.PlaceKarmaFlower && !this.dead && this.grabbedBy.Count == 0 && this.IsTileSolid(1, 0, -1) && !this.room.GetTile(this.bodyChunks[1].pos).DeepWater && !this.IsTileSolid(1, 0, 0) && !this.IsTileSolid(1, 0, 1) && !this.room.GetTile(this.bodyChunks[1].pos).wormGrass && (this.room == null || !this.room.readyForAI || !this.room.aimap.getAItile(this.room.GetTilePosition(this.bodyChunks[1].pos)).narrowSpace))
                //    {
                //        this.karmaFlowerGrowPos = new WorldCoordinate?(this.room.GetWorldCoordinate(this.bodyChunks[1].pos));
                //    }
                //}

                //// SHROOMIES
                //if (this.mushroomCounter > 0)
                //{
                //    if (!this.inShortcut)
                //    {
                //        this.mushroomCounter--;
                //    }
                //    this.mushroomEffect = Custom.LerpAndTick(this.mushroomEffect, 1f, 0.05f, 0.025f);
                //}
                //else
                //{
                //    this.mushroomEffect = Custom.LerpAndTick(this.mushroomEffect, 0f, 0.025f, 0.014285714f);
                //}
                //if (this.Adrenaline > 0f)
                //{
                //    if (this.adrenalineEffect == null)
                //    {
                //        this.adrenalineEffect = new AdrenalineEffect(this);
                //        this.room.AddObject(this.adrenalineEffect);
                //    }
                //    else if (this.adrenalineEffect.slatedForDeletetion)
                //    {
                //        this.adrenalineEffect = null;
                //    }
                //}

                //// death grasp
                //// this is from vanilla but could be reworked into a more flexible system.
                //if (this.dangerGrasp == null)
                //{
                //    this.dangerGraspTime = 0;
                //    foreach (var grasp in creature.grabbedBy)
                //    {
                //        if (grasp.grabber is Lizard || grasp.grabber is Vulture || grasp.grabber is BigSpider || grasp.grabber is DropBug || grasp.pacifying) // cmon joarge
                //        {
                //            this.dangerGrasp = grasp;
                //        }
                //    }
                //}
                //else if (this.dangerGrasp.discontinued || (!this.dangerGrasp.pacifying && this.stun <= 0))
                //{
                //    this.dangerGrasp = null;
                //    this.dangerGraspTime = 0;
                //}
                //else
                //{
                //    this.dangerGraspTime++;
                //    if (this.dangerGraspTime == 60)
                //    {
                //        this.room.game.GameOver(this.dangerGrasp);
                //    }
                //}

                //// map progression specifics
                //if (this.MapDiscoveryActive && this.coord != this.lastCoord)
                //{
                //    if (this.exitsToBeDiscovered == null)
                //    {
                //        if (this.room != null && this.room.shortCutsReady)
                //        {
                //            this.exitsToBeDiscovered = new List<Vector2>();
                //            for (int i = 0; i < this.room.shortcuts.Length; i++)
                //            {
                //                if (this.room.shortcuts[i].shortCutType == ShortcutData.Type.RoomExit)
                //                {
                //                    this.exitsToBeDiscovered.Add(this.room.MiddleOfTile(this.room.shortcuts[i].StartTile));
                //                }
                //            }
                //        }
                //    }
                //    else if (this.exitsToBeDiscovered.Count > 0 && this.room.game.cameras[0].hud != null && this.room.game.cameras[0].hud.map != null && !this.room.CompleteDarkness(this.firstChunk.pos, 0f, 0.95f, false))
                //    {
                //        int index = UnityEngine.Random.Range(0, this.exitsToBeDiscovered.Count);
                //        if (this.room.ViewedByAnyCamera(this.exitsToBeDiscovered[index], -10f))
                //        {
                //            Vector2 vector = this.firstChunk.pos;
                //            for (int j = 0; j < 20; j++)
                //            {
                //                if (Custom.DistLess(vector, this.exitsToBeDiscovered[index], 50f))
                //                {
                //                    this.room.game.cameras[0].hud.map.ExternalExitDiscover((vector + this.exitsToBeDiscovered[index]) / 2f, this.room.abstractRoom.index);
                //                    this.room.game.cameras[0].hud.map.ExternalOnePixelDiscover(this.exitsToBeDiscovered[index], this.room.abstractRoom.index);
                //                    this.exitsToBeDiscovered.RemoveAt(index);
                //                    break;
                //                }
                //                this.room.game.cameras[0].hud.map.ExternalSmallDiscover(vector, this.room.abstractRoom.index);
                //                vector += Custom.DirVec(vector, this.exitsToBeDiscovered[index]) * 50f;
                //            }
                //        }
                //    }
                //}

                // Void melt so damn annoying
                if (creature.room?.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.VoidMelt) is RoomSettings.RoomEffect effect)
                {
                    for (int m = 0; m < creature.bodyChunks.Length; m++)
                    {
                        BodyChunk bodyChunk = creature.bodyChunks[m];
                        bodyChunk.vel.y = bodyChunk.vel.y - creature.gravity * 0.5f * effect.amount * (1f - creature.bodyChunks[m].submersion);
                    }
                }

                // cheats
                if (creature.room != null && creature.room.game.devToolsActive)
                {
                    //if (Input.GetKey("q") && !this.FLYEATBUTTON)
                    //{
                    //    this.AddFood(1);
                    //}
                    //this.FLYEATBUTTON = Input.GetKey("q");

                    if (Input.GetKey("v"))
                    {
                        for (int m = 0; m < 2; m++)
                        {
                            creature.bodyChunks[m].vel = Custom.DegToVec(UnityEngine.Random.value * 360f) * 12f;
                            creature.bodyChunks[m].pos = (Vector2)Input.mousePosition + creature.room.game.cameras[0].pos;
                            creature.bodyChunks[m].lastPos = (Vector2)Input.mousePosition + creature.room.game.cameras[0].pos;
                        }
                    }
                    else if (Input.GetKey("w"))
                    {
                        creature.bodyChunks[1].vel += Custom.DirVec(creature.bodyChunks[1].pos, Input.mousePosition) * 7f;
                    }
                }
            }

            internal PhysicalObject PickupCandidate(float favorSpears)
            {
                PhysicalObject result = null;
                float num = float.MaxValue;
                for (int i = 0; i < creature.room.physicalObjects.Length; i++)
                {
                    for (int j = 0; j < creature.room.physicalObjects[i].Count; j++)
                    {
                        if ((!(creature.room.physicalObjects[i][j] is PlayerCarryableItem) || (creature.room.physicalObjects[i][j] as PlayerCarryableItem).forbiddenToPlayer < 1) && Custom.DistLess(creature.bodyChunks[0].pos, creature.room.physicalObjects[i][j].bodyChunks[0].pos, creature.room.physicalObjects[i][j].bodyChunks[0].rad + 40f) && (Custom.DistLess(creature.bodyChunks[0].pos, creature.room.physicalObjects[i][j].bodyChunks[0].pos, creature.room.physicalObjects[i][j].bodyChunks[0].rad + 20f) || creature.room.VisualContact(creature.bodyChunks[0].pos, creature.room.physicalObjects[i][j].bodyChunks[0].pos)) && this.CanIPickThisUp(creature.room.physicalObjects[i][j]))
                        {
                            float num2 = Vector2.Distance(creature.bodyChunks[0].pos, creature.room.physicalObjects[i][j].bodyChunks[0].pos);
                            if (creature.room.physicalObjects[i][j] is Spear)
                            {
                                num2 -= favorSpears;
                            }
                            if (creature.room.physicalObjects[i][j].bodyChunks[0].pos.x < creature.bodyChunks[0].pos.x == this.flipDirection < 0)
                            {
                                num2 -= 10f;
                            }
                            if (num2 < num)
                            {
                                result = creature.room.physicalObjects[i][j];
                                num = num2;
                            }
                        }
                    }
                }
                return result;
            }

            public bool CanIPickThisUp(PhysicalObject obj)
            {
                if (this.Grabability(obj) == Player.ObjectGrabability.CantGrab)
                {
                    return false;
                }
                
                int num = (int)this.Grabability(obj);
                if (num == 2)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (creature.grasps[i] != null && this.Grabability(creature.grasps[i].grabbed) > Player.ObjectGrabability.OneHand)
                        {
                            return false;
                        }
                    }
                }
                
                int num2 = 0;
                for (int l = 0; l < 2; l++)
                {
                    if (creature.grasps[l] != null)
                    {
                        if (creature.grasps[l].grabbed == obj)
                        {
                            return false;
                        }
                        if (this.Grabability(creature.grasps[l].grabbed) > Player.ObjectGrabability.OneHand)
                        {
                            num2++;
                        }
                    }
                }
                return num2 != 2 && (num2 <= 0 || num <= 2);
            }

            private Player.ObjectGrabability Grabability(PhysicalObject obj)
            {
                switch (obj)
                {
                    case DangleFruit:
                    case WaterNut:
                    case SwollenWaterNut:
                    case Mushroom:
                    case Lantern:
                        return Player.ObjectGrabability.OneHand;
                }

                return Player.ObjectGrabability.CantGrab;
            }

            internal void BiteEdibleObject(Creature.Grasp edible, bool eu)
            {
                try
                {
                    (edible.grabbed as IPlayerEdible).BitByPlayer(edible, eu);
                }
                catch (Exception e)
                {
                    RainMeadow.Error(e);
                    throw;
                }
            }

            internal bool CanEat()
            {
                return true;
            }

            internal static void BindCreature(Creature creature)
            {
                if(creature is Cicada cada)
                {
                    var controller = new CicadaController(cada, 0);
                    player.Add(cada.abstractCreature, controller);
                }
            }
        }

        // Lock player and squiddy
        // player inconsious update
        private static void Cicada_Update(On.Cicada.orig_Update orig, Cicada self, bool eu)
        {
            if (player.TryGetValue(self.abstractCreature, out var p))
            {
                p.Update(eu);
            }

            orig(self, eu); // calls Act/Swim/Held
        }

        // inputs and stuff
        // player consious update
        private static void Cicada_Act(On.Cicada.orig_Act orig, Cicada self)
        {
            if (player.TryGetValue(self.abstractCreature, out var p))
            {
                var room = self.room;
                var chunks = self.bodyChunks;
                var nc = chunks.Length;

                //// shroom things
                //if (p.Adrenaline > 0f)
                //{
                //    if (self.waitToFlyCounter < 30) self.waitToFlyCounter = 30;
                //    if (self.flying)
                //    {
                //        self.flyingPower = Mathf.Lerp(self.flyingPower, 1.4f, 0.03f * p.Adrenaline);
                //    }
                //}
                //var stuccker = self.AI.stuckTracker; // used while in climbing/pipe mode
                //stuccker.stuckCounter = (int)Mathf.Lerp(stuccker.minStuckCounter, stuccker.maxStuckCounter, p.Adrenaline);

                // faster takeoff
                if (self.waitToFlyCounter <= 15)
                    self.waitToFlyCounter = 15;

                var inputDir = p.input[0].analogueDir.magnitude > 0.2f ? p.input[0].analogueDir
                    : p.input[0].IntVec.ToVector2().magnitude > 0.2 ? p.input[0].IntVec.ToVector2().normalized
                    : Vector2.zero;

                var inputLastDir = p.input[1].analogueDir.magnitude > 0.2f ? p.input[1].analogueDir
                    : p.input[1].IntVec.ToVector2().magnitude > 0.2 ? p.input[1].IntVec.ToVector2().normalized
                    : Vector2.zero;

                bool preventStaminaRegen = false;
                if (p.input[0].thrw && !p.input[1].thrw) p.wantToJump = 5;
                if (p.wantToJump > 0) // dash charge
                {
                    if (self.flying && !self.Charging && self.chargeCounter == 0 && self.stamina > 0.2f)
                    {
                        self.Charge(self.mainBodyChunk.pos + (inputDir == Vector2.zero ? (chunks[0].pos - chunks[1].pos) : inputDir) * 100f);
                        p.wantToJump = 0;
                    }
                }

                if (self.chargeCounter > 0) // charge windup or midcharge
                {
                    self.stamina -= 0.008f;
                    preventStaminaRegen = true;
                    if (self.chargeCounter < 20)
                    {
                        if (self.stamina <= 0.2f || !p.input[0].thrw) // cancel out if unable to complete
                        {
                            self.chargeCounter = 0;
                        }
                    }
                    else
                    {
                        if (self.stamina <= 0f) // cancel out mid charge if out of stamina (happens in long bouncy charges)
                        {
                            self.chargeCounter = 0;
                        }
                    }
                    self.chargeDir = (self.chargeDir
                                                + 0.15f * inputDir
                                                + 0.03f * Custom.DirVec(self.bodyChunks[1].pos, self.mainBodyChunk.pos)).normalized;

                    if (self.Charging && self.grasps[0] != null && self.grasps[0].grabbed is Weapon w)
                    {
                        SharedPhysics.CollisionResult result = SharedPhysics.TraceProjectileAgainstBodyChunks(null, self.room, w.firstChunk.lastPos, ref w.firstChunk.pos, w.firstChunk.rad, 1, self, true);
                        if (result.hitSomething)
                        {
                            var dir = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
                            var throwndir = new IntVector2(Mathf.Abs(dir.x) > 0.38 ? (int)Mathf.Sign(dir.x) : 0, Mathf.Abs(dir.y) > 0.38 ? (int)Mathf.Sign(dir.y) : 0);
                            w.Thrown(self, w.firstChunk.pos, w.firstChunk.lastPos, throwndir, 1f, self.evenUpdate);
                            if (w is Spear sp && !(result.obj is Player))
                            {
                                sp.spearDamageBonus *= 0.6f;
                                sp.setRotation = dir;
                            }
                            w.Forbid();
                            self.ReleaseGrasp(0);
                        }
                    }
                }

                // scoooot
                self.AI.swooshToPos = null;
                if (p.input[0].jmp)
                {
                    if (self.room.aimap.getAItile(self.mainBodyChunk.pos).terrainProximity > 1 && self.stamina > 0.5f) // cada.flying && 
                    {
                        self.AI.swooshToPos = self.mainBodyChunk.pos + inputDir * 40f + new Vector2(0, 4f);
                        self.flyingPower = Mathf.Lerp(self.flyingPower, 1f, 0.05f);
                        preventStaminaRegen = true;
                        self.stamina -= 0.6f * self.stamina * inputDir.magnitude / ((!self.gender) ? 120f : 190f);
                    }
                    else // easier takeoff
                    {
                        if (self.waitToFlyCounter < 30) self.waitToFlyCounter = 30;
                    }
                }

                // move
                var basepos = 0.5f * (self.firstChunk.pos + room.MiddleOfTile(self.abstractCreature.pos.Tile));
                if (inputDir != Vector2.zero || self.Charging)
                {
                    self.AI.pathFinder.AbortCurrentGenerationPathFinding(); // ignore previous dest
                    self.AI.behavior = CicadaAI.Behavior.GetUnstuck; // helps with sitting behavior
                    var dest = basepos + inputDir * 20f;
                    if (self.flying) dest.y -= 12f; // nose up goes funny
                    if (Mathf.Abs(inputDir.y) < 0.1f) // trying to move horizontally, compensate for momentum a bit
                    {
                        dest.y -= self.mainBodyChunk.vel.y * 1.3f;
                    }
                    self.abstractCreature.abstractAI.SetDestination(self.room.GetWorldCoordinate(dest));
                }
                else
                {
                    self.AI.behavior = CicadaAI.Behavior.Idle;
                    if (inputDir == Vector2.zero && inputLastDir != Vector2.zero) // let go
                    {
                        self.abstractCreature.abstractAI.SetDestination(self.room.GetWorldCoordinate(basepos));
                    }
                }

                // Grab update was becoming too big just like player
                GrabUpdate(self, p, inputDir);

                // player direct into holes simplified equivalent
                if ((p.input[0].x == 0 || p.input[0].y == 0) && p.input[0].x != p.input[0].y) // a straight direction
                {
                    for (int n = 0; n < nc; n++)
                    {
                        if (room.GetTile(chunks[n].pos + p.input[0].IntVec.ToVector2() * 40f).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                        {
                            chunks[n].vel += (room.MiddleOfTile(chunks[n].pos + new Vector2(20f * (float)p.input[0].x, 20f * (float)p.input[0].y)) - chunks[n].pos) / 10f;
                            break;
                        }
                    }
                }

                // from player movementupdate code, entering a shortcut
                if (self.shortcutDelay < 1)
                {
                    self.abstractCreature.remainInDenCounter = 200; // so can eat whatever whenever
                    for (int i = 0; i < nc; i++)
                    {
                        if (self.enteringShortCut == null && room.GetTile(chunks[i].pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                        {
                            var sctype = room.shortcutData(room.GetTilePosition(chunks[i].pos)).shortCutType;
                            if (sctype != ShortcutData.Type.DeadEnd
                            //&& (sctype != ShortcutData.Type.CreatureHole || self.abstractCreature.abstractAI.HavePrey())
                            && sctype != ShortcutData.Type.NPCTransportation)
                            {
                                IntVector2 intVector = room.ShorcutEntranceHoleDirection(room.GetTilePosition(chunks[i].pos));
                                if (p.input[0].x == -intVector.x && p.input[0].y == -intVector.y)
                                {
                                    Debug.Log("Squiddy: Entering shortcut");
                                    self.enteringShortCut = new IntVector2?(room.GetTilePosition(chunks[i].pos));
                                }
                            }
                        }
                    }
                }

                if (preventStaminaRegen) // opposite of what happens in orig
                {
                    if (self.grabbedBy.Count == 0 && self.stickyCling == null)
                    {
                        self.stamina -= 0.014285714f;
                    }
                }
                self.stamina = Mathf.Clamp01(self.stamina);
            }

            orig(self);
        }

        private static void Cicada_Swim(On.Cicada.orig_Swim orig, Cicada self)
        {
            if (player.TryGetValue(self.abstractCreature, out var p))
            {
                if (self.Consious)
                {
                    self.Act();
                    if (self.Submersion == 1f)
                    {
                        self.flying = false;
                        if (self.graphicsModule is CicadaGraphics cg)
                        {
                            cg.wingDeploymentGetTo = 0.2f;
                        }
                        self.waitToFlyCounter = 0; // so graphics uses wingdeployment
                    }
                }
            }
            orig(self);
        }

        private static void Cicada_CarryObject(On.Cicada.orig_CarryObject orig, Cicada self)
        {
            if (player.TryGetValue(self.abstractCreature, out var p))
            {
                // more realistic grab pos plz
                var oldpos = self.mainBodyChunk.pos;
                var owndir = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
                self.mainBodyChunk.pos += 5f * owndir;
                orig(self); // this thing drops creatures cada doesn't eat. it's a bit weird but its ok I guess
                self.mainBodyChunk.pos = oldpos;
                if (self.grasps[0] != null && self.grasps[0].grabbed is Spear speary)
                {
                    var hangingdir = (speary.firstChunk.pos - oldpos).normalized;
                    speary.setRotation = Vector2.Lerp(
                        Vector2.Lerp(Custom.PerpendicularVector(owndir), owndir.normalized, ((float)self.chargeCounter) / 20f),
                        speary.rotation, 0.25f);
                    speary.rotationSpeed = 0f;
                }
                return;
            }
            orig(self);
        }

        // charging on creature, attack chunk
        private static void Cicada_Collide(On.Cicada.orig_Collide orig, Cicada self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (player.TryGetValue(self.abstractCreature, out var p))
            {
                if (self.Charging && self.grasps[0] != null && self.grasps[0].grabbed is Weapon we && myChunk == 0 && otherChunk >= 0)
                {
                    var dir = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
                    var throwndir = new IntVector2(Mathf.Abs(dir.x) > 0.38 ? (int)Mathf.Sign(dir.x) : 0, Mathf.Abs(dir.y) > 0.38 ? (int)Mathf.Sign(dir.y) : 0);
                    we.Thrown(self, self.mainBodyChunk.pos + dir * 30f, self.mainBodyChunk.pos, throwndir, 1f, self.evenUpdate);
                    we.meleeHitChunk = otherObject.bodyChunks[otherChunk];
                    if (we is Spear sp && !(otherObject is Player))
                    {
                        sp.spearDamageBonus *= 0.6f;
                        sp.setRotation = dir;
                    }
                    we.Forbid();
                    self.ReleaseGrasp(0);
                }
                orig(self, otherObject, myChunk, otherChunk);
                return;
            }
            orig(self, otherObject, myChunk, otherChunk);
        }

        // fall damage, and embed sticks
        private static void Cicada_TerrainImpact(On.Cicada.orig_TerrainImpact orig, Cicada self, int chunk, IntVector2 direction, float speed, bool firstContact)
        {
            orig(self, chunk, direction, speed, firstContact);

            if (player.TryGetValue(self.abstractCreature, out var ap))
            {
                if (speed > 60f && direction.y < 0)
                {
                    self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Death, self.mainBodyChunk);
                    Debug.Log("Fall damage death");
                    self.Die();
                }
                else if (speed > (self.Charging ? 45f : 35f))
                {
                    self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Hard, self.mainBodyChunk);
                    self.Stun((int)Custom.LerpMap(speed, 35f, 60f, 40f, 140f, 2.5f));
                }


                if (self.grasps[0]?.grabbed is Spear speary)
                {
                    var bodyDir = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
                    var dir = direction.ToVector2();
                    if (Vector2.Dot(bodyDir, dir) > 0.67)
                    {
                        speary.Thrown(self, speary.firstChunk.pos, speary.firstChunk.lastPos, direction, 1f, self.evenUpdate);
                        speary.spearDamageBonus *= 0.6f;
                        speary.setRotation = dir;
                        speary.Forbid();
                        self.ReleaseGrasp(0);
                    }
                }
            }
        }

        // dont let AI interfere on squiddy
        private static void CicadaAI_Update(On.CicadaAI.orig_Update orig, CicadaAI self)
        {
            if (player.TryGetValue(self.creature, out var p))
            {
                if (self.cicada.room?.Tiles != null && !self.pathFinder.DoneMappingAccessibility)
                    self.pathFinder.accessibilityStepsPerFrame = self.cicada.room.Tiles.Length; // faster, damn it. on entering a new room this needs to complete before it can pathfind
                else self.pathFinder.accessibilityStepsPerFrame = 10;
                self.pathFinder.Update(); // basic movement uses this
                self.tracker.Update(); // creature looker uses this

                self.timeInRoom++;
            }
            else
            {
                orig(self);
            }
        }

        private static void Cicada_GrabbedByPlayer(On.Cicada.orig_GrabbedByPlayer orig, Cicada self)
        {
            if (player.TryGetValue(self.abstractCreature, out var p) && self.Consious)
            {
                var oldflypower = self.flyingPower;
                self.flyingPower *= 0.6f;
                self.Act();
                orig(self);
                self.flyingPower = oldflypower;
            }
            else
            {
                orig(self);
            }
        }

        // cicada pather gets confused about entering shortcuts, let our code handle that instead, also patchup zerog
        private static void Cicada_Act1(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel dorun = null;
            ILLabel dontrun = null;
            try
            {
                c.GotoNext(MoveType.AfterLabel,
                i => i.MatchLdloc(0),
                i => i.MatchLdfld<MovementConnection>("type"),
                i => i.MatchLdcI4(13),
                i => i.MatchBeq(out dorun),
                i => i.MatchLdloc(0),
                i => i.MatchLdfld<MovementConnection>("type"),
                i => i.MatchLdcI4(14),
                i => i.MatchBneUn(out dontrun)
                );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<Cicada, bool>>((self) => // squiddy don't
                {
                    if (player.TryGetValue(self.abstractCreature, out var p))
                    {
                        return true;
                    }
                    return false;
                });
                c.Emit(OpCodes.Brtrue, dontrun); // dont run if squiddy
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
            // patchup zerog
            c.Index = 0;
            while (c.TryGotoNext(MoveType.After,
                i => (i.MatchMul() && i.Previous.MatchLdcR4(out _)) || i.MatchLdcR4(out _),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Cicada>("flyingPower"),
                i => i.MatchMul(),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Cicada>("stamina"),
                i => i.MatchMul(),
                i => i.MatchAdd(),
                i => i.MatchStfld<Vector2>("y")
                ))
            {
                c.Index -= 2;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit<PhysicalObject>(OpCodes.Callvirt, "get_gravity");
                c.Emit(OpCodes.Mul);
                c.Emit(OpCodes.Ldc_R4, (float)(1d / 0.9d));
                c.Emit(OpCodes.Mul);
            }
        }

        // things were getting out of hand
        private static void GrabUpdate(Cicada self, CreatureController p, Vector2 inputDir)
        {
            var room = self.room;
            var grasps = self.grasps;
            bool holdingGrab = p.input[0].pckp;
            bool still = (inputDir == Vector2.zero && !p.input[0].thrw && !p.input[0].jmp && self.Submersion < 0.5f);
            bool eating = false;
            bool swallow = false;
            if (still)
            {
                Creature.Grasp edible = grasps.FirstOrDefault(g => g != null && g.grabbed is IPlayerEdible ipe && ipe.Edible);

                if (edible != null && (holdingGrab || p.eatCounter < 15))
                {
                    eating = true;
                    if (edible.grabbed is IPlayerEdible ipe)
                    {
                        if (ipe.FoodPoints <= 0 || p.CanEat()) // can eat
                        {
                            if (p.eatCounter < 1)
                            {
                                p.eatCounter = 15;
                                p.BiteEdibleObject(edible, self.evenUpdate);
                            }
                        }
                        else // no can eat
                        {
                            if (p.eatCounter < 20 && room.game.cameras[0].hud != null)
                            {
                                room.game.cameras[0].hud.foodMeter.RefuseFood();
                            }
                            edible = null;
                        }
                    }
                }
            }

            if (eating && p.eatCounter > 0)
            {
                p.eatCounter--;
            }
            else if (!eating && p.eatCounter < 40)
            {
                p.eatCounter++;
            }

            // this was in vanilla might as well keep it
            foreach (var grasp in grasps) if (grasp != null && grasp.grabbed.slatedForDeletetion) self.ReleaseGrasp(grasp.graspUsed);

            // pickup updage
            if (p.input[0].pckp && !p.input[1].pckp) p.wantToPickUp = 5;

            PhysicalObject physicalObject = (p.dontGrabStuff >= 1) ? null : p.PickupCandidate(8f);
            if (p.pickUpCandidate != physicalObject && physicalObject != null && physicalObject is PlayerCarryableItem)
            {
                (physicalObject as PlayerCarryableItem).Blink();
            }
            p.pickUpCandidate = physicalObject;

            if (p.wantToPickUp > 0) // pick up
            {
                var dropInstead = true; // grasps.Any(g => g != null);
                for (int i = 0; i < p.input.Length && i < 5; i++)
                {
                    if (p.input[i].y > -1) dropInstead = false;
                }
                if (dropInstead)
                {
                    for (int i = 0; i < grasps.Length; i++)
                    {
                        if (grasps[i] != null)
                        {
                            p.wantToPickUp = 0;
                            room.PlaySound((!(grasps[i].grabbed is Creature)) ? SoundID.Slugcat_Lay_Down_Object : SoundID.Slugcat_Lay_Down_Creature, grasps[i].grabbedChunk, false, 1f, 1f);
                            room.socialEventRecognizer.CreaturePutItemOnGround(grasps[i].grabbed, p.creature);
                            if (grasps[i].grabbed is PlayerCarryableItem)
                            {
                                (grasps[i].grabbed as PlayerCarryableItem).Forbid();
                            }
                            self.ReleaseGrasp(i);
                            break;
                        }
                    }
                }
                else if (p.pickUpCandidate != null)
                {
                    int freehands = 0;
                    for (int i = 0; i < grasps.Length; i++)
                    {
                        if (grasps[i] == null)
                        {
                            freehands++;
                        }
                    }

                    for (int i = 0; i < grasps.Length; i++)
                    {
                        if (grasps[i] == null)
                        {
                            if (self.TryToGrabPrey(p.pickUpCandidate))
                            {
                                Debug.Log("Squiddy: grabbed " + p.pickUpCandidate);
                                p.pickUpCandidate = null;
                                p.wantToPickUp = 0;
                            }
                            break;
                        }
                    }
                }
            }
        }
    }
}
