using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics;
using CSIRO.Metaheuristics.Objectives;
using CSIRO.Metaheuristics.Parallel.Objectives;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using TIME.Metaheuristics.Parallel.Objectives;
using NUnit.Framework;
using TIME.Metaheuristics.Parallel.SystemConfigurations;
using TIME.Tools.Metaheuristics.Objectives;
using TIME.Tools.Metaheuristics.SystemConfigurations;

namespace TIME.Metaheuristics.Parallel.Tests
{
    [TestFixture]
    class ObjectiveScoresHelperTests
    {
        private Random random;
        private MpiSysConfig config;

        [TestFixtureSetUp]
        public void Setup()
        {
            random = new Random();
            config = new MpiSysConfigTIME();
        }

        [Test]
        public void CorrectObjectiveCount()
        {
            const int objectivesPerScore = 12;
            const int numScores = 305;
            var randomScores = CreateRandomScores(numScores, objectivesPerScore);
            IObjectiveScores result = ObjectiveScoresHelper.Mean(randomScores, config);
            Assert.That(result.ObjectiveCount, Is.EqualTo(objectivesPerScore));
        }

        [Test]
        public void CorrectObjectiveNames()
        {
            const int objectivesPerScore = 3;
            const int numScores = 3;
            var randomScores = CreateRandomScores(numScores, objectivesPerScore);
            IObjectiveScores result = ObjectiveScoresHelper.Mean(randomScores, config);
            for (int i = 0; i < objectivesPerScore; i++)
                Assert.That(result.GetObjective(i).Name, Is.EqualTo(randomScores[0].GetObjective(i).Name));
        }

        [Test]
        public void CorrectMeans()
        {
            const int numScores = 4;
            const int numObjectives = 3;
            MpiObjectiveScores[] scores = new MpiObjectiveScores[numScores];
            double[] expectedMeans = new double[numObjectives];

            for (int i = 0; i < numScores; i++)
            {
                IObjectiveScore[] objectives = new IObjectiveScore[numObjectives];
                for (int j = 0; j < numObjectives; j++)
                {
                    double fs = FakeScore(i + j);
                    expectedMeans[j] += fs;
                    objectives[j] = new DoubleObjectiveScore(String.Format("{0}-{1}", i, j), fs, true);
                }

                scores[i] = new MpiObjectiveScores(objectives, config);
            }

            for (int i = 0; i < numObjectives; i++)
                expectedMeans[i] /= numScores;

            var result = ObjectiveScoresHelper.Mean(scores, config);
            for (int i = 0; i < numObjectives; i++)
            {
                Assert.That((double)result.GetObjective(i).ValueComparable, Is.EqualTo(expectedMeans[i]).Within(1e-8));
            }
        }

        [Test]
        public void ArgumentNullExceptions()
        {
            ArgumentNullException e = Assert.Throws<ArgumentNullException>(() => ObjectiveScoresHelper.Mean(null, config));
            Assert.AreEqual(e.ParamName, "scores");

            e = Assert.Throws<ArgumentNullException>(() => ObjectiveScoresHelper.Mean(new MpiObjectiveScores[3], null));
            Assert.AreEqual(e.ParamName, "sysConfig");
        }

        [Test]
        public void RandomScoreCreationTest()
        {
            var randomScores = CreateRandomScores(9, 7);
            Assert.That(randomScores, Is.Not.Null);
            Assert.That(randomScores.Length, Is.EqualTo(9));
            foreach (var objectiveScores in randomScores)
                Assert.That(objectiveScores.ObjectiveCount, Is.EqualTo(7));
        }

        private MpiObjectiveScores[] CreateRandomScores(int numScores, int objectivesPerScore)
        {
            MpiObjectiveScores[] scores = new MpiObjectiveScores[numScores];

            for (int i = 0; i < numScores; i++)
            {
                IObjectiveScore[] objectives = new IObjectiveScore[objectivesPerScore];
                for (int j = 0; j < objectivesPerScore; j++)
                    objectives[j] = new DoubleObjectiveScore(String.Format("{0}-{1}", i, j), FakeScore(i + j), true);

                scores[i] = new MpiObjectiveScores(objectives, config);
            }

            return scores;
        }

        private double FakeScore(double factor = 0)
        {
            return (random.NextDouble() + factor) / 2;
        }
    }
}
