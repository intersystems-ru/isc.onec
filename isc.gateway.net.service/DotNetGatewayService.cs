﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using NLog;

namespace isc.gateway.net
{
    public class DotNetGatewayService : ServiceBase
    {
		/// <summary>
		/// Arguments of the Windows service which are passed to BridgeStarter intance.
		/// Currently, the TCP port number and, optionally, the KeepAlive value. 
		/// </summary>
		private String[] args;
        private BackgroundWorker bw;
        //private ChangedDotNetGatewaySS worker;
        private BridgeStarter worker;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public DotNetGatewayService()
        {
            this.EventLog.Log = "Application";

            this.CanHandlePowerEvent = false;
            this.CanHandleSessionChangeEvent = false;
            this.CanPauseAndContinue = false;
            this.CanShutdown = true;
            this.CanStop = true;

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(unhandledExceptionHandler);
        }
        public DotNetGatewayService(String[] args): this()
        {
            this.args = args;
        }
     
        public static void Main(String[] args)
        {
            //TODO Write code that could be run both as console application and as windows service
            if (Environment.UserInteractive) { Console.WriteLine(typeof(DotNetGatewayService) + " should be run as service"); return; }      
            ServiceBase.Run(new DotNetGatewayService(args));
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }


        protected override void OnStart(string[] args)
        {
            //It has really no sense on normal server, but RG has troubles with virtual machine start time
            this.RequestAdditionalTime(120000);

		if (args.Length != 0) {
			this.changeStartParameters(this.args = args);
		}

		/*
		 * Either use the updated args passed to the OnStart(...) method,
		 * or fall back to those supplied during service creation.
		 */
		this.worker = new BridgeStarter(this.args);
            //worker.addLogger(new EventLogLogger(this.EventLog));

            this.bw = new BackgroundWorker();

            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);

            bw.RunWorkerAsync(worker);
        }

        private void changeStartParameters(string[] args)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine
                                                    .OpenSubKey("System")
                                                    .OpenSubKey("CurrentControlSet")
                                                    .OpenSubKey("Services")
                                                    .OpenSubKey(this.ServiceName, true);

            string imagePath = (string)key.GetValue("ImagePath");
            string path = imagePath.Split(' ')[0];
            key.SetValue("ImagePath", path + " " + String.Join(" ", args));

            logger.Warn("\\System\\CurrentControlSet\\Services\\" + this.ServiceName + "\\ImagePath is changed.\nOld value:" + imagePath + "\nNew value:" + key.GetValue("ImagePath"));

        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                logger.ErrorException("bw_RunWorkerCompleted: Exception happened, stopping ... ", e.Error);
                this.ExitCode = 1;
                this.Stop();
            }
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            //((ChangedDotNetGatewaySS)(e.Argument)).processConnections();
            ((BridgeStarter)(e.Argument)).processConnections();
        }
        //TODO add parameter whether exit or not on unhandled exception
        private void unhandledExceptionHandler(object sender, UnhandledExceptionEventArgs ue)
        {
            Exception e = (Exception)ue.ExceptionObject;
            this.EventLog.WriteEntry("Stopping. Unhandled exception happened. "+e.Message+" "+e.StackTrace, EventLogEntryType.Error);
            logger.ErrorException("Stopping. Unhandled exception happened. ",e);
            this.ExitCode = 1;
            this.Stop();
        }
        //TODO Close sockets and wait for threads
        protected override void OnStop()
        {
            //Of course it would be better to close all sockets immedie
            this.RequestAdditionalTime(120000);
            logger.Info("Cache Bridge Service is stopping.");
            worker.Dispose();
            bw.CancelAsync();
            bw.Dispose();
        }

        protected override void OnShutdown()
        {
            logger.Info("Cache Bridge Service was shutdowned.");
            this.EventLog.WriteEntry("Cache Bridge Service was shutdowned.", EventLogEntryType.Information);
            base.OnShutdown();
        }

        protected override void OnCustomCommand(int command)
        {
            logger.Info("Cache Bridge Service was custom commanded."+command);
            this.EventLog.WriteEntry("Cache Bridge Service custom commanded."+command, EventLogEntryType.Information);
            base.OnCustomCommand(command);
        }

        protected override void OnContinue()
        {
            logger.Info("Cache Bridge Service was continued.");
            this.EventLog.WriteEntry("Cache Bridge Service was continued.", EventLogEntryType.Information);
            base.OnContinue();
        }
        protected override void OnPause()
        {
            logger.Info("Cache Bridge Service was paused.");
            this.EventLog.WriteEntry("Cache Bridge Service was paused", EventLogEntryType.Information);
            base.OnPause();
        }
    }
}
