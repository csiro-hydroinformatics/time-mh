using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Hpc.Scheduler;
using Microsoft.Hpc.Scheduler.Properties;
using System.IO;

namespace CSIRO.Hpc.WindowsHpc
{
    public class WindowsHpcServer
    {
        private WindowsHpcServer( string headNodeName )
        {
            this.headNodeName = headNodeName;
        }
        string headNodeName = string.Empty;

        public static WindowsHpcServer CreateNew( string headNodeName )
        {
            return new WindowsHpcServer( headNodeName );
        }

        private ManualResetEvent manualEvent = new ManualResetEvent( false );


        public void ExecuteWait(IEnumerable<string> tasksCmdLines, 
            int jobMinimumNumberOfCores = 1, int jobMaximumNumberOfCores = 1, 
            int taskMinimumNumberOfCores = 1, int taskMaximumNumberOfCores = 1, 
            string outFolder = "")
        {
            ISchedulerJob job = null;
            ISchedulerTask task = null;
            using( IScheduler scheduler = new Scheduler( ) )
            {
                scheduler.Connect( headNodeName );
                
                job = scheduler.CreateJob( );
                int i = 0;
                foreach (var taskDef in tasksCmdLines)
                {
                    i++;
                    task = job.CreateTask( );
                    task.CommandLine = taskDef;
                    task.MinimumNumberOfCores = taskMinimumNumberOfCores;
                    task.MaximumNumberOfCores = taskMaximumNumberOfCores;
                    task.StdOutFilePath = createOutFileName(outFolder, "stdout", i);
                    task.StdErrFilePath = createOutFileName(outFolder, "stderr", i);
                    job.AddTask(task);
                }

                try
                {
                    job.AutoCalculateMin = false;
                    job.AutoCalculateMax = false;
                    job.MinimumNumberOfCores = jobMinimumNumberOfCores;
                    job.MaximumNumberOfCores = jobMaximumNumberOfCores;
                    job.OnJobState += new EventHandler<JobStateEventArg>(jobStateCallback);
                    job.OnTaskState += new EventHandler<TaskStateEventArg>(taskStateCallback);

                    // Start the job.
                    scheduler.SubmitJob( job, string.Empty, null );

                    // Blocks so the events get delivered. One of your event
                    // handlers need to set this event.
                    manualEvent.WaitOne( );
                }
                finally
                {
                    job.OnJobState -= jobStateCallback;
                    job.OnTaskState -= taskStateCallback;
                }
            }
        }

        private string createOutFileName(string outFolder, string filenameNoExt, int i)
        {
            return string.IsNullOrEmpty(outFolder) ? string.Empty : Path.Combine(outFolder, filenameNoExt + i.ToString("D4") + ".txt");
        }

        private void jobStateCallback( object src, JobStateEventArg jsea )
        {

            switch( jsea.NewState )
            {
                case JobState.Submitted:
                    Console.WriteLine("Job {0} has been submitted", jsea.JobId);
                    break;
                case JobState.Running:
                    Console.WriteLine("Job {0} is now running", jsea.JobId);
                    break;
                case JobState.Finished:
                case JobState.Failed:
                case JobState.Canceled:
                    Console.WriteLine("Job {0} finished with status {1}", jsea.JobId, jsea.NewState.ToString());
                    manualEvent.Set( );
                    break;
                default:
                    break;
            }
        }

        public void taskStateCallback(object src, TaskStateEventArg tsea)
        {
            ConsoleColor c = ConsoleColor.White;
            switch (tsea.NewState)
            {
                case TaskState.Finished:
                    c = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Task {0} is finished", tsea.TaskId);
                    Console.ForegroundColor = c;
                    break;
                case TaskState.Running:
                    Console.WriteLine("Task {0} is running", tsea.TaskId);
                    break;
                case TaskState.Canceled:
                    Console.WriteLine("Task {0} has been cancelled", tsea.TaskId);
                    break;
                case TaskState.Dispatching:
                    Console.WriteLine("Task {0} has been dispatched", tsea.TaskId);
                    break;
                case TaskState.Failed:
                    c = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Task {0} has failed", tsea.TaskId);
                    Console.ForegroundColor = c;
                    break;
                default:
                    break;
            }
        }
    }
}