using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow
{

    internal abstract class OnlinePlayerHudPart
    {
        public OnlinePlayerIndicator indicator;

        public Vector2 bodyPos;

        public Vector2 lastBodyPos;

        public Vector2 targetPos;

        public Vector2 lastTargetPos;

        public bool slatedForDeletion;

        public bool hidden;

        public bool lastHidden;

        public bool forceHide;

        public bool knownPos;

        public OnlinePlayerHudPart(OnlinePlayerIndicator indicator)
        {
            this.indicator = indicator;
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
