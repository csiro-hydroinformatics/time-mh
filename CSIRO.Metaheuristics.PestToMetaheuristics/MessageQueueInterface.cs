using System;
using System.Messaging;
using TIME.Tools.Optimisation;
using CSIRO.Metaheuristics.UseCases.PEST;


namespace PestToMetaheuristics
{
    /// <summary>
    /// MessageQueueInterface is a helper class for the PestToMetaheuristics application
    /// to do the following:
    /// 1. Setup the message queue
    /// 2. Send parameter set to pest model executor process
    /// 3. wait until model has finished running (so that pest can read the results)
    /// </summary>
    internal class MessageQueueInterface
    {
        //private QueueContainer queues;
        private QueueContainer queues;
        public MessageQueueInterface()
        {
            queues = MessageQueueHelper.GetTridentMessageQueue();
            
        }

        /// <summary>
        /// Executes a single model run interation by sending 
        /// a single parameter set to the PEST Model runner (separate process)
        /// </summary>
        /// <param name="pSet">Parameter set to be executed</param>
        public void RunModelExecution(ParameterSet pSet)
        {
            Message parameterSetMessage = new Message();
            parameterSetMessage = createParameterSetMessage(pSet);
            // send new parameter set to execute a model run
            queues.PestToTridentQueue.Send(parameterSetMessage);
            
            // waits until receive sync message (empty body!)
            Message receivedMessage = queues.TridentToPestQueue.Receive();
            queues.Close();
            
        }

        /// <summary>
        /// Creates a message queue message that can handle the
        /// parameter set object. An explicit message queue formatter
        /// is required otherwise the message body (the parameter set)
        /// will not be able to be formatted and sent onto the queue
        /// </summary>
        /// <param name="pset"></param>
        /// <returns></returns>
        private Message createParameterSetMessage(ParameterSet pset)
        {
            Message formattedMessage = new Message();

            Type[] sendType = new Type[1];
            sendType[0] = typeof(ParameterSet);
            formattedMessage.Formatter = new XmlMessageFormatter(sendType);

            formattedMessage.Body = pset;
            return formattedMessage;
        }
        

    }
}
