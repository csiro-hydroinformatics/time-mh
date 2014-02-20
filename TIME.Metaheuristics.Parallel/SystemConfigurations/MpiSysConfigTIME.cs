using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using TIME.Core;
using TIME.Tools.ModelExecution;
using TIME.Tools.Optimisation;

namespace TIME.Metaheuristics.Parallel.SystemConfigurations
{
    /// <summary>
    /// A serializable implementation of a traditional hypercube, for use in Message Passing.
    /// </summary>
    [Serializable]
    public class MpiSysConfigTIME : MpiSysConfig
    {
        public MpiSysConfigTIME(MpiSysConfigTIME template)
            : base(template)
        {
            this.fullyQualifiedName = template.fullyQualifiedName;
        }

        public MpiSysConfigTIME()
        {
        }

        public string fullyQualifiedName;

        public MpiSysConfigTIME(ParameterSet pset)
        {
            var pParams = pset.freeParameters;
            fullyQualifiedName = pset.modelType.AssemblyQualifiedName;
            var configs = new List<MpiParameterConfig>();
            foreach (var item in pParams)
            {
                configs.Add(new MpiParameterConfig { name = item.member.Name, value = item.Value, min = item.min, max = item.max });
            }
            this.parameters = configs.ToArray();
        }

        private ParameterSet CreateParameterizer(IModel model)
        {
            var pset = new ParameterSet(model);
            foreach (var item in this.parameters)
            {
                var pspec = pset.paramSpec(item.name);
                pspec.isFixed = false;
                pspec.min = item.min;
                pspec.max = item.max;
                pset[item.name] = item.value;
            }
            return pset;
        }

        public override ICloneableSystemConfiguration Clone()
        {
            return new MpiSysConfigTIME
            {
                fullyQualifiedName = this.fullyQualifiedName,
                parameters = (MpiParameterConfig[])this.parameters.Clone()
            };
        }


        public override void ApplyConfiguration(object system)
        {
            if (system is IModelRunner)
                this.ApplyConfiguration((IModelRunner)system);
            else if (system is IModel)
                this.ApplyConfiguration((IModel)system);
            else if (system is IParameterSetHandler)
                this.ApplyConfiguration((IParameterSetHandler)system);
            //else if (system is RCodeSimulation)
            //    this.ApplyConfiguration((RCodeSimulation)system);
            else
                throw new ArgumentException("This can apply parameter sets only to objects implementing IModelRunner, IModel or IParameterSetHandler");
        }

        private void ApplyConfiguration(IModelRunner modelRunner)
        {
            this.CreateParameterizer(modelRunner.Model).applyParametersTo(modelRunner.Model);
        }

        private void ApplyConfiguration(IModel model)
        {
            this.CreateParameterizer(model).applyParametersTo(model);
        }

        private void ApplyConfiguration(IParameterSetHandler system)
        {
            IModel model = (IModel)Activator.CreateInstance(Type.GetType(this.fullyQualifiedName));
            system.ParameterSet = this.CreateParameterizer(model);
        }

        //public void ApplyConfiguration(RCodeSimulation system)
        //{
        //    RCodeSimulation sim = (RCodeSimulation)system;
        //    var varNames = this.GetVariableNames();
        //    foreach (var varName in varNames)
        //    {
        //        sim.rEngine.EagerEvaluate(varName + " <- " + this.GetValue(varName).ToString());
        //    }
        //    //tryExecuteRFunction("onCandidateParameterSet()");
        //    string function = "onCandidateParameterSet()";
        //    try
        //    {
        //        sim.rEngine.EagerEvaluate(function);
        //    }
        //    catch (ParseException e)
        //    {
        //        throw new Exception("There was an error attempting to evaluate the R script function: " + function, e);
        //    }
        //}

        internal void CopyValuesFrom(ParameterSet pset)
        {
            var pParams = pset.freeParameters;
            foreach (var item in pParams)
            {
                this.SetValue(item.member.Name, item.Value);
            }
        }
    }
}
