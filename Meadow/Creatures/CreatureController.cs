using System;
using UnityEngine;
using RWCustom;
using System.Runtime.CompilerServices;
using System.Linq;
using HUD;
using Rewired;

namespace RainMeadow
{
    public abstract class CreatureController : IOwnAHUD
    {
        public DebugDestinationVisualizer debugDestinationVisualizer;
        public static ConditionalWeakTable<AbstractCreature, CreatureController> creatureControllers = new();
        internal static void BindAvatar(Creature creature, OnlineCreature oc)
        {
            if (creature is Cicada cada)
            {
                new CicadaController(cada, oc, 0);
            }
            else if (creature is Lizard liz)
            {
                new LizardController(liz, oc, 0);
            }
            else if (creature is Scavenger scav)
            {
                new ScavengerController(scav, oc, 0);
            }
            else if (creature is NeedleWorm noodle)
            {
                new NoodleController(noodle, oc, 0);
            }
            else if (creature is EggBug bug)
            {
                new EggbugController(bug, oc, 0);
            }
            else if (creature is Player player)
            {
                if (!oc.owner.isMe)
                {
                    player.controller = new OnlineController(oc, player);
                }
            }
            else
            {
                throw new InvalidProgrammerException("You need to implement " + creature.ToString());
            }
        }

        public Creature creature;
        public OnlineCreature onlineCreature;
        public RainWorld rainWorld;

        public CreatureController(Creature creature, OnlineCreature oc, int playerNumber)
        {
            this.creature = creature;
            this.onlineCreature = oc;
            this.playerNumber = playerNumber;

            rainWorld = creature.abstractCreature.world.game.rainWorld;

            if (creatureControllers.TryGetValue(creature.abstractCreature, out _))
            {
                RainMeadow.Error($"Creature was already bound to a controller! {creature} {oc}");
                creatureControllers.Remove(creature.abstractCreature);
            }
            creatureControllers.Add(creature.abstractCreature, this);

            standStillOnMapButton = creature.abstractCreature.world.game.IsStorySession;
            flipDirection = 1;

            RainMeadow.Debug(this + " added!");

            if (oc.isMine)
            {
                //creature.abstractCreature.abstractAI.RealAI.pathFinder.visualize = true;
                debugDestinationVisualizer = new DebugDestinationVisualizer(creature.abstractCreature.world.game.abstractSpaceVisualizer, creature.abstractCreature.world, creature.abstractCreature.abstractAI.RealAI.pathFinder, Color.green);
            }
        }

        public int playerNumber = 0;
        public Player.InputPackage[] input = new Player.InputPackage[10];
        public Vector2 inputDir;
        public Vector2 inputLastDir;

        public int wantToJump;
        public int wantToPickUp;
        public int wantToThrow;

        public int flipDirection;
        public int sleepCounter;
        public int blink;

        public int touchedNoInputCounter;
        public bool standStillOnMapButton;
        public bool readyForWin;

        public PhysicalObject pickUpCandidate;
        public int dontGrabStuff;
        public int eatCounter;
        public int dontEatExternalFoodSourceCounter;
        public int eatExternalFoodSourceCounter;

        public SpecialInput[] specialInput = new SpecialInput[2];

        // IOwnAHUD
        public int CurrentFood => 0;
        // IOwnAHUD
        public Player.InputPackage MapInput => rawInput;
        private Player.InputPackage rawInput;
        // IOwnAHUD
        public bool RevealMap => input[0].mp;
        // IOwnAHUD
        public Vector2 MapOwnerInRoomPosition
        {
            get
            {
                if (this.creature.room == null && this.creature.inShortcut && this.creature.abstractCreature.Room.realizedRoom != null)

                {
                    Vector2? vector = this.creature.abstractCreature.Room.realizedRoom.game.shortcuts.OnScreenPositionOfInShortCutCreature(this.creature.abstractCreature.Room.realizedRoom, this.creature);
                    if (vector != null)
                    {
                        return vector.Value;
                    }
                }
                return this.creature.mainBodyChunk.pos;
            }
        }
        // IOwnAHUD
        public bool MapDiscoveryActive => this.creature.Consious && this.creature.room != null && !this.creature.room.world.singleRoomWorld && this.creature.mainBodyChunk.pos.x > 0f && this.creature.mainBodyChunk.pos.x < this.creature.room.PixelWidth && this.creature.mainBodyChunk.pos.y > 0f && this.creature.mainBodyChunk.pos.y < this.creature.room.PixelHeight;
        // IOwnAHUD
        public int MapOwnerRoom => this.creature.abstractPhysicalObject.pos.room;

        public virtual WorldCoordinate CurrentPathfindingPosition
        {
            get
            {
                return creature.room.GetWorldCoordinate(creature.mainBodyChunk.pos);
            }
        }
        // IOwnAHUD
        public void PlayHUDSound(SoundID soundID)
        {
            this.creature.abstractCreature.world.game.cameras[0].virtualMicrophone.PlaySound(soundID, 0f, 1f, 1f);
        }
        // IOwnAHUD
        public void FoodCountDownDone() { }
        // IOwnAHUD
        public static HUD.HUD.OwnerType controlledCreatureHudOwner = new("MeadowControlledCreature", true);
        public bool lockInPlace;

        public HUD.HUD.OwnerType GetOwnerType() => controlledCreatureHudOwner;

        public virtual void CheckInput()
        {
            for (int i = this.input.Length - 1; i > 0; i--)
            {
                this.input[i] = this.input[i - 1];
            }

            if (onlineCreature.isMine)
            {
                if (creature.stun == 0 && !creature.dead)
                {
                    this.input[0] = RWInput.PlayerInput(playerNumber, rainWorld);
                }
                else
                {
                    this.input[0] = new Player.InputPackage(rainWorld.options.controls[playerNumber].gamePad, rainWorld.options.controls[playerNumber].GetActivePreset(), 0, 0, false, false, false, false, false);
                }

                if (onlineCreature.TryGetData<MeadowCreatureData>(out var mcd))
                {
                    mcd.input = this.input[0];
                }
                else
                {
                    RainMeadow.Error("Missing mcd on send");
                }
            }
            else
            {
                if (onlineCreature.TryGetData<MeadowCreatureData>(out var mcd))
                {
                    this.input[0] = mcd.input;
                }
                else
                {
                    RainMeadow.Error("Missing mcd on receive");
                }
            }

            for (int i = this.specialInput.Length - 1; i > 0; i--)
            {
                this.specialInput[i] = this.specialInput[i - 1];
            }

            if (onlineCreature.isMine)
            {
                this.specialInput[0] = GetSpecialInput(creature.DangerPos - creature.room.game.cameras[0].pos, playerNumber, rainWorld);

                if (onlineCreature.TryGetData<MeadowCreatureData>(out var mcd))
                {
                    mcd.specialInput = this.specialInput[0];
                }
                else
                {
                    RainMeadow.Error("Missing mcd on send");
                }
            }
            else
            {
                if (onlineCreature.TryGetData<MeadowCreatureData>(out var mcd))
                {
                    this.specialInput[0] = mcd.specialInput;
                }
                else
                {
                    RainMeadow.Error("Missing mcd on receive");
                }
            }

            rawInput = this.input[0];
            if ((this.standStillOnMapButton && this.input[0].mp) || this.sleepCounter != 0)
            {
                this.input[0].x = 0;
                this.input[0].y = 0;
                this.input[0].analogueDir = default;
                this.input[0].jmp = false;
                this.input[0].thrw = false;
                this.input[0].pckp = false;
                this.Blink(5);
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
        }

        private SpecialInput GetSpecialInput(Vector2 referencePoint, int playerNumber, RainWorld rainWorld)
        {
            SpecialInput specialInput = default;
            //var controllerPreset = rainWorld.options.controls[playerNumber].GetActivePreset();
            var controller = rainWorld.options.controls[playerNumber].GetActiveController();
            if(controller is Joystick joystick)
            {
                //if(controllerPreset == Options.ControlSetup.Preset.XBox)
                specialInput.direction = Vector2.ClampMagnitude(new Vector2(joystick.GetAxis(2), joystick.GetAxis(3)), 1f);
            }
            else
            {
                if(Input.GetMouseButton(0))
                {
                    specialInput.direction = Vector2.ClampMagnitude((((Vector2)Futile.mousePosition) - referencePoint) / 500f, 1f);
                }
            }
            return specialInput;
        }

        public struct SpecialInput : Serializer.ICustomSerializable
        {
            public Vector2 direction;

            public void CustomSerialize(Serializer serializer)
            {
                serializer.SerializeHalf(ref direction.x);
                serializer.SerializeHalf(ref direction.y);
            }

            public static bool operator ==(SpecialInput self, SpecialInput other)
            {
                return self.direction == other.direction;
            }
            public static bool operator !=(SpecialInput self, SpecialInput other)
            {
                return self.direction != other.direction;
            }
        }

        private void Blink(int blink)
        {
            this.blink = Mathf.Max(this.blink, blink);
        }

        internal virtual void Update(bool eu)
        {
            // Input
            this.CheckInput();

            inputLastDir = inputDir;
            inputDir = input[0].analogueDir.magnitude > 0.2f ? input[0].analogueDir
                : input[0].IntVec.ToVector2().magnitude > 0.2 ? input[0].IntVec.ToVector2().normalized
                : Vector2.zero;


            // a lot of things copypasted from from p.update
            if (this.wantToJump > 0) this.wantToJump--;
            if (this.wantToPickUp > 0) this.wantToPickUp--;
            if (this.wantToThrow > 0) this.wantToThrow--;

            if (this.input[0].jmp && !this.input[1].jmp) wantToJump = 5;
            if (this.input[0].pckp && !this.input[1].pckp) wantToPickUp = 5;
            if (this.input[0].thrw && !this.input[1].thrw) wantToThrow = 5;


            // bunch of unimplemented story things
            // relevant to story
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

            // relevant to story
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

            // relevant to story
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

            // relevant to story
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

            // relevant to story AND meadow?
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
            if (creature.room != null && creature.room.game.devToolsActive && onlineCreature.isMine)
            {
                // relevant to story
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
                var visibility = creature.abstractCreature.world.game.devToolsActive;
                this.debugDestinationVisualizer.sprite1.sprite.isVisible = visibility;
                this.debugDestinationVisualizer.sprite2.sprite.isVisible = visibility;
                this.debugDestinationVisualizer.sprite3.sprite.isVisible = visibility;
                this.debugDestinationVisualizer.sprite4.sprite.isVisible = visibility;
                if (debugDestinationVisualizer.room != creature.room) debugDestinationVisualizer.ChangeRooms(creature.room);
                this.debugDestinationVisualizer.Update();
            }
        }

        public virtual void ForceAIDestination(WorldCoordinate coord)
        {
            if (onlineCreature.isMine)
            {
                if (onlineCreature.TryGetData<MeadowCreatureData>(out var mcd))
                {
                    mcd.destination = coord;
                }
                else
                {
                    throw new InvalidProgrammerException("Missing mcd");
                }
            }
            var absAI = creature.abstractCreature.abstractAI;
            absAI.SetDestination(coord);
            var realAI = absAI.RealAI;
            if(realAI != null)
            {
                // pathfinder has some "optimizations" that need bypassing
                realAI.pathFinder.nextDestination = null;
                realAI.pathFinder.currentlyFollowingDestination = coord;
                realAI.pathFinder.AbortCurrentGenerationPathFinding();
                realAI.pathFinder.AssignNewDestination(coord);
            }
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
            var grabblt = this.Grabability(obj);
            if (grabblt == Player.ObjectGrabability.CantGrab)
            {
                return false;
            }
            if (grabblt == Player.ObjectGrabability.BigOneHand)
            {
                for (int i = 0; i < creature.grasps.Length; i++)
                {
                    if (creature.grasps[i] != null && this.Grabability(creature.grasps[i].grabbed) > Player.ObjectGrabability.OneHand)
                    {
                        return false;
                    }
                }
            }
            int bigthings = 0;
            for (int l = 0; l < creature.grasps.Length; l++)
            {
                if (creature.grasps[l] != null)
                {
                    if (creature.grasps[l].grabbed == obj)
                    {
                        return false;
                    }
                    if (this.Grabability(creature.grasps[l].grabbed) > Player.ObjectGrabability.OneHand)
                    {
                        bigthings++;
                    }
                }
            }
            return (bigthings <= 0 || grabblt <= Player.ObjectGrabability.BigOneHand);
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
                case Creature:
                    return Player.ObjectGrabability.CantGrab;
            }

            return Player.ObjectGrabability.OneHand;
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
                //throw;
            }
        }

        internal bool CanEat()
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

            // eat popcorn
            if (this.dontEatExternalFoodSourceCounter > 0)
            {
                this.dontEatExternalFoodSourceCounter--;
            }
            if (this.eatExternalFoodSourceCounter > 0)
            {
                this.eatExternalFoodSourceCounter--;
                if (this.eatExternalFoodSourceCounter < 1)
                {
                    if (creature.grasps[0]?.grabbed is SeedCob) creature.grasps[0].Release();
                    this.dontEatExternalFoodSourceCounter = 45;
                    creature.room.PlaySound(SoundID.Slugcat_Bite_Fly, creature.mainBodyChunk);
                }
            }

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

        internal virtual void ConsciousUpdate()
        {
            var room = creature.room;
            var chunks = creature.bodyChunks;
            var nc = chunks.Length;

            bool localTrace = Input.GetKey(KeyCode.L);

            if (onlineCreature.isMine)
            {
                GrabUpdate();
            }

            if (this.specialInput[0].direction != Vector2.zero)
            {
                LookImpl(creature.DangerPos + 500f * this.specialInput[0].direction);
            }

            // player direct into holes simplified equivalent
            // seems to not work too well for lizards because 3 chunks middle chunk causes stuckiness
            if ((input[0].x == 0 || input[0].y == 0) && input[0].x != input[0].y) // a straight direction
            {
                for (int n = 0; n < nc; n++)
                {
                    if (room.GetTile(chunks[n].pos + input[0].IntVec.ToVector2() * 40f).Terrain == Room.Tile.TerrainType.ShortcutEntrance
                        || room.GetTile(chunks[n].pos + input[0].IntVec.ToVector2() * 20f).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                    {
                        chunks[n].vel += (room.MiddleOfTile(chunks[n].pos + new Vector2(20f * (float)input[0].x, 20f * (float)input[0].y)) - chunks[n].pos) / 10f;
                        break;
                    }
                }
            }

            if (onlineCreature.isMine)
            {
                var mcd = onlineCreature.GetData<MeadowCreatureData>();
                var basecoord = CurrentPathfindingPosition;
                if (!lockInPlace && this.inputDir != Vector2.zero)
                {
                    if (specialInput[0].direction.magnitude < 0.2f)
                    {
                        LookImpl(creature.DangerPos + 200 * inputDir);
                    }
                    // todo have remote send us this instead of pathfinding for remote entities
                    if (FindDestination(basecoord, out var toPos, out float magnitude))
                    {
                        Moving(magnitude);
                        mcd.moveSpeed = magnitude;
                        if (toPos != creature.abstractCreature.abstractAI.destination)
                        {
                            if (localTrace) RainMeadow.Debug($"new destination {toPos.Tile}");
                            this.ForceAIDestination(toPos);
                            mcd.destination = toPos;
                        }
                    }
                    else
                    {
                        Resting();
                        mcd.moveSpeed = 0f;
                        if (basecoord != creature.abstractCreature.abstractAI.destination)
                        {
                            if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"resting at {basecoord.Tile}");
                            this.ForceAIDestination(basecoord);
                            mcd.destination = basecoord;
                        }
                    }
                }
                else
                {
                    Resting();
                    mcd.moveSpeed = 0f;
                    if (basecoord != creature.abstractCreature.abstractAI.destination)
                    {
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"resting at {basecoord.Tile}");
                        this.ForceAIDestination(basecoord);
                        mcd.destination = basecoord;
                    }
                }
            }
            else
            {
                var mcd = onlineCreature.GetData<MeadowCreatureData>();
                if (mcd.moveSpeed > 0f)
                {
                    Moving(mcd.moveSpeed);
                }
                else
                {
                    Resting();
                }
                if (mcd.destination != creature.abstractCreature.abstractAI.destination)
                {
                    this.ForceAIDestination(mcd.destination);
                }
            }

            lockInPlace = false;
        }


        internal void AIUpdate(ArtificialIntelligence ai)
        {
            if (creature.room?.Tiles != null && !ai.pathFinder.DoneMappingAccessibility)
                ai.pathFinder.accessibilityStepsPerFrame = creature.room.Tiles.Length; // faster, damn it. on entering a new room this needs to complete before it can pathfind
            else ai.pathFinder.accessibilityStepsPerFrame = 10;
            ai.pathFinder.Update(); // basic movement uses this
            ai.tracker.Update(); // creature looker uses this
            ai.timeInRoom++;
        }

        protected abstract void LookImpl(Vector2 pos);

        internal virtual bool FindDestination(WorldCoordinate basecoord, out WorldCoordinate toPos, out float magnitude)
        {
            var room = creature.room;
            var chunks = creature.bodyChunks;
            var nc = chunks.Length;

            var template = creature.Template;

            toPos = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(this.inputDir.normalized * 1.42f));
            magnitude = 0.5f;
            var previousAccessibility = room.aimap.getAItile(basecoord).acc;

            // prio 1: entering shortcut
            if (creature.enteringShortCut == null && creature.shortcutDelay < 1)
            {
                for (int i = 0; i < nc; i++)
                {
                    if (room.GetTile(chunks[i].pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                    {
                        var scdata = room.shortcutData(room.GetTilePosition(chunks[i].pos));
                        if (scdata.shortCutType != ShortcutData.Type.DeadEnd)
                        {
                            IntVector2 intVector = room.ShorcutEntranceHoleDirection(room.GetTilePosition(chunks[i].pos));
                            if (this.input[0].x == -intVector.x && this.input[0].y == -intVector.y)
                            {
                                RainMeadow.Debug("creature entering shortcut");
                                creature.enteringShortCut = new IntVector2?(room.GetTilePosition(chunks[i].pos));

                                if (scdata.shortCutType == ShortcutData.Type.NPCTransportation)
                                {
                                    var whackamoles = room.shortcuts.Where(s => s.shortCutType == ShortcutData.Type.NPCTransportation).ToList();
                                    var index = whackamoles.IndexOf(creature.room.shortcuts.FirstOrDefault(s => s.StartTile == scdata.StartTile));
                                    if (index > -1 && whackamoles.Count > 0)
                                    {
                                        var newindex = (index + 1) % whackamoles.Count;
                                        RainMeadow.Debug($"creature entered at {index} will exit at {newindex} mapped to {creature.NPCTransportationDestination}");
                                        creature.NPCTransportationDestination = whackamoles[newindex].startCoord;
                                        // needs to be set as destination as well otherwise might be overriden
                                        toPos = creature.NPCTransportationDestination;
                                        return true;
                                    }
                                    else
                                    {
                                        RainMeadow.Error("shortcut issue");
                                    }
                                }
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        protected abstract void Resting();
        protected abstract void Moving(float magnitude);
    }
}
