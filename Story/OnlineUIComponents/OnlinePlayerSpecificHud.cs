#region Assembly Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\lol\Downloads\stripped-assembly-csharp.dll
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HUD;
using RWCustom;
using UnityEngine;
using RainMeadow.Story.OnlineUIComponents;

namespace RainMeadow
{
    public partial class OnlinePlayerSpecificHud : HudPart
    {

        public AbstractCreature abstractPlayer;

        public OnlinePlayerArrow playerArrow;

        public OnlineDeathBump deathBump;

        public OnlineAwayFromRoom offRoom;

        public int playerNumber;

        public string playerName;

        public bool inShortcutLast;

        public bool inShortcut;

        public Color playerColor;

        public Vector2 camPos;

        public FContainer fContainer;

        public List<OnlinePlayerHudPart> parts;

        public bool addedDeathBumpThisSession;

        public PlayerState PlayerState => abstractPlayer.state as PlayerState;

        public RoomCamera Camera => abstractPlayer.world.game.cameras[0];

        public bool PlayerRoomBeingViewed => abstractPlayer.Room == Camera.room?.abstractRoom;

        public Player RealizedPlayer => abstractPlayer.realizedCreature as Player;



        public OnlinePlayerSpecificHud(global::HUD.HUD hud, FContainer fContainer, AbstractCreature player, string name, Color bodyColor)
            : base(hud)
        {

            abstractPlayer = player;
            playerName = name;
            playerNumber = PlayerState.playerNumber;
            inShortcut = false;
            inShortcutLast = inShortcut;
            playerColor = bodyColor;
            this.fContainer = fContainer;
            parts = new List<OnlinePlayerHudPart>();
            playerArrow = new OnlinePlayerArrow(this, playerName);
            parts.Add(playerArrow);
            /*            offRoom = new JollyOffRoom(this); // TODO: Offroom
                        parts.Add(offRoom);*/
            addedDeathBumpThisSession = false;

        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            for (int i = 0; i < parts.Count; i++)
            {
                parts[i].ClearSprites();
            }
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            for (int i = 0; i < parts.Count; i++)
            {
                parts[i].Draw(timeStacker);
            }
        }

        public void AddDeathBump()
        {
            if (PlayerRoomBeingViewed && abstractPlayer.realizedCreature != null)
            {
                deathBump = new OnlineDeathBump(this);
                parts.Add(deathBump);
            }
            else
            {
                hud.PlaySound(SoundID.UI_Multiplayer_Player_Dead_A);
                hud.PlaySound(SoundID.UI_Multiplayer_Player_Dead_B);
            }

            addedDeathBumpThisSession = true;
        }

        public override void Update()
        {
            base.Update();
            var players = OnlineManager.lobby.playerAvatars
            .Where(avatar => avatar.type != (byte)OnlineEntity.EntityId.IdType.none)
            .Select(avatar => avatar.FindEntity(true))
            .OfType<OnlinePhysicalObject>()
            .Select(opo => opo.apo as AbstractCreature)
            .Zip(OnlineManager.players, (creature, player) => new { AbstractCreature = creature, PlayerName = player })
            .ToList();


            if (RainMeadow.playersWithArrows.Count != players.Count)
            {

                RainMeadow.Debug("Adding extra HUD to players who just joined");
                for (int i = 0; i < players.Count; i++)
                {

                    if (!RainMeadow.playersWithArrows.Contains(players[i].PlayerName.id.name))
                    {

                        OnlinePlayerSpecificHud otherPlayerJollyHud = new OnlinePlayerSpecificHud(this.hud, this.fContainer, players[i].AbstractCreature, "", Color.white);
                        this.hud.parts.Add(otherPlayerJollyHud);
                        RainMeadow.playersWithArrows.Add(players[i].PlayerName.id.name);


                    }
                }
            }

            inShortcutLast = inShortcut;
            camPos = Camera.pos;
            if (RealizedPlayer != null)
            {
                inShortcut = RealizedPlayer.inShortcut;
            }

            if ((PlayerState.dead || PlayerState.permaDead) && !addedDeathBumpThisSession)
            {
                AddDeathBump();
            }

            for (int num = parts.Count - 1; num >= 0; num--)
            {
                if (parts[num].slatedForDeletion)
                {
                    if (parts[num] == playerArrow)
                    {
                        playerArrow = null;
                    }
                    else if (parts[num] == offRoom)
                    {
                        offRoom = null;
                    }
                    else if (parts[num] == deathBump)
                    {
                        deathBump = null;
                    }

                    parts[num].ClearSprites();
                    parts.RemoveAt(num);
                }
                else if (Camera.InCutscene && Camera.cutsceneType == RoomCamera.CameraCutsceneType.VoidSea)
                {
                    parts[num].slatedForDeletion = true;
                }
                else
                {
                    parts[num].forceHide = (Camera.InCutscene && Camera.cutsceneType == RoomCamera.CameraCutsceneType.EndingOE) || Camera.cutsceneType == RoomCamera.CameraCutsceneType.HunterStart;
                    parts[num].Update();
                }
            }
        }
    }
}
