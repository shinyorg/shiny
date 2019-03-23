//using System;
//using Shiny.Jobs;
//using Autofac;
//using Samples.Models;


//namespace Samples.Jobs
//{
//    public class JobLogger : IStartable
//    {
//        readonly IJobManager jobManager;
//        readonly SampleSqliteConnection conn;


//        public JobLogger(IJobManager jobManager, SampleSqliteConnection conn)
//        {
//            this.jobManager = jobManager;
//            this.conn = conn;
//        }


//        public void Start()
//        {
//            this.jobManager.JobStarted += async (sender, args) =>
//            {
//                await this.conn.InsertAsync(new JobLog
//                {
//                    JobName = args.Identifier,
//                    JobType = args.Type.FullName,
//                    Started = true,
//                    Timestamp = DateTime.Now
//                });
//            };
//            this.jobManager.JobFinished += async (sender, args) =>
//            {
//                await this.conn.InsertAsync(new JobLog
//                {
//                    JobName = args.Job.Identifier,
//                    JobType = args.Job.Type.FullName,
//                    Error = args.Exception?.ToString(),
//                    Timestamp = DateTime.Now
//                });
//            };
//        }
//    }
//}
