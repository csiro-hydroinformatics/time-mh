using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics;
using CSIRO.Metaheuristics.Objectives;
using CSIRO.Metaheuristics.Parallel.Objectives;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using TIME.Tools.Metaheuristics.Objectives;
using TIME.Tools.Metaheuristics.SystemConfigurations;

namespace TIME.Metaheuristics.Parallel.Objectives
{
    internal static class ObjectiveScoresHelper
    {
        /// <summary>
        /// Calculates the per-objective means of the specified scores.
        /// </summary>
        /// <param name="scoresArray">The scores.</param>
        /// <param name="sysConfig">The sys config.</param>
        /// <returns>A single <see cref="IObjectiveScores"/> instance where the i'th score is the 
        /// mean of the i'th score in each element of the scores array.
        /// The result score names are taken from the first input element.</returns>
        /// <remarks>
        /// This method makes a few assumptions which are not checked for performance reasons:
        ///     - each element in the scores array has the same objective count. More specifically,
        ///       if an element has more scores than element 0, the additional scores will be ignored.
        ///       If an element has less scores, then an out of bounds array access error will occur.
        ///     - score names are ignored. It is assumed that every element in scores has named scores
        ///       occuring in the same order.
        /// </remarks>
        public static MpiObjectiveScores Mean(MpiObjectiveScores[] scoresArray, MpiSysConfig sysConfig)
        {
            if (scoresArray == null) throw new ArgumentNullException("scoresArray");
            if (sysConfig == null) throw new ArgumentNullException("sysConfig");
            if (scoresArray.Length < 1) throw new ArgumentException("Scores array is empty", "scoresArray");

            IObjectiveScores referenceScore = scoresArray[0];
            int objectiveCount = referenceScore.ObjectiveCount;

            double[] means = new double[objectiveCount];
            for (int objectiveIdx = 0; objectiveIdx < objectiveCount; objectiveIdx++)
            {
                foreach (MpiObjectiveScores objectiveScores in scoresArray)
                    means[objectiveIdx] += (double)objectiveScores.GetObjective(objectiveIdx).ValueComparable;

                means[objectiveIdx] /= scoresArray.Length;
            }
            IObjectiveScore[] meanScores = new IObjectiveScore[objectiveCount];
            for (int i = 0; i < means.Length; i++)
                meanScores[i] = new DoubleObjectiveScore(referenceScore.GetObjective(i).Name, means[i], referenceScore.GetObjective(i).Maximise);

            return new MpiObjectiveScores(meanScores, sysConfig);
        }
    }
}
