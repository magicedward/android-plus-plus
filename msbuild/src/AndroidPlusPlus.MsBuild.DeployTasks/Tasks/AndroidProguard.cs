﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.IO;
using System.Reflection;
using System.Resources;

using Microsoft.Build.Framework;
using Microsoft.Win32;
using Microsoft.Build.Utilities;

using AndroidPlusPlus.MsBuild.Common;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.MsBuild.DeployTasks
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class AndroidProguard : TrackedOutOfDateToolTask, ITask
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AndroidProguard ()
      : base (new ResourceManager ("AndroidPlusPlus.MsBuild.DeployTasks.Properties.Resources", Assembly.GetExecutingAssembly ()))
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [Required]
    public string ProguardJar { get; set; }

    private Dictionary<string, ITaskItem> m_qualifiedOutputJars = new Dictionary<string, ITaskItem> ();

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override int TrackedExecuteTool (string pathToTool, string responseFileCommands, string commandLineCommands)
    {
      int retCode = -1;

      try
      {
        retCode = base.TrackedExecuteTool (pathToTool, responseFileCommands, commandLineCommands);

        // 
        // Evaluate a distinct (unique) list of registered output jar files.
        // 

        foreach (ITaskItem source in OutOfDateSources)
        {
          string outJarPath = source.GetMetadata ("OutJars");

          if (!m_qualifiedOutputJars.ContainsKey (outJarPath))
          {
            m_qualifiedOutputJars [outJarPath] = new TaskItem (outJarPath);
          }
        }

        if (m_qualifiedOutputJars.Count == 0)
        {
          throw new ArgumentException ("No valid 'OutJars' evaluated.");
        }

        OutputFiles = new ITaskItem [m_qualifiedOutputJars.Count];

        m_qualifiedOutputJars.Values.CopyTo (OutputFiles, 0);
      }
      catch (Exception e)
      {
        Log.LogErrorFromException (e, true);

        retCode = -1;
      }

      return retCode;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void AddTaskSpecificDependencies (ref TrackedFileManager trackedFileManager, ITaskItem [] sources)
    {
      // 
      // Mark additional dependencies for .class files contained within specified class paths.
      // 

      foreach (ITaskItem source in sources)
      {
        string sourceFullPath = source.GetMetadata ("FullPath");

        if (Directory.Exists (sourceFullPath))
        {
          string [] classPathFiles = Directory.GetFiles (sourceFullPath, "*.class", SearchOption.AllDirectories);

          List<ITaskItem> classPathFileItems = new List<ITaskItem> (classPathFiles.Length);

          foreach (string classpath in classPathFiles)
          {
            classPathFileItems.Add (new TaskItem (classpath));
          }

          trackedFileManager.AddDependencyForSources (classPathFileItems.ToArray (), new ITaskItem [] { source });
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void AddTaskSpecificOutputFiles (ref TrackedFileManager trackedFileManager, ITaskItem [] sources)
    {
      if (m_qualifiedOutputJars.Count > 0)
      {
        ITaskItem [] outputFiles = new ITaskItem [m_qualifiedOutputJars.Count];

        m_qualifiedOutputJars.Values.CopyTo (outputFiles, 0);

        trackedFileManager.AddDependencyForSources (outputFiles, sources);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override string GenerateCommandLineCommands ()
    {
      // 
      // Build a command-line based on parsing switches from the registered property sheet, and any additional flags.
      // 

      StringBuilder builder = new StringBuilder (PathUtils.CommandLineLength);

      // 
      // JavaVM options need to go at the start of the command line.
      // 

      string jvmInitialHeapSize = m_parsedProperties.ParseProperty (Sources [0], "JvmInitialHeapSize");

      string jvmMaximumHeapSize = m_parsedProperties.ParseProperty (Sources [0], "JvmMaximumHeapSize");

      string jvmThreadStackSize = m_parsedProperties.ParseProperty (Sources [0], "JvmThreadStackSize");

      builder.Append (jvmInitialHeapSize + " ");

      builder.Append (jvmMaximumHeapSize + " ");

      builder.Append (jvmThreadStackSize + " ");

      string frameworkDir = Path.GetDirectoryName (ProguardJar);

      builder.Append ("-jar \"" + ProguardJar + "\" ");

      // 
      // Ensure the JVM options aren't duplicated.
      // 

      StringBuilder parsedProperties = new StringBuilder (m_parsedProperties.Parse (Sources [0]));

      if (!string.IsNullOrEmpty (jvmInitialHeapSize))
      {
        parsedProperties.Replace (jvmInitialHeapSize, "");
      }

      if (!string.IsNullOrEmpty (jvmMaximumHeapSize))
      {
        parsedProperties.Replace (jvmMaximumHeapSize, "");
      }

      if (!string.IsNullOrEmpty (jvmThreadStackSize))
      {
        parsedProperties.Replace (jvmThreadStackSize, "");
      }

      builder.Append (parsedProperties.ToString ());

      return builder.ToString ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override string ToolName
    {
      get
      {
        return "AndroidProguard";
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override bool AppendSourcesToCommandLine
    {
      get
      {
        return false;
      }
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
