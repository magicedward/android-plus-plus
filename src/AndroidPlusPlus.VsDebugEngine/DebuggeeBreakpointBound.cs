﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Interop;
using AndroidPlusPlus.Common;
using AndroidPlusPlus.VsDebugCommon;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugEngine
{
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class DebuggeeBreakpointBound : IDebugBoundBreakpoint2, IDebugBoundBreakpoint3
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public sealed class Enumerator : DebugEnumerator<IDebugBoundBreakpoint2, IEnumDebugBoundBreakpoints2>, IEnumDebugBoundBreakpoints2
    {
      public Enumerator (List<IDebugBoundBreakpoint2> breakpoints)
        : base (breakpoints)
      {
      }
      public Enumerator (IDebugBoundBreakpoint2 [] breakpoints)
        : base (breakpoints)
      {
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public sealed class Event : AsynchronousDebugEvent, IDebugBreakpointBoundEvent2
    {
      private IDebugPendingBreakpoint2 m_pendingBreakpoint;

      private IDebugBoundBreakpoint2 m_boundBreakpoint;

      public Event (IDebugPendingBreakpoint2 pendingBreakpoint, IDebugBoundBreakpoint2 boundBreakpoint)
      {
        m_pendingBreakpoint = pendingBreakpoint;

        m_boundBreakpoint = boundBreakpoint;
      }

      int IDebugBreakpointBoundEvent2.EnumBoundBreakpoints (out IEnumDebugBoundBreakpoints2 ppEnum)
      {
        IDebugBoundBreakpoint2 [] breakpoints = new IDebugBoundBreakpoint2 [] { m_boundBreakpoint };

        ppEnum = new DebuggeeBreakpointBound.Enumerator (breakpoints);

        return Constants.S_OK;
      }

      int IDebugBreakpointBoundEvent2.GetPendingBreakpoint (out IDebugPendingBreakpoint2 ppPendingBP)
      {
        ppPendingBP = m_pendingBreakpoint;

        return Constants.S_OK;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected readonly DebugBreakpointManager m_breakpointManager;

    protected readonly DebuggeeBreakpointPending m_pendingBreakpoint;

    protected readonly DebuggeeCodeContext m_codeContext;

    protected readonly IDebugBreakpointResolution2 m_breakpointResolution;

    protected bool m_breakpointEnabled;

    protected bool m_breakpointDeleted;

    protected uint m_hitCount;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebuggeeBreakpointBound (DebugBreakpointManager breakpointManager, DebuggeeBreakpointPending pendingBreakpoint, DebuggeeCodeContext codeContext)
    {
      if (breakpointManager == null)
      {
        throw new ArgumentNullException ("breakpointManager");
      }

      if (pendingBreakpoint == null)
      {
        throw new ArgumentNullException ("pendingBreakpoint");
      }

      if (codeContext == null)
      {
        throw new ArgumentNullException ("codeContext");
      }

      m_breakpointManager = breakpointManager;

      m_pendingBreakpoint = pendingBreakpoint;

      m_codeContext = codeContext;

      m_breakpointResolution = new DebuggeeBreakpointResolution (m_codeContext, "<bound breakpoint>");

      m_breakpointEnabled = true;

      m_breakpointDeleted = false;

      m_hitCount = 0;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugBoundBreakpoint2 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int Delete ()
    {
      // 
      // Deletes the breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        if (m_breakpointDeleted)
        {
          return Constants.E_BP_DELETED;
        }

        IDebugBreakpointRequest2 breakpointRequest;

        LoggingUtils.RequireOk (m_pendingBreakpoint.GetBreakpointRequest (out breakpointRequest));

        m_breakpointManager.Engine.Broadcast (new DebugEngineEvent.BreakpointUnbound (this), m_breakpointManager.Engine.Program, null);

        m_breakpointDeleted = true;

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int Enable (int fEnable)
    {
      // 
      // Enables or disables the breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      m_breakpointEnabled = (fEnable != 0);

      if (m_breakpointDeleted)
      {
        return Constants.E_BP_DELETED;
      }

      return Constants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int GetBreakpointResolution (out IDebugBreakpointResolution2 ppBPResolution)
    {
      // 
      // Gets the breakpoint resolution that describes this breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      ppBPResolution = m_breakpointResolution;

      if (m_breakpointDeleted)
      {
        return Constants.E_BP_DELETED;
      }

      return Constants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int GetHitCount (out uint pdwHitCount)
    {
      // 
      // Gets the current hit count for this bound breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      pdwHitCount = m_hitCount;

      if (m_breakpointDeleted)
      {
        return Constants.E_BP_DELETED;
      }

      return Constants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int GetPendingBreakpoint (out IDebugPendingBreakpoint2 ppPendingBreakpoint)
    {
      // 
      // Gets the pending breakpoint from which the specified bound breakpoint was created.
      // 

      LoggingUtils.PrintFunction ();

      ppPendingBreakpoint = m_pendingBreakpoint;

      if (m_breakpointDeleted)
      {
        return Constants.E_BP_DELETED;
      }

      return Constants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int GetState (enum_BP_STATE [] pState)
    {
      // 
      // Gets the state of this bound breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        pState [0] = enum_BP_STATE.BPS_NONE;

        if (m_breakpointDeleted)
        {
          pState [0] = enum_BP_STATE.BPS_DELETED;
        }
        else
        {
          pState [0] = (m_breakpointEnabled) ? enum_BP_STATE.BPS_ENABLED : enum_BP_STATE.BPS_DISABLED;
        }

        return Constants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int SetCondition (BP_CONDITION bpCondition)
    {
      // 
      // Sets or changes the condition associated with this bound breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      if (m_breakpointDeleted)
      {
        return Constants.E_BP_DELETED;
      }

      return Constants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int SetHitCount (uint dwHitCount)
    {
      // 
      // Sets the hit count for this bound breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      m_hitCount = dwHitCount;

      if (m_breakpointDeleted)
      {
        return Constants.E_BP_DELETED;
      }

      return Constants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int SetPassCount (BP_PASSCOUNT bpPassCount)
    {
      // 
      // Sets or change the pass count associated with this bound breakpoint.
      // 

      LoggingUtils.PrintFunction ();

      if (m_breakpointDeleted)
      {
        return Constants.E_BP_DELETED;
      }

      return Constants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugBoundBreakpoint3 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual int SetTracepoint (string bpBstrTracepoint, enum_BP_FLAGS bpFlags)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return Constants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #endregion

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
