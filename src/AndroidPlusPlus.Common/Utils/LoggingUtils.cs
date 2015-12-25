﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.Common
{
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class LoggingUtils
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void Print (string format)
    {
      DateTime now = DateTime.Now;

      string text = string.Format ("[{0}] {1}", now.ToString ("u"), format);

#if DEBUG && false
      Debug.WriteLine (text);
#endif

      Trace.WriteLine (text);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void PrintFunction ()
    {
#if DEBUG
      // 
      // Print the class, method name, and attributes of the caller function.
      // 

      StackTrace stackTrace = new StackTrace ();

      StackFrame stackFrame = stackTrace.GetFrame (1);

      MethodBase method = stackFrame.GetMethod ();

      Print (string.Format ("[{0}] {1}", method.DeclaringType.Name, method.Name));
#endif
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static bool CheckOk (int handle)
    {
      return (handle == 0);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void RequireOk (int handle)
    {
      if (!CheckOk (handle))
      {
        throw new InvalidOperationException (string.Format ("RequireOk received unexpected error handle (0x{0})", handle.ToString ("X8")));
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void RequireOk (int handle, string error)
    {
      if (!CheckOk (handle))
      {
        throw new InvalidOperationException (error);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void HandleException (string description, Exception e)
    {
      StringBuilder builder = new StringBuilder ();

      builder.AppendFormat ("[{0}]: {1}\n", e.GetType (), e.Message);

      if (!string.IsNullOrWhiteSpace (description))
      {
        builder.AppendFormat ("  {0}\n", description);
      }

      builder.AppendFormat ("  Stack trace:\n{0}", e.StackTrace);

      if (e.InnerException != null)
      {
        builder.AppendFormat ("  (Inner) [{0}]: {1}\n", e.InnerException.GetType (), e.InnerException.Message);

        builder.AppendFormat ("  Stack trace:\n{0}", e.InnerException.StackTrace);
      }

      Print (builder.ToString ());

#if DEBUG && false
      System.Diagnostics.Debugger.Break ();
#endif
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void HandleException (Exception e)
    {
      HandleException ("", e);
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
