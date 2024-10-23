using System;
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
    }
}
