
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public interface IChatSubscriber
    {
        public bool Active{ get; }
        public void AddMessage(string user, string text);
    }
}
