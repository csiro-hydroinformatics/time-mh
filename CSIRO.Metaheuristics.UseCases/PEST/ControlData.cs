using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Commons.Collections;
using NVelocity;
using NVelocity.App;
using NVelocity.Runtime;
using TIME.DataTypes;
using TIME.Tools.Optimisation;
using System.Xml.Serialization;

namespace CSIRO.Metaheuristics.UseCases.PEST
{
    [Serializable()]
    public class PestControlData
    {
        private const string OBSERVATION_GROUP_NAME = "obsevation";
        #region enum defs
        /// <summary>
        /// Constant container class. Values are readonly not const
        /// to stop copying of values at compile time
        /// </summary>
        public static class PestConstants
        {
            public static class ControlDataConstants
            {
                public static class RestartFileConstant
                {
                    public static readonly string restart = "restart";
                    public static readonly string norestart = "norestart";
                }
                public static class PestMode
                {
                    public static readonly string estimation = "estimation";
                    public static readonly string prediction = "prediction";
                    public static readonly string regularisation = "regularisation";

                }
                public static class Precision
                {
                    public static readonly string doublePrecision = "double";
                    public static readonly string singlePrecision = "single";
                }
                public static class DPoint
                {
                    public static readonly string point = "point";
                    public static readonly string nopoint = "nopoint";
                }
            }
            public static class ParameterGroupConstants
            {
                public static class IncTyp
                {
                    public static readonly string relative = "relative";
                    public static readonly string absolute = "absolute";
                    public static readonly string relativeToMax = "rel_to_max";
                }
                public static class ForwardCentral
                {
                    public static readonly string always2 = "always_2";
                    public static readonly string always3 = "always_3";
                    public static readonly string switchValue = "switch_val";
                }
                public static class ThreePointMethodUsed
                {
                    public static readonly string parabolic = "parabolic";
                    public static readonly string bestFit = "best_fit";
                    public static readonly string outsidePoints = "outside_pts";
                }
            }
            public static class ParameterDataConstants
            {
                public static class ParameterTransformationType
                {
                    public static readonly string noneType = "none";
                    public static readonly string logType = "log";
                    public static readonly string fixedType = "fixed";
                    public static readonly string tiedType = "tied";
                }
                public static class ParameterChangeLimitedType
                {
                    public static readonly string relativeChange = "relative";
                    public static readonly string factorChange = "factor";
                }
            }
        }

        #endregion

        #region control variables

        /// <summary>
        /// Option if pest can be restarted (i.e. binary files are written to disk upon completion or failure).
        /// </summary>
        private string restartFile;

        [XmlElement("restartfile")]
        public string RestartFile
        {
            get { return restartFile; }
            set { restartFile = value; }
        }
        /// <summary>
        /// operation mode. Most likely used in estimation mode
        /// </summary>
        private string pestMode;

        [XmlElement("pestmode")]
        public string PestMode
        {
            get { return pestMode; }
            set { pestMode = value; }
        }
        // number of params, observations, groups and files
        private int? numParameters;

        public int? NumParameters
        {
            get { return numParameters; }
            set { numParameters = value; }
        }
        private int? numObservations;

        public int? NumObservations
        {
            get { return numObservations; }
            set { numObservations = value; }
        }
        private int? numParameterGroups;

        public int? NumParameterGroups
        {
            get { return numParameterGroups; }
            set { numParameterGroups = value; }
        }
        private int? numPrior;

        public int? NumPrior
        {
            get { return numPrior; }
            set { numPrior = value; }
        }
        private int? numObservationGroups;

        /// <summary>
        /// The number of oberservation group names.
        /// This needs to be kept consistent with the 
        /// length of the obervation groups array
        /// </summary>
        public int? NumObservationGroups
        {
            get { return numObservationGroups; }
            set { numObservationGroups = value; }
        }
        private int? numTemplateFiles;

        public int? NumTemplateFiles
        {
            get { return numTemplateFiles; }
            set { numTemplateFiles = value; }
        }
        private int? numInstructionFiles;

        public int? NumInstructionFiles
        {
            get { return numInstructionFiles; }
            set { numInstructionFiles = value; }
        }

        // select double or single precision for results
        private string pestPrecision;

        public string PestPrecision
        {
            get { return pestPrecision; }
            set { pestPrecision = value; }
        }
        // forced decimal point representation
        private string point;
        [XmlElement("point")]
        public string Point
        {
            get { return point; }
            set { point = value; }
        }

        // variables to control how PEST obtains derivatives. Normal operation: numcom = 1, jacfile = 0, messfile = 0
        private int? numcom;

        [XmlElement("numcom")]
        public int? Numcom
        {
            get { return numcom; }
            set { numcom = value; }
        }
        private int? jacfile;
[XmlElement("jacfile")]
        public int? Jacfile
        {
            get { return jacfile; }
            set { jacfile = value; }
        }
        private int? messfile;

[XmlElement("messfile")]
        public int? Messfile
        {
            get { return messfile; }
            set { messfile = value; }
        }

        // initial marquardt lambda value. Suggested values from pest manual ~[1.0,10.0]
        private double? rlambda1;

[XmlElement("rlambda1")]
        public double? Rlambda1
        {
            get { return rlambda1; }
            set { rlambda1 = value; }
        }
        // factor by which PEST reduces the marquardt lambda value. suggested value = 2.0. Must be greater than 1.0
        private double? rlamfac;

[XmlElement("rlamfac")]
        public double? Rlamfac
        {
            get { return rlamfac; }
            set { rlamfac = value; }
        }
        // suggested pest value = 0.3
        private double? phiratsuf;

[XmlElement("phiratsuf")]
        public double? Phiratsuf
        {
            get { return phiratsuf; }
            set { phiratsuf = value; }
        }
        // criterion value for moving on to next optimization step. suggested value = 0.01
        private double? phiredlam;

[XmlElement("phiredlam")]
        public double? Phiredlam
        {
            get { return phiredlam; }
            set { phiredlam = value; }
        }


        // max number of lambdas that can be tested in one optimization iteration.
        private int? numlam;
[XmlElement("numlam")]
        public int? Numlam
        {
            get { return numlam; }
            set { numlam = value; }
        }
        // relative and factor max changes in one iteration step;
        private double? relparmax, facparmax;

[XmlElement("facparmax")]
        public double? Facparmax
        {
            get { return facparmax; }
            set { facparmax = value; }
        }

[XmlElement("relparmax")]
        public double? Relparmax
        {
            get { return relparmax; }
            set { relparmax = value; }
        }


        private double? facorig;

[XmlElement("facorig")]
        public double? Facorig
        {
            get { return facorig; }
            set { facorig = value; }
        }
        private double? phiredswh;

[XmlElement("phiredswh")]
        public double? Phiredswh
        {
            get { return phiredswh; }
            set { phiredswh = value; }
        }

        // max number of iterations. if set to 0. Pest will only run one iteration without calculating the jacobian matrix
        // if set to -1 PEST will terminate after calculating the jacobian matrix the first time.
        private int? noptmax;


        public int? Noptmax
        {
            get { return noptmax; }
            set { noptmax = value; }
        }

        // PEST manual suggested values: phiredstp = 0.005 and nphistp = 4
        private double? phiredstp;
        [XmlElement("phiredstp")]
        public double? Phiredstp
        {
            get { return phiredstp; }
            set { phiredstp = value; }
        }
        private int? nphistp;
        [XmlElement("nphistp")]
        public int? Nphistp
        {
            get { return nphistp; }
            set { nphistp = value; }
        }
        // max number of iteration tries to lower objective function. suggested value = 4
        private int? nphinored;

[XmlElement("nphinored")]
        public int? Nphinored
        {
            get { return nphinored; }
            set { nphinored = value; }
        }

        // suggested values = 0.01 and 3 or 4
        private double? relparstp;

        [XmlElement("relparstp")]
        public double? Relparstp
        {
            get { return relparstp; }
            set { relparstp = value; }
        }
        private int? nrelpar;
[XmlElement("nrelpar")]
        public int? Nrelpar
        {
            get { return nrelpar; }
            set { nrelpar = value; }
        }
        // matrix variables
        private int? icov, icor, ieig;

[XmlElement("ieig")]
        public int? Ieig
        {
            get { return ieig; }
            set { ieig = value; }
        }

[XmlElement("icor")]
        public int? Icor
        {
            get { return icor; }
            set { icor = value; }
        }

[XmlElement("icov")]
        public int? Icov
        {
            get { return icov; }
            set { icov = value; }
        }
        #endregion

        #region parameter groups
        public struct ParameterGroup
        {


            #region parameter group variables
            // group name max 12 characters
            private string parameterGroupName;

            public string ParameterGroupName
            {
                get { return parameterGroupName; }
                set { parameterGroupName = value; }
            }

            // Specifies how pest increments parameters of this group
            private string incrementType;

            public string IncrementType
            {
                get { return incrementType; }
                set { incrementType = value; }
            }
            private double derivativeIncrement;

            public double DerivativeIncrement
            {
                get { return derivativeIncrement; }
                set { derivativeIncrement = value; }
            }

            private double derivativeIncrementLowerBound;

            public double DerivativeIncrementLowerBound
            {
                get { return derivativeIncrementLowerBound; }
                set { derivativeIncrementLowerBound = value; }
            }

            private string forwardCentral;

            public string ForwardCentral
            {
                get { return forwardCentral; }
                set { forwardCentral = value; }
            }

            private double derivativeIncrementMultiplier;

            public double DerivativeIncrementMultiplier
            {
                get { return derivativeIncrementMultiplier; }
                set { derivativeIncrementMultiplier = value; }
            }

            private string derivativeMethodCalculation;

            public string DerivativeMethodCalculation
            {
                get { return derivativeMethodCalculation; }
                set { derivativeMethodCalculation = value; }
            }


            #endregion
        }
        private ParameterGroup[] parameterGroups;

        public ParameterGroup[] ParameterGroups
        {
            get { return parameterGroups; }
            set { 
                parameterGroups = value;
                this.NumParameterGroups = parameterGroups.Length;
            }
        }


        #endregion

        #region parameters
        public struct PESTParameterData
        {

            #region parameter variables
            // most of these values are fairly self explainitory.
            // more detailed restrictions of values are explained
            // in the pest manual
            private string parameterName;

            public string ParameterName
            {
                get { return parameterName; }
                set { parameterName = value; }
            }
            private string transformType;

            public string TransformType
            {
                get { return transformType; }
                set { transformType = value; }
            }
            private string changeLimitedType;

            public string ChangeLimitedType
            {
                get { return changeLimitedType; }
                set { changeLimitedType = value; }
            }
            private double initialValue;

            public double InitialValue
            {
                get { return initialValue; }
                set { initialValue = value; }
            }
            private double minValue, maxValue;

            public double MaxValue
            {
                get { return maxValue; }
                set { maxValue = value; }
            }

            public double MinValue
            {
                get { return minValue; }
                set { minValue = value; }
            }
            private string parameterGroup;

            public string ParameterGroup
            {
                get { return parameterGroup; }
                set { parameterGroup = value; }
            }

            private double scale, offset;

            public double Offset
            {
                get { return offset; }
                set { offset = value; }
            }

            public double Scale
            {
                get { return scale; }
                set { scale = value; }
            }
            // constant value to specifiy whether to use external derivatives functionality
            // assume that we don't want to at this time
            private const double dercom = 1;

            public double Dercom
            {
                get { return dercom; }
            }



            #endregion

        }

        private PESTParameterData[] parameters;

        /// <summary>
        /// Public accessor for the optimization parameters.
        /// When this is changed, the value for number of parameters is
        /// also automatically changed
        /// </summary>
        public PESTParameterData[] Parameters
        {
            get { return parameters; }
            set 
            { 
                parameters = value;
                this.NumParameters = parameters.Length;
            }
        }
        // key = child parameter name, value= parameter parameter name
        public struct TiedParameter
        {
            private string parameterName;

            public string ParameterName
            {
                get { return parameterName; }
                set { parameterName = value; }
            }
            private string tiedParameterName;

            public string TiedParameterName
            {
                get { return tiedParameterName; }
                set { tiedParameterName = value; }
            }
        }
        private TiedParameter[] tiedParameters;

        public TiedParameter[] TiedParameters
        {
            get { return tiedParameters; }
            set { tiedParameters = value; }
        }

        #endregion

        private string[] observationGroupNames;

        public string[] ObservationGroupNames
        {
            get { return observationGroupNames; }
            set
            {
                observationGroupNames = value;
                this.NumObservationGroups = observationGroupNames.Length;
            }
        }

        #region observations
        public struct PESTObservationData
        {
            private string observationName;

            public string ObservationName
            {
                get { return observationName; }
                set { observationName = value; }
            }
            private double observationValue;

            public double ObservationValue
            {
                get { return observationValue; }
                set { observationValue = value; }
            }
            private double weight;

            public double Weight
            {
                get { return weight; }
                set { weight = value; }
            }
            private string observationGroupName;

            public string ObservationGroupName
            {
                get { return observationGroupName; }
                set { observationGroupName = value; }
            }
        }

        private PESTObservationData[] observations;

        public PESTObservationData[] Observations
        {
            get { return observations; }
            set { 
                observations = value;
                this.NumObservations = observations.Length;
            }
        }

        #endregion

        private string modelCommandLine;

        public string ModelCommandLine
        {
            get { return modelCommandLine; }
            set { modelCommandLine = value; }
        }

        #region model input/output
        public struct ModelInputOutput
        {
            /// <summary>
            /// either a template or a instruction file
            /// </summary>
            private string pestFile;

            public string PestFile
            {
                get { return pestFile; }
                set { pestFile = value; }
            }
            /// <summary>
            /// model input or output file
            /// </summary>
            private string modelFile;

            public string ModelFile
            {
                get { return modelFile; }
                set { modelFile = value; }
            }
        };

        private ModelInputOutput[] modelIO;

        public ModelInputOutput[] ModelIO
        {
            get { return modelIO; }
            set { modelIO = value; }
        }

        #endregion

        //TODO: prior information section of the control file.
        // currently have not implemented since I dont know 
        // how I would gather this information from within Trident

        public PestControlData()
        {
            // create empty array so that values can be appended at a later stage
            // array is used rather than a list to work with NVelocity
            this.ModelIO = new ModelInputOutput[0];
        }

        /// <summary>
        /// Sets the default values to all control values as specified in the PEST manual
        /// </summary>
        public void SetDefaultControlValues()
        {
            this.RestartFile = (!String.IsNullOrEmpty(this.restartFile)) ? this.restartFile : PestConstants.ControlDataConstants.RestartFileConstant.restart; 
            this.pestMode = (!String.IsNullOrEmpty(this.pestMode)) ? this.pestMode : PestConstants.ControlDataConstants.PestMode.estimation;
            this.pestPrecision =  PestConstants.ControlDataConstants.Precision.doublePrecision;
            this.point = (!String.IsNullOrEmpty(this.point)) ? this.point : PestConstants.ControlDataConstants.DPoint.point;
            // default is to assume only a single file
            this.numTemplateFiles = 1;
            this.numInstructionFiles = 1;

            // compare to null since these may have already been set when deserialized from xml
            this.numcom = (null != this.numcom) ? this.numcom : 1;
            this.jacfile = (null != this.jacfile) ? this.jacfile : 0;
            this.messfile = (null != this.messfile) ? this.messfile : 0;
            this.rlamfac = (null != this.rlamfac) ? this.rlamfac : 2.0;
            this.phiratsuf = (null != this.phiratsuf) ? this.phiratsuf : 0.3;
            this.phiredlam =(null != this.phiratsuf) ? this.phiratsuf : 0.01;
            this.numlam = ( null != this.numlam) ? this.numlam :  1;
            this.facorig =(null != this.facorig) ? this.facorig : 0.001;
            this.phiredstp = (null != this.phiredstp) ? this.phiredstp : 0.005;
            this.nphistp = (null != this.nphistp) ? this.nphistp : 4;
            this.nphinored = (null != this.nphinored) ? this.nphinored : 4;
            this.relparstp = (null != this.relparstp) ? this.relparstp : 0.01;
            this.nrelpar = (null != this.nrelpar) ? this.nrelpar : 4;
            this.relparmax = (null != this.relparmax) ? this.relparmax : 1.1;
            this.facparmax = (null != this.facparmax) ?  this.facparmax : 1.1;
            this.phiredswh = (null != this.phiredswh) ? this.phiredswh : 0.2;
            this.numParameterGroups = (null != numParameterGroups) ? this.numParameterGroups : 1;
            this.rlambda1 = (null != this.rlambda1) ? this.rlambda1 : 2.0;
            this.numPrior = 0;

            this.icor = (null != this.icor) ? this.icor : 0;
            this.ieig = (null != this.ieig) ? this.ieig : 0;
            this.icov = (null != this.icov) ? this.icov : 0;


            ParameterGroup p = new ParameterGroup();
            p.ParameterGroupName = "param";
            p.IncrementType = "rel_to_max";
            p.DerivativeIncrement = 0.001;
            p.DerivativeIncrementLowerBound = 0.0;
            p.ForwardCentral = "switch";
            p.DerivativeIncrementMultiplier = 1.0;
            p.DerivativeMethodCalculation = "best_fit";
            this.ParameterGroups = new ParameterGroup[1]{p};
  
        }

        public void SetMaxNumberOfIterations(int Iterations)
        {
            if (0 >= Iterations)
            {
                throw new ArgumentException("The maximum number of iterations must be positive (i.e. >= 0)");
            }

            this.noptmax = Iterations;
        }

        public StringWriter CreateControlFile()
        {
            VelocityEngine velocity = new VelocityEngine();
            ExtendedProperties props = new ExtendedProperties();
            props.AddProperty(RuntimeConstants.RESOURCE_LOADER, "assembly");
            props.AddProperty("assembly.resource.loader.class",
                              "NVelocity.Runtime.Resource.Loader.AssemblyResourceLoader, NVelocity");
            props.AddProperty("assembly.resource.loader.assembly", GetType().Assembly.GetName().Name);
            velocity.Init(props);


            // todo how do i get template as stream. check hwb code.
            // Set up the required velocity data types and parameters
            Template controlTemplate = velocity.GetTemplate("CSIRO.Metaheuristics.UseCases.PEST.Resources.ControlFile.vm");
            StringWriter writer = new StringWriter();
            VelocityContext controlContext = new VelocityContext();

            string val = this.RestartFile.ToString();
            controlContext.Put("restartFile", this.RestartFile.ToString());
            controlContext.Put("ControlData", this);

            controlTemplate.Merge(controlContext, writer);

            return writer;
        }

        /// <summary>
        /// populates the internal array of parameters with the relavant pest values. In the 
        /// current version, it considers all parameters to be of the same group (not sure
        /// if this functionality is required for TIME models)
        /// </summary>
        /// <param name="parameters">starting point and range of input parameters </param>
        public void AddParameterSet(IHyperCube<double> parameters)
        {
            List<PESTParameterData> parameterList = new List<PESTParameterData>();
            foreach (String paramName in parameters.GetVariableNames())
            {
                PESTParameterData tmpParameter = new PESTParameterData();
                tmpParameter.Scale = 1.0;
                tmpParameter.MaxValue = parameters.GetMaxValue(paramName);
                tmpParameter.MinValue = parameters.GetMinValue(paramName);
                tmpParameter.InitialValue = parameters.GetValue(paramName);
                tmpParameter.ParameterName = paramName;
                tmpParameter.TransformType = PestConstants.ParameterDataConstants.ParameterTransformationType.noneType;
                tmpParameter.ChangeLimitedType = PestConstants.ParameterDataConstants.ParameterChangeLimitedType.relativeChange;
                tmpParameter.ParameterGroup = "param";
                parameterList.Add(tmpParameter);
            }
            this.Parameters = parameterList.ToArray();
        }

        /// <summary>
        /// Creates a PESTObservationData struct for each observation value
        /// Method allocates all the observation data to the same group.
        /// This must be called after 
        /// </summary>
        /// <param name="observedData"></param>
        /// <param name="groupName"></param>
        /// <remarks>This method must be called AFTER AddObservationGroupNames</remarks>
        public void AddObservationalData(TimeSeries observedData,string groupName)
        {
            if (0 == this.ObservationGroupNames.Length)
            {
                throw new InvalidOperationException("AddObservationalData must be called after AddObservationGroupNames method");
            }

            // check that the group name is in observation group names array
            if (!this.ObservationGroupNames.Contains(groupName))
            {
                throw new ArgumentException(String.Format("Group name,{0} , is not a valid observation group name", groupName));
            }

            // Data type to hold the dates to write 
            // values into template file
            List<DateTime> lDates = new List<DateTime>();

            List<PESTObservationData> observationList = new List<PESTObservationData>();
            
            /* Is there a more general way to construct
             * an array starting from observedData.Start 
             * to observedData.End (with timeStep increments)?
             */
            for (DateTime t = observedData.Start;
                t <= observedData.End;
                t += observedData.timeStep.GetTimeSpan())
            {
                PESTObservationData tmpObservation = new PESTObservationData();
                
                tmpObservation.ObservationName = t.ToString(IsoDateTime.DATE_TSYMBOL_TIME_FORMAT_TO_SECOND);
                tmpObservation.ObservationValue = observedData[t];
                // default value for the time being
                tmpObservation.Weight = 1;
                tmpObservation.ObservationGroupName = groupName;
                observationList.Add(tmpObservation);
            }
            this.Observations = observationList.ToArray();
        }

        /// <summary>
        /// Observation Group names setter.
        /// </summary>
        /// <param name="outputVariableNames"></param>
        public void AddObservationGroupNames(String[] outputVariableNames)
        {
            if (0 == outputVariableNames.Length)
            {
                throw new ArgumentNullException("outputVariableNames must have a least one group name");
            }
            
            this.ObservationGroupNames = outputVariableNames;
        }

        /// <summary>
        /// Adds a new model input output struct to the end of the current model IO values
        /// </summary>
        public void AddModelIOInformation(string PestFile,string ModelFile)
        {
            // get array so we can expand and add new value to list
            List<ModelInputOutput> newModelIOArray;
            ModelInputOutput newModelIOValue = new ModelInputOutput();
            newModelIOValue.PestFile = PestFile;
            newModelIOValue.ModelFile = ModelFile;

            newModelIOArray = this.ModelIO.ToList<ModelInputOutput>();

            newModelIOArray.Add(newModelIOValue);


            this.ModelIO = newModelIOArray.ToArray();
        }

        
        public void CreateCommandLine(string programFileName, string inputFileName)
        {


            String command = "START /B /wait ";
            
            command = String.Concat(command," " ,programFileName);
            
            command = String.Concat(command," ",inputFileName);

            this.ModelCommandLine = command;
        }
    }

}


