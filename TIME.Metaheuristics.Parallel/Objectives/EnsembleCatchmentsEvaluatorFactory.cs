using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TIME.Metaheuristics.Parallel.Objectives
{
    public enum EvaluatorEnsembleType
    {
        MPI,
        Serial
    }

    public class EnsembleCatchmentsEvaluatorFactory
    {
        public EvaluatorEnsembleType EnsembleType = EvaluatorEnsembleType.MPI;

        public BaseGriddedCatchmentObjectiveEvaluator Create(
            FileInfo globalDefinitionFileInfo, 
            FileInfo objectivesDefinitionFileInfo,
            int rank,
            int size)
        {
            switch (EnsembleType)
            {
                case EvaluatorEnsembleType.MPI:
                    return new MpiGriddedCatchmentObjectiveEvaluator(globalDefinitionFileInfo, objectivesDefinitionFileInfo, rank);
                case EvaluatorEnsembleType.Serial:
                    return new SerialGriddedCatchmentObjectiveEvaluator(globalDefinitionFileInfo, objectivesDefinitionFileInfo, rank, size);
                default:
                    throw new NotSupportedException(EnsembleType.ToString());
            }
        }
    }
}
