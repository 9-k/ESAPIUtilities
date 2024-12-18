﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using Microsoft.VisualBasic;
using System.Windows.Media.Media3D;
using System.IO;
using System.Threading;
using System.Windows.Automation;

namespace ESAPIUtilities
{
    public class ESAPIUtility
    {
        /// <summary>
        /// Naively iterates over all points in two structures to find the minimum distance between them. Slow.
        /// </summary>
        /// <param name="firstStructure"></param>
        /// <param name="secondStructure"></param>
        /// <returns></returns>
        public static double MinimumStructureDistance(Structure firstStructure, Structure secondStructure)
        {
            double minDistance = double.MaxValue;
            foreach (var firstStructureSurfacePoint in firstStructure.MeshGeometry.Positions)
            {
                foreach (var secondStructureSurfacePoint in secondStructure.MeshGeometry.Positions)
                {
                    double dist = Point3D.Subtract(firstStructureSurfacePoint, secondStructureSurfacePoint).Length;
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                    }
                }
            }
            return minDistance;
        }

        /// <summary>
        /// Gets the amount of the volume (either as a percent of total volume of the structure or absolute in cc) receiving XX% of the prescription dose.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="plan"></param>
        /// <param name="percent"></param>
        /// <param name="vp"></param>
        /// <returns></returns>
        public static double GetVXX(Structure structure, ExternalPlanSetup plan, double percent, VolumePresentation vp)
        {
            DoseValue DVToCheck = new DoseValue(plan.TotalDose.Dose*percent/100.0, plan.TotalDose.Unit);
            return plan.GetVolumeAtDose(structure, DVToCheck, vp);
        }

        public static Dictionary<string, string> SettingsDict(string settingsPathString)
        {
            Dictionary<string, string> settingsDict = new Dictionary<string, string>();
            string settingsPath = Path.GetFullPath(settingsPathString);
            foreach (string line in File.ReadLines(settingsPath))
            {
                var parts = line.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    settingsDict[key] = value;
                }
                else
                {
                    throw new ApplicationException("Malformed settings.txt file. Ensure each line is of of the form *:*?");
                }
            }
            return settingsDict;
        }

        public static void AutoClickOk()
        {
            // Event handler for when a new window opens
            AutomationEventHandler windowOpenedEventHandler = null;
            windowOpenedEventHandler = (sender, e) =>
            {
                try
                {
                    var window = sender as AutomationElement;
                    if (window != null)
                    {
                        // Optionally, filter windows by process ID to ensure you're interacting with the correct application
                        // int targetProcessId = Process.GetCurrentProcess().Id;
                        // if (window.Current.ProcessId != targetProcessId) return;

                        // Find any button within the window
                        var button = window.FindFirst(TreeScope.Descendants,
                            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));

                        if (button != null)
                        {
                            // Invoke the button (simulate a click)
                            var invokePattern = button.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                            invokePattern?.Invoke();
                            Console.WriteLine("Clicked OK button on a dialog.");
                        }
                        else
                        {
                            Console.WriteLine("No button found in the dialog.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling window opened event: {ex.Message}");
                }
            };

            // Subscribe to the WindowOpenedEvent
            Automation.AddAutomationEventHandler(
                WindowPattern.WindowOpenedEvent,
                AutomationElement.RootElement,
                TreeScope.Subtree,
                windowOpenedEventHandler);
        }

    }
}
