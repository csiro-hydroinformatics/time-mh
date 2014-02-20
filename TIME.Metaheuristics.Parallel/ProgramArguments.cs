using System.IO;
using CSIRO.Metaheuristics.Parallel.Objectives;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;

// #pragma warning disable 649     // Field is never assigned to, and will always have its default value

namespace TIME.Metaheuristics.Parallel
{
    public abstract class ProgramArguments
    {
        //[DirectoryArgument('o', "outputPath", Description = "Output directory for final calibration results", FullDescription = "You must have write permissions at this location.", DirectoryMustExist = false, Optional = false)]
        public abstract DirectoryInfo OutputPath{ get; set; }

        //[ValueArgument(typeof(string), 'r', "resultsFile", Description = "Results file name", FullDescription = "The results file will be created in the output directory (outputPath argument).", DefaultValue = "parameters.csv", Optional = true)]
        public abstract string ResultsFile{ get; set; }
        
        //[ValueArgument(typeof(string), 'l', "log", Description = "Log file name", FullDescription = "The log file will be created in the output directory (outputPath argument).", Optional = true, DefaultValue = "outputLog.csv")]
        public abstract string LogFile{ get; set; }

        //[ValueArgument(typeof(string), 'n', "name", Description = "The calibration experiment name", Optional = false)]
        public abstract string Name{ get; set; }

        //[FileArgument('s', "simulationDef", Description = "Simulation metadata that defines the catchments and cells to be calibrated", FileMustExist = true, Optional = false)]
        public abstract FileInfo GlobalDefinition{ get; set; }

        //[FileArgument('b', "objectiveDef", Description = "Defines the statistics to be used in the objective functions", FileMustExist = true, Optional = false)]
        public abstract FileInfo ObjectiveDefinition{ get; set; }

        //[FileArgument('c', "globalCompoundObjective", Description = "Defines the R compound objective function used to produce the global score from the catchment scores", FileMustExist = true, Optional = false)]
        public abstract FileInfo GlobalCompoundObjectiveDefinition{ get; set; }

        public abstract CompositeObjectiveCalculation<MpiSysConfig> CreateCompositeEvaluator();

        //[FileArgument('i', "inputParams", Description = "Input parameter definitions", FileMustExist = true, Optional = false)]
        public abstract FileInfo ParameterDefinitions{ get; set; }

        //[FileArgument('x', "seedParams", Description = "Additional seeds for the population", FileMustExist = true, Optional = true)]
        public abstract FileInfo SeedParameterSets { get; set; }

        //[FileArgument('p', "optimiserParams", Description = "Additional parameters for the optimiser", FileMustExist = true, Optional = false)]
        public abstract FileInfo OptimiserParams{ get; set; }

        //[BoundedValueArgument(typeof(int), 'e', "RosenIter", MinValue = MasterSystem.MinRosenbrockIterations, MaxValue = MasterSystem.MaxRosenbrockIterations, DefaultValue = MasterSystem.DefaultRosenbrockIterations, Description = "Maximum number of iterations for the Rosenbrock optimiser", Optional = true)]
        public abstract int RosenbrockIterations{ get; set; }

        // Note that the allowed values must match the tags in MasterProgram.TerminationCriteria
        //[EnumeratedValueArgument(typeof(string), 'z', "terminationCriterion", AllowedValues = "MaxShuffles;ConvergenceThreshold", Description = "type of termination criteria", Optional = true, DefaultValue = "MaxShuffles")] 
        public abstract string TerminationCriterion{ get; set; }

        //[BoundedValueArgument(typeof(double), 'w', "wallClock", MinValue = MasterSystem.MinWallClock, MaxValue = MasterSystem.MaxWallClock, DefaultValue = MasterSystem.DefaultWallClock, Description = "Wall clock value in hours", Optional = true)]
        public abstract double WallClockHours{ get; set; }

        //[BoundedValueArgument(typeof(double), 'x', "convergeThreshold", MinValue = MasterSystem.MinConvergenceThreshold, MaxValue = MasterSystem.MaxConvergenceThreshold, DefaultValue = MasterSystem.DefaultConvergenceThreshold, Description = "Convergence criterion Cv threshold", Optional = true)]
        public abstract double ConvergenceCriterionCvThreshold{ get; set; }

        //[BoundedValueArgument(typeof(int), 'e', "randSeed", MinValue = 0, MaxValue = Int32.MaxValue, DefaultValue = 0, Description = "Seed for the pseudo random number generator", Optional = true)]
        //public abstract int PrngSeed{ get; set; }

        //[ValueArgument(typeof(string), 't', "init", Description = "Initialisation string for the model", DefaultValue = "", Optional = true)]
        public abstract string InitString{ get; set; }

        //[EnumeratedValueArgument(typeof(string), 'm', "sceOptions", AllowedValues = "partial;full", Description = "Additional option to optimiser. Valid values are 'partial' and 'full'", Optional = true, DefaultValue = "partial")]
        public abstract string SceOptions{ get; set; }

        // Note that the allowed values must match the tags in MasterProgram.OptimisationMethods
        //[EnumeratedValueArgument(typeof(string), 'k', "optimisationMethod", AllowedValues = "Sce;RosenSce", Description = "Optimisation method to use", Optional = true, DefaultValue = "Sce")] 
        public abstract string OptimisationMethod{ get; set; }
    }
}

//#pragma warning restore 649
