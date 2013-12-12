using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TIME.Tools.ModelExecution;
using IronPython.Hosting;
using TIME.Core;
using TIME.Tools;
using TIME.Tools.Persistence;

namespace CSIRO.TIME.Tools.ModelExecution
{
    public class PythonSimulationStateInitialization : AbstractXmlSerializableSimulationStateInitialization
    {
        public PythonSimulationStateInitialization( )
        {
            ScriptBody = string.Empty;
        }

        /// <summary>
        /// Gets or sets the Python Program to execute.
        /// </summary>
        /// <value>The Python Program to execute.</value>
        public string ScriptBody
        {
            get;
            set;
        }

        public override object Clone()
        {
            return new PythonSimulationStateInitialization() { ScriptBody = this.ScriptBody };
        }

        public override void SetupStateInitialization(IPointTimeSeriesSimulation simulation)
        {
            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();
            scope.SetVariable("model", ((ModelRunner)simulation).Model);
            engine.CreateScriptSourceFromString(this.ScriptBody).Execute(scope);
        }
    }
}
