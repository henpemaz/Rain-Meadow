using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMeadow.Story.OnlineUIComponents
{
    public class ChatOverlay : Menu.Menu
    {
        public RainWorldGame game;
        public ChatOverlay chatOverlay;
        public ChatOverlay(ProcessManager manager, RainWorldGame game) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            this.game = game;

        }
    }
}
