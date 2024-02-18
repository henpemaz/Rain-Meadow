using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow
{

    public abstract partial class JollyPart
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

        public JollyPart(OnlinePlayerSpecificHud jollyHud)
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
