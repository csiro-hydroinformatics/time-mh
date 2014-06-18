using System;
using System.Collections.Generic;
using TIME.Tools.Metaheuristics.Persistence;
using TIME.Tools.Metaheuristics.Persistence.Gridded;

namespace TIME.Metaheuristics.Parallel.WorkAllocation
{
    [Serializable]
    public class WorkPackage
    {
        public WorkPackage(int cellCount)
        {
            Catchments = new HashSet<CatchmentDefinition>();
            Cells = new CellDefinition[cellCount];
        }

        public HashSet<CatchmentDefinition> Catchments { get; private set; }
        public CellDefinition[] Cells { get; private set; }
    }
}