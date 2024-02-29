using UnityEngine;

namespace RainMeadow
{
    public abstract class OnlinePlayerHudPart
    {
        public PlayerSpecificOnlineHud owner;
        public Vector2 pos;
        public Vector2 lastPos;
        public bool slatedForDeletion;

        public OnlinePlayerHudPart(PlayerSpecificOnlineHud owner)
        {
            this.owner = owner;
        }

        public virtual void Update()
        {
            this.lastPos = this.pos;
        }

        public virtual void Draw(float timeStacker)
        {

        }

        public virtual void ClearSprites()
        {

        }
    }
}
