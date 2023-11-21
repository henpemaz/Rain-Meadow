﻿using RWCustom;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public partial class MeadowCustomization
    {
        public static ConditionalWeakTable<AbstractCreature, CreatureController> creatureController = new();
        public abstract class CreatureController
        {
            public DebugDestinationVisualizer debugDestinationVisualizer;

            public static void BindCreature(Creature creature)
            {
                if (creature is Cicada cada)
                {
                    var controller = new CicadaController(cada, 0);
                    creatureController.Add(cada.abstractCreature, controller);
                }
                else if (creature is Lizard liz)
                {
                    var controller = new LizardController(liz, 0);
                    creatureController.Add(liz.abstractCreature, controller);
                }
            }

            public Creature creature;

            public CreatureController(Creature creature, int playerNumber)
            {
                this.creature = creature;
                this.playerNumber = playerNumber;

                input = new Player.InputPackage[10];
                rainWorld = creature.abstractCreature.world.game.rainWorld;

                //creature.abstractCreature.abstractAI.RealAI.pathFinder.visualize = true;
                //debugDestinationVisualizer = new DebugDestinationVisualizer(creature.abstractCreature.world.game.abstractSpaceVisualizer, creature.abstractCreature.world, creature.abstractCreature.abstractAI.RealAI.pathFinder, Color.green);
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
            public PhysicalObject pickUpCandidate;
            public int dontGrabStuff;
            public int eatCounter;
            public int flipDirection;
            public Vector2 inputDir;
            public Vector2 inputLastDir;

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

                inputDir = input[0].analogueDir.magnitude > 0.2f ? input[0].analogueDir
                    : input[0].IntVec.ToVector2().magnitude > 0.2 ? input[0].IntVec.ToVector2().normalized
                    : Vector2.zero;

                inputLastDir = input[1].analogueDir.magnitude > 0.2f ? input[1].analogueDir
                    : input[1].IntVec.ToVector2().magnitude > 0.2 ? input[1].IntVec.ToVector2().normalized
                    : Vector2.zero;
            }
            public virtual void Update(bool eu)
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

                if (this.debugDestinationVisualizer != null)
                {
                    this.debugDestinationVisualizer.Update();
                }
            }

            public void ForceAIDestination(WorldCoordinate coord)
            {
                var absAI = creature.abstractCreature.abstractAI;
                var realAI = absAI.RealAI;
                absAI.SetDestination(coord);
                // pathfinder has some "optimizations" that need bypassing
                realAI.pathFinder.nextDestination = null;
                realAI.pathFinder.AssignNewDestination(coord);
            }

            public virtual PhysicalObject PickupCandidate(float favorSpears)
            {
                PhysicalObject result = null;
                float num = float.MaxValue;
                for (int i = 0; i < creature.room.physicalObjects.Length; i++)
                {
                    for (int j = 0; j < creature.room.physicalObjects[i].Count; j++)
                    {
                        var candidate = creature.room.physicalObjects[i][j];
                        if ((!(candidate is PlayerCarryableItem) || (candidate as PlayerCarryableItem).forbiddenToPlayer < 1) && Custom.DistLess(creature.bodyChunks[0].pos, candidate.bodyChunks[0].pos, candidate.bodyChunks[0].rad + 40f) && (Custom.DistLess(creature.bodyChunks[0].pos, candidate.bodyChunks[0].pos, candidate.bodyChunks[0].rad + 20f) || creature.room.VisualContact(creature.bodyChunks[0].pos, candidate.bodyChunks[0].pos)) && this.CanIPickThisUp(candidate))
                        {
                            float num2 = Vector2.Distance(creature.bodyChunks[0].pos, creature.room.physicalObjects[i][j].bodyChunks[0].pos);
                            if (candidate is Spear)
                            {
                                num2 -= favorSpears;
                            }
                            if (candidate.bodyChunks[0].pos.x < creature.bodyChunks[0].pos.x == this.flipDirection < 0)
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

            public void BiteEdibleObject(Creature.Grasp edible, bool eu)
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

            public bool CanEat()
            {
                return true;
            }

            // things were getting out of hand
            public void GrabUpdate()
            {
                var room = creature.room;
                var grasps = creature.grasps;
                bool holdingGrab = input[0].pckp;
                bool still = (inputDir == Vector2.zero && !input[0].thrw && !input[0].jmp && creature.Submersion < 0.5f);
                bool eating = false;
                bool swallow = false;
                if (still)
                {
                    Creature.Grasp edible = grasps.FirstOrDefault(g => g != null && g.grabbed is IPlayerEdible ipe && ipe.Edible);

                    if (edible != null && (holdingGrab || eatCounter < 15))
                    {
                        eating = true;
                        if (edible.grabbed is IPlayerEdible ipe)
                        {
                            if (ipe.FoodPoints <= 0 || CanEat()) // can eat
                            {
                                if (eatCounter < 1)
                                {
                                    eatCounter = 15;
                                    BiteEdibleObject(edible, creature.evenUpdate);
                                }
                            }
                            else // no can eat
                            {
                                if (eatCounter < 20 && room.game.cameras[0].hud != null)
                                {
                                    room.game.cameras[0].hud.foodMeter.RefuseFood();
                                }
                                edible = null;
                            }
                        }
                    }
                }

                if (eating && eatCounter > 0)
                {
                    eatCounter--;
                }
                else if (!eating && eatCounter < 40)
                {
                    eatCounter++;
                }

                // this was in vanilla might as well keep it
                foreach (var grasp in grasps) if (grasp != null && grasp.grabbed.slatedForDeletetion) creature.ReleaseGrasp(grasp.graspUsed);

                // pickup updage
                if (input[0].pckp && !input[1].pckp) wantToPickUp = 5;

                PhysicalObject physicalObject = (dontGrabStuff >= 1) ? null : PickupCandidate(8f);
                if (pickUpCandidate != physicalObject && physicalObject != null && physicalObject is PlayerCarryableItem)
                {
                    (physicalObject as PlayerCarryableItem).Blink();
                }
                pickUpCandidate = physicalObject;

                if (wantToPickUp > 0) // pick up
                {
                    var dropInstead = true; // grasps.Any(g => g != null);
                    for (int i = 0; i < input.Length && i < 5; i++)
                    {
                        if (input[i].y > -1) dropInstead = false;
                    }
                    if (dropInstead)
                    {
                        for (int i = 0; i < grasps.Length; i++)
                        {
                            if (grasps[i] != null)
                            {
                                wantToPickUp = 0;
                                room.PlaySound((!(grasps[i].grabbed is Creature)) ? SoundID.Slugcat_Lay_Down_Object : SoundID.Slugcat_Lay_Down_Creature, grasps[i].grabbedChunk, false, 1f, 1f);
                                room.socialEventRecognizer.CreaturePutItemOnGround(grasps[i].grabbed, creature);
                                if (grasps[i].grabbed is PlayerCarryableItem)
                                {
                                    (grasps[i].grabbed as PlayerCarryableItem).Forbid();
                                }
                                creature.ReleaseGrasp(i);
                                break;
                            }
                        }
                    }
                    else if (pickUpCandidate != null)
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
                                if (GrabImpl(pickUpCandidate))
                                {
                                    pickUpCandidate = null;
                                    wantToPickUp = 0;
                                }
                                break;
                            }
                        }
                    }
                }
            }

            public abstract bool GrabImpl(PhysicalObject pickUpCandidate);

            public virtual void ConsciousUpdate()
            {
                var room = creature.room;
                var chunks = creature.bodyChunks;
                var nc = chunks.Length;

                GrabUpdate();

                // player direct into holes simplified equivalent
                if ((input[0].x == 0 || input[0].y == 0) && input[0].x != input[0].y) // a straight direction
                {
                    for (int n = 0; n < nc; n++)
                    {
                        if (room.GetTile(chunks[n].pos + input[0].IntVec.ToVector2() * 40f).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                        {
                            chunks[n].vel += (room.MiddleOfTile(chunks[n].pos + new Vector2(20f * input[0].x, 20f * input[0].y)) - chunks[n].pos) / 10f;
                            break;
                        }
                    }
                }

                // from player movementupdate code, entering a shortcut
                if (creature.shortcutDelay < 1)
                {
                    for (int i = 0; i < nc; i++)
                    {
                        if (creature.enteringShortCut == null && room.GetTile(chunks[i].pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                        {
                            var sctype = room.shortcutData(room.GetTilePosition(chunks[i].pos)).shortCutType;
                            if (sctype != ShortcutData.Type.DeadEnd
                            && sctype != ShortcutData.Type.NPCTransportation)
                            {
                                IntVector2 intVector = room.ShorcutEntranceHoleDirection(room.GetTilePosition(chunks[i].pos));
                                if (input[0].x == -intVector.x && input[0].y == -intVector.y)
                                {
                                    creature.enteringShortCut = new IntVector2?(room.GetTilePosition(chunks[i].pos));
                                }
                            }
                        }
                    }
                }
            }

            public void AIUpdate(ArtificialIntelligence ai)
            {
                if (creature.room?.Tiles != null && !ai.pathFinder.DoneMappingAccessibility)
                    ai.pathFinder.accessibilityStepsPerFrame = creature.room.Tiles.Length; // faster, damn it. on entering a new room this needs to complete before it can pathfind
                else ai.pathFinder.accessibilityStepsPerFrame = 10;
                ai.pathFinder.Update(); // basic movement uses this
                ai.tracker.Update(); // creature looker uses this
                ai.timeInRoom++;
            }
        }
    }
}
