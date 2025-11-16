using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using UnityEngine;

namespace RainMeadow
{

    public static class GameplayExtensions
    {
        public static bool FriendlyFireSafetyCandidate(this Creature creature, Creature? friend)
        {
            if (creature.abstractCreature.GetOnlineCreature() is not OnlineCreature oc)
            {
                return false;
            }

            if (!oc.isAvatar)
            {
                return false;
            }

            if (RainMeadow.isArenaMode(out var arena))
            {
                if (creature.room.game.IsArenaSession && creature.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.spearsHitPlayers == false)
                {
                    return true; // you are a safety candidate
                }

                if (friend is not null)
                {
                    if (TeamBattleMode.isTeamBattleMode(arena, out _))
                    {
                        return ArenaHelpers.CheckSameTeam(oc.owner, friend.abstractCreature.GetOnlineCreature()?.owner, creature, friend);
                    }
                }

                if (arena.countdownInitiatedHoldFire) return true;
            }

            if (RainMeadow.isStoryMode(out var story) && friend is Player)
            {
                return !story.friendlyFire;
            }
            return false;
        }
    }
    public static class GameplayOverrides
    {
        public static void StopPlayerMovement(Player p)
        {
            if (p != null)
            {
                p.input[0].x = 0;
                p.input[0].y = 0;
                p.input[0].analogueDir *= 0f;
                p.input[0].jmp = false;
                p.input[0].thrw = false;
                p.input[0].pckp = false;
                p.input[0].mp = false;
                p.input[0].spec = false;
            } else
            {
                RainMeadow.Debug("Player is null while trying to stop movement");
            }
        }


        public static void HoldFire(Player p)
        {
            p.input[0].thrw = false;


        }

        public static void StopSpecialSkill(Player p)
        {
            if (p.wantToJump > 0 && p.input[0].pckp)
            {
                p.input[0].pckp = false;
            }

        }

        public static float MoveMenuItemFromYInput(float y)
        {
            var controller = RWCustom.Custom.rainWorld.options.controls[0].GetActiveController();
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (controller is Rewired.Joystick js) scrollInput -= js.GetAxis(3) * -1;
            scrollInput += (ChatHud.isLogToggled == false && Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) ? -1f : 0f;
            scrollInput += (ChatHud.isLogToggled == false && Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) ? 1f : 0f;
            scrollInput = Mathf.Clamp(scrollInput, -1.0f, 1.0f);

            return y + scrollInput * RainMeadow.rainMeadowOptions.ScrollSpeed.Value;
        }

    }
}
