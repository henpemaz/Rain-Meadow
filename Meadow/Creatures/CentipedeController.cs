using System;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class CentipedeController : CreatureController
    {
        public Centipede centipede => (Centipede)creature;
        public CentipedeController(Centipede creature, OnlineCreature oc, int playerNumber, MeadowAvatarData customization)
            : base(creature, oc, playerNumber, customization)
        {
            
        }

        public static void Enable()
        {
            On.Centipede.Update += Centipede_Update; // input, sync, player things
            On.Centipede.Act += Centipede_Act; // movement
        }

        public override void ConsciousUpdate()
        {
            Vector2 direction = new Vector2(input[0].x, input[0].y);
            int headIndex = 0;
            int lastGrounded = -1;

    

            for (int i = 0; i < centipede.bodyChunks.Length; i++)
            {
                if (centipede.AccessibleTile(centipede.room.GetTilePosition(centipede.bodyChunks[i].pos) + new IntVector2(input[0].x, input[0].y)))
                {
                    lastGrounded = i;
                }

                float groundX;
                if (lastGrounded == -1)
                {
                    groundX = 1.0f;
                }
                else
                {
                    int offGroundChunks = Math.Min(8, centipede.bodyChunks.Length - 1);        
                    groundX = Mathf.Clamp01(Mathf.Min(Mathf.Abs(lastGrounded - i), offGroundChunks)/(float)offGroundChunks);

                    centipede.bodyChunks[i].vel *= 0.7f*(1f-groundX);
                    centipede.bodyChunks[i].vel.y += centipede.gravity*Mathf.Pow(1f-groundX, 0.2f);
                }                

                if (i == centipede.bodyChunks.Length - 1)
                {
                    centipede.bodyChunks[i].vel += direction * 10f *(0.2f + Mathf.Pow(1f-groundX, 0.2f)*0.8f);
                }
                else
                {
                    centipede.bodyChunks[i].vel += direction.magnitude * 
                        Custom.DirVec(centipede.bodyChunks[i].pos, centipede.bodyChunks[i + 1].pos) * 0.5f *(0.2f + Mathf.Pow(1f-groundX, 0.2f)*0.8f);
                }
            }


            base.ConsciousUpdate();

        }

        public static void Centipede_Update(On.Centipede.orig_Update orig, Centipede self, bool eu)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.Update(eu);
            }
            orig(self, eu);
        }

        // inputs and stuff
        // player consious update
        public static void Centipede_Act(On.Centipede.orig_Act orig, Centipede self)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.ConsciousUpdate();
                return;
            }

            orig(self);
        }

        
        protected override void LookImpl(Vector2 pos)
        {
        
        }

        protected override void Moving(float magnitude)
        {
            
        }

        protected override void OnCall()
        {
            
        }

        protected override void Resting()
        {
            
        }
    }
}