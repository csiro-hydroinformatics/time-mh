using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Messaging;
using TIME.Tools.Optimisation;
using TIME.Tools;
using TIME.Core;
using TIME.Tools.Persistence;
using TIME.Tools.Persistence.DataMapping;
using System.IO;
using TIME.DataTypes;
using TIME.Management;
using System.Diagnostics;


namespace CSIRO.Metaheuristics.UseCases.PEST
{

    public class MessageQueueModelRunner
    {
        private QueueContainer queues;
        private const string LOCAL_QUEUE_PATH = ".";
        private ModelRunner modelRunner;
        private readonly string outputTimeSeriesFile;
        private readonly string outputTimeSeriesName;
        private int currentIteration=0;
#if DEBUG
        private const int TIMEOUT = 1000;
#else
        // set timeout for 10 seconds
        private const int TIMEOUT = 10;
#endif

        private ParameterSet result;
        public int CurrentIteration
        {
            get
            {
                return currentIteration;
            }
        }
        // Need to detemine how the evolution engine and this class
        // are to interact
        // i.e. when does the evolution engine execute pest
        // is it before this class is created? What time frame do
        // I have before the messages are sent from the pest executable
        // to processing the message here
        public MessageQueueModelRunner(string outputFile,ModelRunner mr ,string recordVar)
        {
            this.outputTimeSeriesName = recordVar;
            this.outputTimeSeriesFile = outputFile;
            // possibly build this up earlier (maybe pass a SimulationXml
            this.modelRunner = mr;
            // setup the message passing system
            queues = MessageQueueHelper.GetTridentMessageQueue();
        }

        /// <summary>
        /// Runs a single iteration of the message queue runner.
        /// This waits until it receives a new message (containing a new parameter set to run),
        /// deserializes this message, re-parameterises the model definition and runs a single 
        /// model run. The process completes by saving the result before sending a complete message
        /// back on the queue.
        /// </summary>
        /// <returns> 
        /// Returns whether an iteration was successful
        /// </returns>
        public bool RunIteration(out ParameterSet resultParameters)
        {
            resultParameters = null;
//#if DEBUG
//            Debugger.Launch();
//#endif
            try
            {
                Message receivedMessage = queues.PestToTridentQueue.Receive(new TimeSpan(0,0,TIMEOUT));
                ParameterSet pSet = this.processMessage(receivedMessage);
                
                pSet.applyParametersTo(modelRunner.Model);
                
                TimeSeries output = new TimeSeries();
                
                modelRunner.record(this.outputTimeSeriesName, output);
                modelRunner.execute();
                NonInteractiveIO.Save(this.outputTimeSeriesFile, output);
                // save output time series to specified location

                // send resulting set so we can calculate score on parameters
                resultParameters = pSet;
                this.result = pSet;
                //this.queue.Send("Iteration Complete");
                this.queues.TridentToPestQueue.Send(null);
            }
            catch (MessageQueueException e)
            {
                if (e.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    queues.Close();
                    // return the last given result
                    resultParameters = this.result;
                    return false;
                }
                else
                {
                    throw e;
                }
            }
            // iteration ran successfully
            
            return true;
        }

        /// <summary>
        /// Creates a parameter set from the received message
        /// </summary>
        /// <param name="receivedMessage">Received message from the message queue</param>
        /// <returns>A deserialized parameter set</returns>
        private ParameterSet processMessage(Message receivedMessage)
        {
            Type[] receivedType = new Type[1];
            receivedType[0] = typeof(ParameterSet);
            receivedMessage.Formatter = new XmlMessageFormatter(receivedType);

            ParameterSet modelRunDef = (ParameterSet)receivedMessage.Body;

            return modelRunDef;
        }
    }


}
