﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.Common
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public sealed class GdbServer : AsyncRedirectProcess.EventListener, IDisposable
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly GdbSetup m_gdbSetup;

    private AsyncRedirectProcess m_gdbServerInstance;

    private ManualResetEvent m_gdbServerAttached;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public GdbServer (GdbSetup gdbSetup)
    {
      LoggingUtils.PrintFunction ();

      m_gdbSetup = gdbSetup;

      m_gdbServerInstance = null;

      m_gdbServerAttached = null;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Dispose ()
    {
      LoggingUtils.PrintFunction ();

      if (m_gdbServerInstance != null)
      {
        m_gdbServerInstance.Dispose ();

        m_gdbServerInstance = null;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Start ()
    {
      LoggingUtils.PrintFunction ();

      CheckGdbServerTarget ();

      KillActiveGdbServerSessions ();

      // 
      // Construct a adaptive command line based on GdbSetup requirements.
      // 

      StringBuilder commandLineArgumentsBuilder = new StringBuilder ();

      commandLineArgumentsBuilder.AppendFormat ("run-as {0} lib/gdbserver ", m_gdbSetup.Process.Name);

      if (!string.IsNullOrWhiteSpace (m_gdbSetup.Socket))
      {
        commandLineArgumentsBuilder.AppendFormat ("+{0} ", m_gdbSetup.Socket);
      }

      commandLineArgumentsBuilder.Append ("--attach ");

      if (string.IsNullOrWhiteSpace (m_gdbSetup.Socket)) // Don't need a host if we have a bound socket?
      {
        commandLineArgumentsBuilder.AppendFormat ("{0}:{1} ", m_gdbSetup.Host, m_gdbSetup.Port);
      }

      commandLineArgumentsBuilder.Append (m_gdbSetup.Process.Pid);

      // 
      // Launch 'gdbserver' and wait for output to determine success.
      // 

      Stopwatch waitForConnectionTimer = new Stopwatch ();

      waitForConnectionTimer.Start ();

      m_gdbServerAttached = new ManualResetEvent (false);

      m_gdbServerInstance = AndroidAdb.AdbCommandAsync (m_gdbSetup.Process.HostDevice, "shell", commandLineArgumentsBuilder.ToString ());

      m_gdbServerInstance.Start (this);

      LoggingUtils.Print (string.Format ("[GdbServer] Waiting to attach..."));

      uint timeout = 5000;

      bool responseSignaled = false;

      while ((!responseSignaled) && (waitForConnectionTimer.ElapsedMilliseconds < timeout))
      {
        responseSignaled = m_gdbServerAttached.WaitOne (0);

        if (!responseSignaled)
        {
          Thread.Yield ();
        }
      }

      if (!responseSignaled)
      {
        throw new TimeoutException ("Timed out waiting for GdbServer to execute");
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Kill ()
    {
      LoggingUtils.PrintFunction ();

      try
      {
        if (m_gdbServerInstance != null)
        {
          m_gdbServerInstance.Kill ();
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void CheckGdbServerTarget ()
    {
      // 
      // Check the target 'gdbserver' binary exits on target device/emulator.
      // 

      LoggingUtils.PrintFunction ();

      string expectedPath = string.Format ("ls {0}/gdbserver", m_gdbSetup.Process.NativeLibraryPath);

      using (SyncRedirectProcess checkGdbServer = AndroidAdb.AdbCommand (m_gdbSetup.Process.HostDevice, "shell", expectedPath))
      {
        int exitCode = checkGdbServer.StartAndWaitForExit (1000);

        if ((exitCode != 0) || checkGdbServer.StandardOutput.ToLower ().Contains ("no such file"))
        {
          // TODO: Push the required gdbserver binary, so we can attach to any app.
          throw new InvalidOperationException (string.Format ("Failed to locate required 'gdbserver' binary on device ({0}). Expected location: {1}.", m_gdbSetup.Process.HostDevice.ID, expectedPath));
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void KillActiveGdbServerSessions ()
    {
      LoggingUtils.PrintFunction ();

      // 
      // Killing GDB server instances requires use of run-as [package-name],
      // but it's very difficult to get the parent package of lib/gdbserver as the PPID
      // will always refer to the zygote. This hack uses the sand-boxed 'user' to try all combinations.
      // 

      m_gdbSetup.Process.HostDevice.Refresh ();

      uint [] activeDevicePids = m_gdbSetup.Process.HostDevice.GetActivePids ();

      List <AndroidProcess> activeGdbProcesses = new List<AndroidProcess> ();

      foreach (uint pid in activeDevicePids)
      {
        AndroidProcess process = m_gdbSetup.Process.HostDevice.GetProcessFromPid (pid);

        if (process.Name.Contains ("lib/gdbserver"))
        {
          activeGdbProcesses.Add (process);
        }
      }

      foreach (AndroidProcess gdbProcess in activeGdbProcesses)
      {
        foreach (uint pid in activeDevicePids)
        {
          AndroidProcess process = m_gdbSetup.Process.HostDevice.GetProcessFromPid (pid);

          if ((gdbProcess != process) && (gdbProcess.User.Equals (process.User)))
          {
            LoggingUtils.Print (string.Format ("[GdbServer] Attempting to terminate existing GDB debugging session: {0} ({1}).", gdbProcess.Name, gdbProcess.Pid));

            m_gdbSetup.Process.HostDevice.Shell ("run-as", string.Format ("{0} kill -9 {1}", process.Name, gdbProcess.Pid));
          }
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ProcessStdout (object sendingProcess, DataReceivedEventArgs args)
    {
      if (!string.IsNullOrEmpty (args.Data))
      {
        LoggingUtils.Print (string.Format ("[GdbServer] ProcessStdout: {0}", args.Data));

        if (args.Data.Contains ("Attached;"))
        {
          m_gdbServerAttached.Set ();
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ProcessStderr (object sendingProcess, DataReceivedEventArgs args)
    {
      if (!string.IsNullOrEmpty (args.Data))
      {
        LoggingUtils.Print (string.Format ("[GdbServer] ProcessStderr: {0}", args.Data));
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ProcessExited (object sendingProcess, EventArgs args)
    {
      LoggingUtils.Print (string.Format ("[GdbServer] ProcessExited"));
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
