using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMeadow.UI.Interfaces
{
    public interface IPLEASEUPDATEME
    {
        /// <summary>
        /// Basically makes you be able to manage ur updates with Tabs -> (tabs will update the class). 
        /// </summary>
        public bool IsHidden { get; set; }
    }
}
