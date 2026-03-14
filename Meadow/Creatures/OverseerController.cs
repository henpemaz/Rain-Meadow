using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{

    public class OverseerController : CreatureController
    {
        public Overseer overseer;
        public SpectateCursor? cursor;
        public OverseerController(Overseer creature, OnlineCreature oc, int playerNumber, MeadowAvatarData customization) : base(creature, oc, playerNumber, customization)
        {
            this.overseer = creature;
        }
        public OverseerController(Overseer creature, OnlineCreature oc, int playerNumber) : base(creature, oc, playerNumber)
        {
            this.overseer = creature;
        }
        public static void EnableOverseer()
        {
            On.OverseerAI.HoverScoreOfTile += OverseerAI_HoverScoreOfTile;
            On.OverseerAI.Update += OverseerAI_Update;
            On.Overseer.PlaceInRoom += Overseer_PlaceInRoom;
            IL.RoomCamera.GetCameraBestIndex += RoomCamera_GetCameraBestIndex;
            new Hook(
                typeof(OverseerGraphics).GetProperty("MainColor").GetGetMethod(),
                Overseer_BodyColor
            );
            On.OverseerAbstractAI.AbstractBehavior += OverseerAbstractAI_AbstractBehavior;
        }

        public static void OverseerAbstractAI_AbstractBehavior(On.OverseerAbstractAI.orig_AbstractBehavior orig, OverseerAbstractAI self, int time)
        {
            if (self.parent?.realizedCreature is not null)
            if (creatureControllers.TryGetValue(self.parent.realizedCreature, out var p) && p is OverseerController) return;
            orig(self, time);
        }

        public static Color Overseer_BodyColor(Func<OverseerGraphics, Color> orig, OverseerGraphics self)
        {
            if (self.overseer.abstractCreature.GetOnlineCreature() is OnlineCreature s)
            {
                if (s.TryGetData<MeadowAvatarData>(out var meadowAvatarData))
                {
                    return meadowAvatarData.skinData.baseColor ?? Color.white;
                }
                if (s.TryGetData<SlugcatCustomization>(out var custom))
                {
                    return custom.bodyColor;
                }
            }

            return orig(self);
        }

        public static void OverseerAI_Update(On.OverseerAI.orig_Update orig, OverseerAI self)
        {
            orig(self);
            if (creatureControllers.TryGetValue(self.overseer, out var p) && p is OverseerController controller)
            {
                controller.ConsciousUpdate();
                if (controller.cursor != null) self.lookAt = controller.cursor.pos;
            }
        }

        public static float OverseerAI_HoverScoreOfTile(On.OverseerAI.orig_HoverScoreOfTile orig, OverseerAI self, IntVector2 testTile)
        {
            float score = orig(self, testTile);
            if (creatureControllers.TryGetValue(self.overseer, out var p) && p is OverseerController controller)
            {
                if (controller.cursor != null)
                {
                    score += Mathf.Max(0f, Vector2.Distance(self.overseer.room.MiddleOfTile(testTile), controller.cursor.pos) - 250f) * 30f;
                }
            }

            return score;
        }



        public static void RoomCamera_GetCameraBestIndex(ILContext ctx)
        {
            try
            {
                ILCursor cursor = new(ctx);
                cursor.GotoNext(MoveType.After, x => x.MatchStloc(4));
                cursor.MoveBeforeLabels();
                cursor.Emit(OpCodes.Ldloc_0);
                cursor.Emit(OpCodes.Ldloca, 3);
                cursor.Emit(OpCodes.Ldloca, 4);
                cursor.EmitDelegate((Creature creature, ref Vector2 testPos, ref Vector2 testPos2) =>
                {
                    if (creature is Overseer overseer && 
                        creatureControllers.TryGetValue(overseer, out var p) && p is OverseerController controller)
                    {
                        if (controller.cursor != null)
                        {
                            testPos = controller.cursor.pos;
                            testPos2 = controller.cursor.pos;
                        }
                    }
                });
            }
            catch (Exception except)
            {
                RainMeadow.Error(except);
            }
        }

        public static void Overseer_PlaceInRoom(On.Overseer.orig_PlaceInRoom orig, Overseer self, Room room)
        {
            orig(self, room);
            if (creatureControllers.TryGetValue(self, out var p) && p is OverseerController controller)
            {
                if (controller.cursor is null || controller.cursor.room != room)
                {
                    room.game.cameras[0].MoveCamera(room, -1);
                    controller.AddCursor();
                }
            }
        }

        public void AddCursor()
        {
            RainMeadow.DebugMe();
            Vector2 targetPos = overseer.mainBodyChunk.pos;
            // int camerapos = room.CameraViewingPoint(targetPos);
            // if (camerapos != -1) targetPos = room.cameraPositions[camerapos];
            cursor = new SpectateCursor(overseer, playerNumber, targetPos); 
            cursor.mouseMode = true;
            overseer.room.AddObject(cursor);
        }

        protected override void PointImpl(Vector2 dir)
        {
            base.PointImpl(dir);
        }

        public override void ConsciousUpdate() { }
        protected override void LookImpl(Vector2 pos) => throw new System.NotImplementedException();
        protected override void Moving(float magnitude) => throw new System.NotImplementedException();
        protected override void OnCall() => throw new System.NotImplementedException();
        protected override void Resting() => throw new System.NotImplementedException();


        // Token: 0x02000C0F RID: 3087
        public class SpectateCursor : UpdatableAndDeletable, IDrawable
        {
            public readonly Overseer overseer;
            public Vector2 ScreenPos => this.pos - this.room.game.cameras[0].pos;
            public bool OverseerActive => overseer.room == this.room && overseer.mode != Overseer.Mode.Zipping;

            public Vector2 OverseerEyePos(float timeStacker)
            {
                if (overseer.graphicsModule is OverseerGraphics gr && overseer.room is not null) return gr.DrawPosOfSegment(0f, timeStacker);
                return Vector2.Lerp(overseer.mainBodyChunk.lastPos, overseer.mainBodyChunk.pos, timeStacker);
            }

            public SpectateCursor(Overseer overseer, int playerNumber, Vector2 initPos)
            {
                this.playerNumber = playerNumber;
                this.overseer = overseer;
                this.pos = initPos;
                this.lastPos = initPos;
                this.lastHomePos = initPos;
                this.homePos = initPos;
                this.rotations = new float[5];
                this.rotations[0] = 1f;
                this.input = new Player.InputPackage[2];
            }

            public void NewRotation(float to, float goalSpeed)
            {
                if (this.rotations[0] < 1f || this.rotations[1] < 1f || to == this.rotations[3])
                {
                    return;
                }
                this.rotations[0] = 0f;
                this.rotations[1] = 0f;
                this.rotations[2] = this.rotations[3];
                this.rotations[3] = to;
                this.rotations[4] = Mathf.Lerp(1f / (Mathf.Abs(this.rotations[2] - this.rotations[3]) * 60f), goalSpeed, 0.5f);
            }

            public float GetRotation(float timeStacker)
            {
                return Mathf.Lerp(this.rotations[2], this.rotations[3], Custom.SCurve(Mathf.Lerp(this.rotations[1], this.rotations[0], timeStacker), 0.65f));
            }

            public void Bump(bool redBump)
            {
                this.bump = 1f;
                this.red = redBump;
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                this.counter++;
                this.lastHomeIn = this.homeIn;
                this.lastHomePos = this.homePos;
                this.lastPos = this.pos;
                this.lastSquare = this.square;
                this.lastMobile = this.mobile;
                this.lastMenuFac = this.menuFac;
                this.lastBump = this.bump;
                this.lastQuality = this.quality;
                this.lastPushAroundPos = this.pushAroundPos;
                this.pushAroundPos *= 0.8f;
                if (overseer.extended > 0f)
                {
                    this.pushAroundPos += (overseer.firstChunk.pos - overseer.firstChunk.lastPos) * overseer.extended;
                }
                if (this.OverseerActive)
                {
                    this.quality = Mathf.Min(1f, this.quality + 0.05f);
                }
                else
                {
                    this.quality = Mathf.Max(0f, this.quality - 1f / Mathf.Lerp(30f, 80f, this.quality));
                }

                if (Mathf.Approximately(quality, 0f) && overseer.room != this.room)
                {
                    Destroy();
                    return;    
                }

                if (UnityEngine.Random.value < 0.1f)
                {
                    this.quality = Mathf.Min(this.quality, Mathf.InverseLerp(600f, 400f, Vector2.Distance(this.OverseerEyePos(1f), this.pos)));
                }
                this.rotations[1] = this.rotations[0];
                this.rotations[0] = Mathf.Min(1f, this.rotations[0] + this.rotations[4]);
                this.menuFac = Custom.LerpAndTick(this.menuFac, this.menuMode ? 1f : 0f, 0.07f, 0.1f);
                this.bump = Mathf.Max(0f, this.bump - 0.033333335f);
                if (this.bump == 0f && this.lastBump == 0f)
                {
                    this.red = false;
                }

                var game = (RainWorldGame)RWCustom.Custom.rainWorld.processManager.currentMainLoop;
                if (this.mouseMode)
                {
                    this.pos += this.vel;
                    this.vel *= 0.6f * (1f - this.homeIn);
                    Vector2 targetpos = (Vector2)Futile.mousePosition + game.cameras[0].pos;
                    Vector2 diff = targetpos - pos;
                    this.vel += Vector2.ClampMagnitude(diff, 5f) * 2f;
                    this.pos += Vector2.ClampMagnitude(diff, 5f);

                    
                }
                else
                {
                    this.pos += this.vel;
                    this.vel *= 0.6f * (1f - this.homeIn);

                    Vector2 inputdirection = this.input[0].analogueDir;
                    if (inputdirection.CloseEnough(Vector2.zero, 0.1f)  && inputdirection.y == 0f && (this.input[0].x != 0 || this.input[0].y != 0))
                    {
                        inputdirection = Custom.DirVec(new Vector2(0f, 0f), new Vector2((float)this.input[0].x, (float)this.input[0].y));
                    }
                    this.vel += inputdirection * 2f;
                    this.pos += inputdirection * 5f;
                    this.mobile = Custom.LerpAndTick(this.mobile, inputdirection.magnitude, 0.02f, 0.033333335f);
                }


                for (int i = this.input.Length - 1; i > 0; i--)
                {
                    this.input[i] = this.input[i - 1];
                }
                if (this.mouseMode)
                {
                    this.input[0] = new Player.InputPackage(false, Options.ControlSetup.Preset.KeyboardSinglePlayer, 0, 0, false, Input.GetKey(KeyCode.Mouse0), Input.GetKey(KeyCode.Mouse1), false, false);
                }
                else
                {
                    this.input[0] = RWInput.PlayerInput(this.playerNumber);
                }


                float num = 0f;
                bool clickedSomething = false;

                // if (this.input[0].pckp && !this.input[1].pckp)
                // {
                //     //TODO: Selectables.
                //     this.menuMode = !this.menuMode;
                //     clickedSomething = true;
                //     // this.room.PlaySound(this.menuMode ? SoundID.SANDBOX_Overseer_Switch_To_Menu_Mode : SoundID.SANDBOX_Overseer_Switch_Out_Of_Menu_Mode, 0f, 1f, 1f);
                //     // if (this.menuMode && this.mouseMode)
                //     // {
                //     //     this.editor.overlay.sandboxEditorSelector.MouseCursorEnterMenuMode(this);
                //     // }
                // }
                // if (this.menuMode)
                // {
                //     if (this.input[0].x != 0 && this.input[0].x != this.input[1].x)
                //     {
                //         this.menuCursor.Move(this.input[0].x, 0);
                //     }
                //     if (this.input[0].y != 0 && this.input[0].y != this.input[1].y)
                //     {
                //         this.menuCursor.Move(0, this.input[0].y);
                //     }
                //     if (!this.mouseMode && this.input[0].thrw != this.input[1].thrw)
                //     {
                //         if (this.input[0].thrw)
                //         {
                //             this.menuCursor.clickOnRelease = true;
                //         }
                //         else if (this.menuCursor.clickOnRelease)
                //         {
                //             this.menuCursor.Click();
                //         }
                //     }
                //     this.mobile = Custom.LerpAndTick(this.mobile, 0f, 0.02f, 0.033333335f);
                // }
                // else
                // {
                //     Vector2 vector = this.input[0].analogueDir;
                //     if (vector.x == 0f && vector.y == 0f && (this.input[0].x != 0 || this.input[0].y != 0))
                //     {
                //         vector = Custom.DirVec(new Vector2(0f, 0f), new Vector2((float)this.input[0].x, (float)this.input[0].y));
                //     }
                //     this.vel += vector * 2f;
                //     this.pos += vector * 5f;
                //     this.mobile = Custom.LerpAndTick(this.mobile, vector.magnitude, 0.02f, 0.033333335f);
                //     if (!this.input[0].jmp && this.input[1].jmp)
                //     {
                //         if (this.dragIcon != null)
                //         {
                //             this.pos = this.dragIcon.pos;
                //             this.homeIn = 0f;
                //             flag = true;
                //             this.room.PlaySound(SoundID.SANDBOX_Remove_Item, this.pos, 1f, 1f);
                //             this.editor.RemoveIcon(this.dragIcon, true);
                //             this.dragIcon = null;
                //             this.Bump(true);
                //             this.square *= 0.5f;
                //         }
                //         else if (this.homeInIcon != null && this.homeIn > 0.65f)
                //         {
                //             this.pos = this.homeInIcon.pos;
                //             this.homeIn = 0f;
                //             flag = true;
                //             this.room.PlaySound(SoundID.SANDBOX_Remove_Item, this.pos, 1f, 1f);
                //             this.editor.RemoveIcon(this.homeInIcon, true);
                //             this.homeInIcon = null;
                //             this.Bump(true);
                //             this.square *= 0.5f;
                //         }
                //     }
                // }
                // this.pos.x = Mathf.Clamp(this.pos.x, this.room.game.cameras[0].pos.x, this.room.game.cameras[0].pos.x + this.room.game.cameras[0].sSize.x);
                // this.pos.y = Mathf.Clamp(this.pos.y, this.room.game.cameras[0].pos.y, this.room.game.cameras[0].pos.y + this.room.game.cameras[0].sSize.y);
                // this.square = Custom.LerpAndTick(this.square, (this.homeInIcon != null || this.dragIcon != null) ? 1f : 0f, 0.03f, 0.025f);
                // if (this.dragIcon != null)
                // {
                //     this.dragIcon.pos = Vector2.Lerp(this.dragIcon.pos, this.pos + this.dragOffset + this.pushAroundPos * 0.5f, 0.8f);
                //     this.rotations[0] = 1f;
                //     if (!this.mouseMode)
                //     {
                //         this.homeIn = Mathf.Max(0f, this.homeIn - 0.033333335f);
                //     }
                //     this.homeInIcon = this.dragIcon;
                //     this.homePos = this.dragIcon.pos;
                //     if (!this.menuMode && this.input[0].thrw && !this.input[1].thrw)
                //     {
                //         flag = true;
                //         this.room.PlaySound(SoundID.SANDBOX_Release_Item, this.pos, 1f, 1f);
                //         this.dragIcon = null;
                //     }
                //     if (this.dragIcon != null && this.mouseMode && !this.input[0].thrw)
                //     {
                //         this.room.PlaySound(SoundID.SANDBOX_Release_Item, this.pos, 1f, 1f);
                //         this.editor.overlay.trashBin.IconReleased(this.dragIcon);
                //         this.dragIcon = null;
                //     }
                // }
                // else
                // {
                //     if (UnityEngine.Random.value < 0.022222223f)
                //     {
                //         this.rotat += UnityEngine.Random.Range(1, UnityEngine.Random.Range(1, 4)) * ((UnityEngine.Random.value < 0.5f) ? -1 : 1);
                //         this.NewRotation((float)this.rotat / 4f, 1f / Mathf.Lerp(30f, 80f, UnityEngine.Random.value));
                //     }
                //     if (this.menuMode)
                //     {
                //         this.homeInIcon = null;
                //     }
                //     else
                //     {
                //         SandboxEditor.PlacedIcon placedIcon = null;
                //         float dst = 50f;
                //         for (int j = 0; j < this.editor.icons.Count; j++)
                //         {
                //             if (Custom.DistLess(this.pos, this.editor.icons[j].pos, dst) && this.editor.icons[j].DraggedBy == null)
                //             {
                //                 dst = Vector2.Distance(this.pos, this.editor.icons[j].pos);
                //                 placedIcon = this.editor.icons[j];
                //             }
                //         }
                //         if (placedIcon != this.homeInIcon)
                //         {
                //             if (this.homeInIcon != null)
                //             {
                //                 this.lingerFac = 1f;
                //             }
                //             if (placedIcon != null)
                //             {
                //                 placedIcon.Flash();
                //             }
                //             this.homeInIcon = placedIcon;
                //         }
                //     }
                //     if (this.homeInIcon != null)
                //     {
                //         this.homeIn = Custom.LerpAndTick(this.homeIn, 1f, 0.03f, 1f / (30f + num * 120f));
                //     }
                //     else
                //     {
                //         this.homeIn = Custom.LerpAndTick(this.homeIn, 0f, 0.03f, 0.033333335f);
                //     }
                //     if (this.homeInIcon != null)
                //     {
                //         this.homeInIcon.SetFlashValue((0.5f + 0.5f * Mathf.Sin((float)this.counter / 8f)) * this.homeIn);
                //         this.homeInIcon.setDisplace += Vector2.ClampMagnitude(this.pos - this.homeInIcon.pos, 30f) / 15f * this.homeIn;
                //         this.homePos = Vector2.Lerp(this.homePos, this.homeInIcon.DrawPos(1f), this.homeIn * (1f - this.lingerFac));
                //         if (!this.menuMode && this.input[0].thrw && !this.input[1].thrw && this.homeInIcon.DraggedBy == null)
                //         {
                //             this.dragIcon = this.homeInIcon;
                //             flag = true;
                //             this.room.PlaySound(SoundID.SANDBOX_Grab_Item, this.pos, 1f, 1f);
                //             if (this.mouseMode)
                //             {
                //                 this.dragOffset = this.dragIcon.pos - this.pos;
                //                 this.homeIn = 1f;
                //             }
                //             else
                //             {
                //                 this.dragOffset *= 0f;
                //                 this.pos = this.DrawPos(1f);
                //                 this.homeIn = 0f;
                //                 this.homePos = this.pos;
                //             }
                //         }
                //     }
                //     else
                //     {
                //         this.homePos = Vector2.Lerp(this.pos, this.homePos, Mathf.Max(this.homeIn, this.lingerFac));
                //     }
                // }
                // this.lingerFac = Mathf.Max(0f, this.lingerFac - 0.05f);
                // if (!this.menuMode && !clickedSomething && ((this.input[0].thrw && !this.input[1].thrw) || (this.input[0].jmp && !this.input[1].jmp)))
                // {
                //     this.room.PlaySound(SoundID.SANDBOX_Nothing_Click, this.pos, 1f, 1f);
                // }
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[10];
                sLeaser.sprites[0] = new FSprite("Futile_White", true);
                sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["FlatLight"];
                sLeaser.sprites[0].color = Color.black;
                sLeaser.sprites[1] = new FSprite("Futile_White", true);
                sLeaser.sprites[1].shader = rCam.game.rainWorld.Shaders["LightSource"];
                for (int i = 0; i < 8; i++)
                {
                    sLeaser.sprites[2 + i] = new FSprite("pixel", true);
                    sLeaser.sprites[2 + i].scaleX = 2f;
                    sLeaser.sprites[2 + i].scaleY = 15f;
                    sLeaser.sprites[2 + i].anchorY = 0f;
                    sLeaser.sprites[2 + i].shader = rCam.game.rainWorld.Shaders["Hologram"];
                }
                this.AddToContainer(sLeaser, rCam, null);
            }

            public Vector2 DrawPos(float timeStacker)
            {
                return Vector2.Lerp(Vector2.Lerp(this.lastPos, this.pos, timeStacker), Vector2.Lerp(this.lastHomePos, this.homePos, timeStacker), Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(this.lastHomeIn, this.homeIn, timeStacker)), 2f) * 0.95f);
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                Vector2 vector = this.OverseerEyePos(timeStacker);
                Vector2 a = Vector2.Lerp(this.lastPushAroundPos, this.pushAroundPos, timeStacker);
                float num = Mathf.Pow(1f - Mathf.Lerp(this.lastQuality, this.quality, timeStacker), 1.5f);
                Vector2 vector2 = this.DrawPos(timeStacker) + a * Mathf.Lerp(0.5f, 1f, num);
                float num2 = 0.5f + 0.5f * Mathf.Sin(((float)this.counter + timeStacker) / 8f);
                num2 = Mathf.Lerp(num2, UnityEngine.Random.value, num * 0.2f);
                float num3 = Mathf.InverseLerp(0.25f, 0.75f, Mathf.Lerp(this.lastSquare, this.square, timeStacker));
                float num4 = Mathf.Lerp(this.lastMobile, this.mobile, timeStacker);
                float num5 = Mathf.Lerp(this.lastMenuFac, this.menuFac, timeStacker);
                num3 = Mathf.Max(num3, num5);
                float num6 = Mathf.Lerp(this.lastBump, this.bump, timeStacker);
                float num7 = 0f;
                float num8 = 20f + num7 * (Mathf.Lerp(-10f, -5f, num2 * (1f - num4)) + 10f * num5);
                num8 += num6 * 10f;
                if (this.input[0].thrw || (this.input[0].jmp && !this.menuMode))
                {
                    num8 -= 4f;
                }
                float num9 = this.GetRotation(timeStacker) * 360f + 45f * Mathf.Lerp(this.lastSquare, this.square, timeStacker) + 45f * num7;
                float num10 = Mathf.Lerp(Mathf.Lerp(45f, 75f, num7), 90f, num5);
                num8 -= 10f * num5;
                Color color = Color.white;
                if (overseer.graphicsModule is OverseerGraphics gr) color = gr.MainColor;
                if (this.input[0].jmp && !this.menuMode)
                {
                    color = Color.Lerp(color, Color.red, UnityEngine.Random.value);
                }
                else if (this.red)
                {
                    color = Color.Lerp(color, Color.red, num6);
                }
                sLeaser.sprites[0].x = vector2.x - camPos.x;
                sLeaser.sprites[0].y = vector2.y - camPos.y;
                sLeaser.sprites[0].scale = (150f + num8 * (1f - UnityEngine.Random.value * num)) / 8f;
                sLeaser.sprites[0].alpha = 0.3f * (1f - UnityEngine.Random.value * num);
                sLeaser.sprites[1].x = vector2.x - camPos.x;
                sLeaser.sprites[1].y = vector2.y - camPos.y;
                sLeaser.sprites[1].scale = (150f + num8 * (1f - UnityEngine.Random.value * num) * 2f) / 8f;
                sLeaser.sprites[1].alpha = 0.3f * (1f - UnityEngine.Random.value * num);
                sLeaser.sprites[1].color = color;
                float num11 = Vector2.Distance(vector, vector2);
                for (int i = 0; i < 8; i++)
                {
                    float num12 = Mathf.InverseLerp(0f, 4f, (float)(i / 2)) * 360f + num9;
                    Vector2 vector3 = vector2 + Custom.DegToVec(num12) * (num8 + 15f);
                    Vector2 vector4 = vector2 + Vector2.Lerp(Custom.DegToVec(num12) * num8, Custom.DegToVec(num12) * (num8 + 15f) - Custom.DegToVec(num12 + ((i % 2 == 0) ? -1f : 1f) * num10) * Mathf.Lerp(10f, 8f, num7), num3);
                    vector3 += this.pushAroundPos * (0.5f * Mathf.Pow(Mathf.InverseLerp(num11 + 40f, num11 - 40f, Vector2.Distance(vector, vector3)), 2f) + 0.5f * UnityEngine.Random.value * num);
                    vector4 += this.pushAroundPos * (0.5f * Mathf.Pow(Mathf.InverseLerp(num11 + 40f, num11 - 40f, Vector2.Distance(vector, vector4)), 2f) + 0.5f * UnityEngine.Random.value * num);
                    if (UnityEngine.Random.value < num)
                    {
                        if (UnityEngine.Random.value < 0.5f)
                        {
                            vector3 += Custom.RNV() * UnityEngine.Random.value * 20f * num;
                        }
                        else
                        {
                            vector4 += Custom.RNV() * UnityEngine.Random.value * 20f * num;
                        }
                    }
                    if (UnityEngine.Random.value < 1f / Mathf.Lerp(40f, 10f, num))
                    {
                        sLeaser.sprites[2 + i].scaleX = 1f;
                        sLeaser.sprites[2 + i].alpha = Mathf.Pow(UnityEngine.Random.value, Mathf.Lerp(2f, 0.2f, num));
                        if (UnityEngine.Random.value < 0.5f)
                        {
                            vector3 = Vector2.Lerp(vector4, vector, UnityEngine.Random.value * UnityEngine.Random.value);
                        }
                        else
                        {
                            vector4 = Vector2.Lerp(vector3, vector, UnityEngine.Random.value * UnityEngine.Random.value);
                        }
                    }
                    else
                    {
                        sLeaser.sprites[2 + i].scaleX = 2f + num6;
                        sLeaser.sprites[2 + i].alpha = ((UnityEngine.Random.value < num) ? (1f - UnityEngine.Random.value * num) : 1f);
                    }
                    sLeaser.sprites[2 + i].x = vector3.x - camPos.x;
                    sLeaser.sprites[2 + i].y = vector3.y - camPos.y;
                    sLeaser.sprites[2 + i].rotation = Custom.AimFromOneVectorToAnother(vector3, vector4);
                    sLeaser.sprites[2 + i].scaleY = Vector2.Distance(vector3, vector4);
                    sLeaser.sprites[2 + i].color = color;
                }
                if (base.slatedForDeletetion)
                {
                    sLeaser.CleanSpritesAndRemove();
                }
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                if (newContatiner == null)
                {
                    newContatiner = rCam.ReturnFContainer("HUD2");
                }
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    sLeaser.sprites[i].RemoveFromContainer();
                    if (i < 2)
                    {
                        rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[i]);
                    }
                    else
                    {
                        newContatiner.AddChild(sLeaser.sprites[i]);
                    }
                }
            }
            public int playerNumber;
            public Vector2 pos;
            public Vector2 lastPos;
            public Vector2 homePos;
            public Vector2 lastHomePos;
            public Vector2 vel;
            private Vector2 lastPushAroundPos;
            private Vector2 pushAroundPos;
            private float square;
            private float lastSquare;
            private float homeIn;
            private float lastHomeIn;
            private float lingerFac;
            private float lastMenuFac;
            private float menuFac;
            private float lastBump;
            private float bump;
            private float lastQuality;
            private float quality;
            private Vector2 dragOffset;
            public Player.InputPackage[] input;
            public float[] rotations;
            private int rotat;
            private int counter;
            private float mobile;
            private float lastMobile;
            public bool menuMode;
            public bool mouseMode;
            private bool red;
        }

    }
}