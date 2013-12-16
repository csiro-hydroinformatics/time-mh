using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using TIME.Tools.Metaheuristics.SystemConfigurations;

namespace TIME.Metaheuristics.Parallel
{
    [Serializable]
    class MpiWorkPacket
    {
        public int Command;
        public MpiSysConfig Parameters;

        public MpiWorkPacket()
        {
            Command = SlaveActions.Nothing;
            Parameters = null;
        }

        public MpiWorkPacket(int command)
        {
            Command = command;
        }

        public MpiWorkPacket(int command, MpiSysConfig parameters)
        {
            Command = command;
            Parameters = parameters;
        }

    }
}
