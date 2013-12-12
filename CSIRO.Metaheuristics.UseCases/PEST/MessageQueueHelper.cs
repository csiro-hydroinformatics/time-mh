using System;
using System.Messaging;

namespace CSIRO.Metaheuristics.UseCases.PEST
{

    public class QueueContainer
    {
        public MessageQueue PestToTridentQueue;
        public MessageQueue TridentToPestQueue;

        public void Close()
        {
            PestToTridentQueue.Close();
            TridentToPestQueue.Close();
        }
    }


    /// <summary>
    /// Helper class that creates a standard queue for both the 
    /// Trident application and the PEST application
    /// </summary>
    public static class MessageQueueHelper
    {
        private static MessageQueue GetMessageQueue(string path)
        {
            MessageQueue queue;
            if (MessageQueue.Exists(path))
            {
                try
                {
                    queue = new MessageQueue(path);
                }
                catch (Exception e)
                {
                    throw new Exception("Error Getting message queue", e);
                }
            }
            else
            {
                try
                {
                    queue = MessageQueue.Create(path);
                    
                }
                catch (Exception e)
                {
                    throw new Exception("Error Creating new message queue", e);
                }
            }
            return queue;
        }

        /// <summary>
        /// Creates a new message queue. 
        /// This is the only entry point such that the two applications
        /// are reading/writing to the same queue.
        /// </summary>
        /// <returns></returns>
        public static QueueContainer GetTridentMessageQueue()
        {
            QueueContainer queues = new QueueContainer();
            queues.PestToTridentQueue = MessageQueueHelper.GetMessageQueue(".\\private$\\PestToTridentQueue");
            queues.TridentToPestQueue= MessageQueueHelper.GetMessageQueue(".\\private$\\TridentToPestQueue");
            return queues;

        }




    }
}
