using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CSIRO.Metaheuristics;
using CSIRO.Metaheuristics.CandidateFactories;
using CSIRO.Metaheuristics.Fitness;
using CSIRO.Metaheuristics.Logging;
using CSIRO.Metaheuristics.Optimization;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using CSIRO.Metaheuristics.RandomNumberGenerators;
using CSIRO.Metaheuristics.Utils;
using TIME.Metaheuristics.Parallel.ExtensionMethods;
using TIME.Metaheuristics.Parallel.Objectives;
using MPI;
using TIME.DataTypes;
using TIME.Tools.Metaheuristics.Optimization;
using TIME.Tools.Metaheuristics.Persistence;
using TIME.Tools.Metaheuristics.SystemConfigurations;
using TIME.Tools.Persistence;

namespace TIME.Metaheuristics.Parallel
{
    /// <summary>
    ///   Runs the master process. Primary responsibility is running the optimiser.
    ///   Loosely corresponds to CalibrateParallelModel.OptimizerProgram, although some functionality also came from
    ///   CalibrateParallelModel.Program and other classes.
    /// </summary>
    public sealed class MasterSystem : IDisposable
    {
        #region OptimisationMethods enum

        public enum OptimisationMethods
        {
            Sce,
            RosenSce,
        }

        #endregion

        #region TerminationCriteria enum

        public enum TerminationCriteria
        {
            MaxShuffles,
            ConvergenceThreshold
        }

        #endregion

        public const int MinRosenbrockIterations = 10;
        public const int MaxRosenbrockIterations = 5000;
        public const int DefaultRosenbrockIterations = 300;
        public const double MinWallClock = 0.01;
        public const double MaxWallClock = 4000;
        public const double DefaultWallClock = 24;
        public const double MinConvergenceThreshold = 0;
        public const double MaxConvergenceThreshold = 1;
        public const double DefaultConvergenceThreshold = 2.5e-2;
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly MultiCatchmentCompositeObjectiveEvaluator evaluator;
        private readonly IEvolutionEngine<MpiSysConfig> optimisationEngine;
        private readonly SceOptions sceOptions;
        private bool disposed;
        private SceParameterDefinition sceParameterDefinition;

        public MasterSystem(ProgramArguments arguments)
        {
        //DebugHelpers.MpiSleep(10000);
            LoadOptimiserParams(arguments);
            sceOptions = SceOptions.RndInSubComplex;
            if (arguments.SceOptions == "full")
                sceOptions |= SceOptions.ReflectionRandomization;
        //DebugHelpers.MpiSleep(11000);

            OutputPath = arguments.OutputPath;
            // Ensure existance of some locations that are assumed to exist when writing the results
            if (!OutputPath.Exists)
                OutputPath.Create();

        //DebugHelpers.MpiSleep(12000);
            RosenbrockIterations = arguments.RosenbrockIterations.Clamp(MinRosenbrockIterations, MaxRosenbrockIterations);
            WallClock = arguments.WallClockHours.Clamp(MinWallClock, MaxWallClock);
            ConvergenceCriterionCvThreshold = arguments.ConvergenceCriterionCvThreshold.Clamp(MinConvergenceThreshold, MaxConvergenceThreshold);
            TerminationCriterion = (TerminationCriteria) Enum.Parse(typeof (TerminationCriteria), arguments.TerminationCriterion);
            OptimisationMethod = (OptimisationMethods) Enum.Parse(typeof (OptimisationMethods), arguments.OptimisationMethod);
        //DebugHelpers.MpiSleep(13000);
            SeedParametersFile = arguments.ParameterDefinitions.FullName;
            LogFileName = Path.Combine(arguments.OutputPath.FullName, arguments.LogFile);
            OutputFileName = Path.Combine(arguments.OutputPath.FullName, arguments.ResultsFile);
            TemplateParameterSet = GridModelHelper.LoadParameterSpace(arguments.ParameterDefinitions.FullName);
        //DebugHelpers.MpiSleep(14000);
            evaluator = new MultiCatchmentCompositeObjectiveEvaluator(
                arguments.GlobalDefinition, arguments.ObjectiveDefinition, arguments.CreateCompositeEvaluator());
        //DebugHelpers.MpiSleep(15000);
            InMemoryLogger = new InMemoryLogger();
            optimisationEngine = CreateEngine(evaluator, TemplateParameterSet, InMemoryLogger, arguments.Name, arguments.InitString);
        //DebugHelpers.MpiSleep(16000);
        }

        #region Properties

        private string SeedParametersFile { get; set; }

        private string LogFileName { get; set; }

        private InMemoryLogger InMemoryLogger { get; set; }

        public IOptimizationResults<ICloneableSystemConfiguration> Results { get; private set; }

        public DirectoryInfo OutputPath { get; set; }

        public string OutputFileName { get; set; }

        public string PythonSysPaths { get; set; }

        private MpiSysConfig TemplateParameterSet { get; set; }

        public SceParameterDefinition SceParameterDefinition
        {
            get { return sceParameterDefinition; }
            set
            {
                sceParameterDefinition = value;
                if (sceParameterDefinition.Parameters.NumShuffle < 2)
                {
                    Log.Warn("Root: Number of SCE shuffles must be >= 2");
                    sceParameterDefinition.Parameters.NumShuffle = 2;
                }
            }
        }

        public int RosenbrockIterations { get; set; }

        public TerminationCriteria TerminationCriterion { get; set; }

        public double WallClock { get; set; }

        public double ConvergenceCriterionCvThreshold { get; set; }

        public OptimisationMethods OptimisationMethod { get; set; }

        #endregion

        #region IDisposable Members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private IEvolutionEngine<MpiSysConfig> CreateEngine(
            IClonableObjectiveEvaluator<MpiSysConfig> objectiveEvaluator,
            MpiSysConfig templateParameterSet,
            ILoggerMh logger,
            string calibName,
            string initialisationOption)
        {
            Log.Debug("Root: creating optimisation engine");
            var dict = string.IsNullOrEmpty(calibName) ? CreateTimeStampCalibName() : CreateTag(calibName);
            var populationInitializer = CreatePopulationInitialiser(templateParameterSet, objectiveEvaluator, initialisationOption);
            return CreateEngine(objectiveEvaluator, dict, populationInitializer, logger);
        }

        private IEvolutionEngine<MpiSysConfig> CreateEngine(
            IClonableObjectiveEvaluator<MpiSysConfig> objectiveEvaluator,
            IDictionary<string, string> dict,
            ICandidateFactory<MpiSysConfig> populationInitializer,
            ILoggerMh logger)
        {
            switch (OptimisationMethod)
            {
                default:
                case OptimisationMethods.Sce:
                    return CreateShuffledComplexEvolution(objectiveEvaluator, dict, populationInitializer, logger);
                case OptimisationMethods.RosenSce:
                    return CreateSceRosenEngine(objectiveEvaluator, dict, populationInitializer, logger);
            }
        }

        private static Dictionary<string, string> CreateTimeStampCalibName()
        {
            return CreateTag(DateTime.Now.ToString(IsoDateTime.DATE_TSYMBOL_TIME_FORMAT_TO_SECOND));
        }

        private static Dictionary<string, string> CreateTag(string calibName)
        {
            return new Dictionary<string, string> {{"CalibName", calibName}};
        }

        private void LoadOptimiserParams(ProgramArguments arguments)
        {
            SceParameterDefinition = InputOutputHelper.DeserializeFromXML<SceParameterDefinition>(arguments.OptimiserParams.FullName);
        }

        /// <summary>
        ///   Runs the master system.
        /// </summary>
        public void Run()
        {
            Log.Info("Root: running optimiser");
            
            Results = optimisationEngine.Evolve();
            
            // Done. Print out some runtime data
            int simulationCount = evaluator.SimulationCount;
            float workerCount = MPI.Communicator.world.Size - 1;
            float cellCount = evaluator.TotalCellCount;
            float cellDensity = cellCount / workerCount;
            TimeSpan simulationTime = evaluator.SimulationTime;
            double avgTimePerSimulation = (simulationTime.TotalMilliseconds / simulationCount) * 0.001; // time in seconds
            Log.InfoFormat("Worker count: {0}", workerCount);
            Log.InfoFormat("Cell count: {0}", cellCount);
            Log.InfoFormat("Cell density: {0}", cellDensity);
            Log.InfoFormat("Total simulation time: {0}", simulationTime);
            Log.InfoFormat("Simulations: {0}", simulationCount);
            Log.InfoFormat("Average time per simulation: {0}", avgTimePerSimulation);
            
            Log.Info("Root: optimisation complete");
        }

        /// <summary>
        ///   This method must be called by the root process, as RunSlaveSystem sits in an infinite loop.
        ///   Only the root process containing the optimiser knows when the work is complete.
        /// </summary>
        public void TerminateSlaves()
        {
            // this is required because the Slaves sit in an infinite loop waiting for a new parameter set.
            Log.Info("Root: Shutting down MPI processes");
            MpiWorkPacket workPacket = new MpiWorkPacket(SlaveActions.Terminate);
            Communicator.world.Broadcast(ref workPacket, 0);
        }

        /// <summary>
        ///   Writes the results.
        /// </summary>
        public void WriteResults()
        {
            Log.InfoFormat("Writing in-memory log to '{0}'", LogFileName);
            InMemoryLogger.CsvSerialise(LogFileName, "GriddedCalib");

            Log.InfoFormat("Root: saving final parameters to '{0}'", OutputFileName);
            MetaheuristicsHelper.SaveAsCsv<MpiSysConfig>(Results, OutputFileName);

            string parameterSetPath = Path.Combine(OutputPath.FullName, "BestParamSet.xml");
            Log.InfoFormat("Root: saving reseed parameters to '{0}'", parameterSetPath);
            GridModelHelper.WriteParameters<MpiSysConfig>(Results, SeedParametersFile, parameterSetPath);
        }

        /// <summary>
        ///   Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"> <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources. </param>
        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here
                    evaluator.Dispose();
                }

                // dispose unmanaged resources here

                // disposal is done. Set the flag so we don't get disposed more than once.
                disposed = true;
            }
        }

        #region SCE

        private ShuffledComplexEvolution<MpiSysConfig> CreateShuffledComplexEvolution(
            IClonableObjectiveEvaluator<MpiSysConfig> objectiveEvaluator,
            IDictionary<string, string> dict,
            ICandidateFactory<MpiSysConfig> populationInitializer,
            ILoggerMh logger)
        {
            Log.Debug("Root: creating SCE optimiser");
            ShuffledComplexEvolution<MpiSysConfig> optimEngine = new ShuffledComplexEvolution<MpiSysConfig>(
                objectiveEvaluator,
                populationInitializer,
                CreateTerminationCriterion(),
                SceParameterDefinition.Parameters.P,
                SceParameterDefinition.Parameters.M,
                SceParameterDefinition.Parameters.Q,
                SceParameterDefinition.Parameters.Alpha,
                SceParameterDefinition.Parameters.Beta,
                SceParameterDefinition.Parameters.NumShuffle,
                new BasicRngFactory(SceParameterDefinition.RandomizationSeed + 1000),
                new DefaultFitnessAssignment(),
                dict,
                trapezoidalPdfParam: SceParameterDefinition.Parameters.TrapezoidalDensityParameter,
                options: sceOptions,
                pmin: SceParameterDefinition.Parameters.Pmin) {Logger = logger};
            return optimEngine;
        }

        private ITerminationCondition<MpiSysConfig> CreateTerminationCriterion()
        {
            switch (TerminationCriterion)
            {
                case TerminationCriteria.MaxShuffles:
                    Log.Debug("Root: Using max shuffle termination condition");
                    return new ShuffledComplexEvolution<MpiSysConfig>.MaxShuffleTerminationCondition();
                default:
                case TerminationCriteria.ConvergenceThreshold:
                    Log.Debug("Root: Using coefficient of variation termination condition");
                    return new ShuffledComplexEvolution<MpiSysConfig>.CoefficientOfVariationTerminationCondition(
                        threshold: ConvergenceCriterionCvThreshold, maxHours: WallClock);
            }
        }

        #endregion

        #region Rosenbrock + SCE

        private IEvolutionEngine<MpiSysConfig> CreateSceRosenEngine(
            IClonableObjectiveEvaluator<MpiSysConfig> objectiveEvaluator,
            IDictionary<string, string> dict,
            ICandidateFactory<MpiSysConfig> populationInitializer,
            ILoggerMh logger)
        {
            Log.Debug("Root: Creating Rosenbrock + SCE optimiser");
            Func<IOptimizationResults<MpiSysConfig>> rosenFunc = () => ExecuteRosenbrock(objectiveEvaluator, dict, populationInitializer, logger);
            return new ChainOptimizations<MpiSysConfig>("Sce+Rosen", rosenFunc);
        }

        private IOptimizationResults<MpiSysConfig> ExecuteRosenbrock(
            IClonableObjectiveEvaluator<MpiSysConfig> objectiveEvaluator,
            IDictionary<string, string> dict,
            ICandidateFactory<MpiSysConfig> populationInitializer,
            ILoggerMh logger)
        {
            Log.Debug("Root: Executing Rosenbrock optimiser");
            var engine = new RosenbrockOptimizer<MpiSysConfig, double>(
                objectiveEvaluator,
                ExecuteShuffledComplexForRosen(objectiveEvaluator, dict, populationInitializer, logger),
                new RosenbrockOptimizer<MpiSysConfig, double>.RosenbrockOptimizerIterationTermination(RosenbrockIterations),
                logTags: dict) {Logger = logger, AlgebraProvider = new TimeAlgebraProvider()};

            return engine.Evolve();
        }

        /// <summary>
        ///   Executes the shuffled complex evolution for nesting within the rosenbrock optimiser.
        /// </summary>
        /// <param name="objectiveEvaluator"> The compound obj calculator. </param>
        /// <param name="dict"> The dict. </param>
        /// <param name="populationInitializer"> The population initializer. </param>
        /// <param name="logger"> The logger. </param>
        /// <returns> </returns>
        private MpiSysConfig ExecuteShuffledComplexForRosen(
            IClonableObjectiveEvaluator<MpiSysConfig> objectiveEvaluator,
            IDictionary<string, string> dict,
            ICandidateFactory<MpiSysConfig> populationInitializer,
            ILoggerMh logger)
        {
            Log.Debug("Root: Executing SCE optimiser");
            var optimEngine = CreateShuffledComplexEvolution(objectiveEvaluator, dict, populationInitializer, logger);
            return BestParameterSet(optimEngine.Evolve(), new DefaultFitnessAssignment());
        }

        private MpiSysConfig BestParameterSet(IEnumerable<IObjectiveScores> population, IFitnessAssignment<double> fitnessAssignment)
        {
            var tmpArray = fitnessAssignment.AssignFitness(population.ToArray());
            Array.Sort(tmpArray);
            return (MpiSysConfig) tmpArray[0].Scores.GetSystemConfiguration();
        }

        #endregion

        #region Population Initialiser

        private ICandidateFactory<MpiSysConfig> CreatePopulationInitialiser(
            MpiSysConfig templateParameterSet,
            IClonableObjectiveEvaluator<MpiSysConfig> objectiveEvaluator,
            string initialisationOption = "")
        {
            Log.Debug("Root: creating population initialiser");
            var options = initialisationOption.Split(':');
            var seeds = new List<MpiSysConfig> {templateParameterSet};
            var rng = new BasicRngFactory(SceParameterDefinition.RandomizationSeed);

            ICandidateFactory<MpiSysConfig> result = CreatePopulationInitialiser(
                templateParameterSet,
                rng,
                objectiveEvaluator,
                options);
            if (result is BestOfSampling<MpiSysConfig>)
                return result;
            else
                return new SeededSamplingFactory<MpiSysConfig>(result, seeds);
        }

        private static ICandidateFactory<MpiSysConfig> CreatePopulationInitialiser(
            MpiSysConfig templateParameterSet,
            BasicRngFactory rng,
            IClonableObjectiveEvaluator<MpiSysConfig> objectiveEvaluator,
            params string[] options)
        {
            switch (options[0].ToLower())
            {
                case "bestof":
                    int poolSize = int.Parse(options[1]);
                    int bestPoints = int.Parse(options[2]);
                    string[] subOptions = Subset(3, options);

                    if (poolSize < bestPoints)
                    {
                        Log.WarnFormat(
                            "Root: Best of Sampling pool size ({0}) is smaller than best points ({1}). Setting best points to match pool size.",
                            poolSize,
                            bestPoints);
                        bestPoints = poolSize;
                    }

                    Log.DebugFormat("Root: creating BestOfSampling. Pool size: {0}, best points: {1}", poolSize, bestPoints);
                    return
                        new BestOfSampling<MpiSysConfig>(
                            CreatePopulationInitialiser(templateParameterSet, rng, objectiveEvaluator, subOptions),
                            poolSize,
                            bestPoints,
                            objectiveEvaluator,
                            new DefaultFitnessAssignment());

                case "lhc":
                    Log.Debug("Root: creating Latin Hypercube Sampling");
                    return new LatinHypercubeSampling<MpiSysConfig>(rng, templateParameterSet);

                case "weibull":
                    Log.Debug("Root: creating weibull distribution");
                    return new SeededSamplingFactory<MpiSysConfig>(
                        new WeibullGen<MpiSysConfig>(rng, templateParameterSet),
                        new List<MpiSysConfig> {templateParameterSet});

                case "":
                case "urs":
                default:
                    Log.Debug("Root: creating URS");
                    return new UniformRandomSamplingFactory<MpiSysConfig>(rng, templateParameterSet);
            }
        }

        private static string[] Subset(int startIndex, string[] options)
        {
            string[] result = new string[options.Length - startIndex];
            for (int i = startIndex; i < options.Length; i++)
            {
                result[i - startIndex] = options[i];
            }
            return result;
        }

        #endregion
    }
}