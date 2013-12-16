using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Hpc.Scheduler;

namespace CalibrateGriddedModel
{
    public class WinHpcSchedulerJob
    {
        private ISchedulerJob Job { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WinHpcSchedulerJob"/> class.
        /// </summary>
        public WinHpcSchedulerJob()
        {
            // Discover the job's context from the environment
            String headNodeName = Environment.GetEnvironmentVariable("CCP_CLUSTER_NAME");
            int jobId = Convert.ToInt32(Environment.GetEnvironmentVariable("CCP_JOBID"));

            // Connect to the head node and get the job
            IScheduler scheduler = new Scheduler();
            scheduler.Connect(headNodeName);
            Job = scheduler.OpenJob(jobId);
        }

        /// <summary>
        /// Updates the progress.
        /// </summary>
        /// <param name="percentage">The percentage. Should be between 0 and 100. It will be clamped if out of range.</param>
        /// <param name="message">The optional progress message.</param>
        public void UpdateProgress(int percentage, string message = null)
        {
            // Set the progress percentage (must be an int between 0 - 100)
            Job.Progress = percentage;

            if (message != null)
                Job.ProgressMessage = message;

            // Commit the change
            Job.Commit();
        }
    }
}
