using System.Collections.Generic;
using HUD;
using UnityEngine;

namespace RainMeadow
{
    public class OnlinePlayerIndicator : HudPart
    {

        public AbstractCreature abstractPlayer;

        public OnlinePlayerArrow playerArrow;

        public OnlineDeathBump deathBump;

        public OnlineAwayFromRoom offRoom;

        //public int playerNumber;

        public string playerName;

        public bool inShortcutLast;

        public bool inShortcut;

        public Color playerColor;

        public Vector2 camPos;

        public FContainer fContainer;

        public List<OnlinePlayerHudPart> parts;

        public bool addedDeathBumpThisSession;
        public StoryClientSettings clientSettings;

        public PlayerState PlayerState => abstractPlayer.state as PlayerState;

        public RoomCamera Camera => abstractPlayer.world.game.cameras[0];

        public bool PlayerRoomBeingViewed => abstractPlayer.Room == Camera.room?.abstractRoom;

        public Player RealizedPlayer => abstractPlayer.realizedCreature as Player;



        public OnlinePlayerIndicator(global::HUD.HUD hud, FContainer fContainer, AbstractCreature player, string name, Color bodyColor)
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

        public OnlinePlayerIndicator(HUD.HUD hud, FContainer fContainer, ClientSettings clientSettings) : base(hud)
        {
            this.clientSettings = clientSettings;
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
