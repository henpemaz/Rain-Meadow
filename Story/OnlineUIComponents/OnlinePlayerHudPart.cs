using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.Story.OnlineUIComponents
{

    public abstract partial class OnlinePlayerHudPart
    {
        public OnlinePlayerSpecificHud jollyHud;

        public Vector2 bodyPos;

        public Vector2 lastBodyPos;

        public Vector2 targetPos;

        public Vector2 lastTargetPos;

        public bool slatedForDeletion;

        public bool hidden;

        public bool lastHidden;

        public bool forceHide;

        public bool knownPos;

        public OnlinePlayerHudPart(OnlinePlayerSpecificHud jollyHud)
        {
            this.jollyHud = jollyHud;
        }

        public virtual void Update()
        {
        }

        public virtual void Draw(float timeStacker)
        {
        }

        public virtual void ClearSprites()
        {
        }
    }
}
