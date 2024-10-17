using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VMS.TPS.Common.Model.API;

namespace ESAPIUtilities
{
    public class ESAPIDiff
    {

        /// <summary>
        /// Takes two beams and returns bool, true if the number of control points and the MLC leaf positions between them are identical.
        /// </summary>
        /// <param name="firstBeam"></param>
        /// <param name="secondBeam"></param>
        public static bool LeafDiffer(Beam firstBeam, Beam secondBeam, bool verbose)
        {
            bool incongruityFlag = false;
            ControlPointCollection firstBeamCPs = firstBeam.ControlPoints;
            ControlPointCollection secondBeamCPs = secondBeam.ControlPoints;
            if (firstBeamCPs.Count() != secondBeamCPs.Count()) 
            { 
                if (verbose)
                {
                    MessageBox.Show($"Number of control points between beams {firstBeam.Id} and {secondBeam.Id} differ by {firstBeamCPs.Count() - secondBeamCPs.Count()}.");
                }
                return false; 
            }

            foreach (ControlPoint fbcp in firstBeamCPs)
            {
                int fbcpindex = fbcp.Index;
                ControlPoint sbcp = secondBeamCPs[fbcpindex];
                if (!AreLeafPosArraysEqual(fbcp.LeafPositions, sbcp.LeafPositions))
                {
                    incongruityFlag = true;
                    if (verbose)
                    {
                        MessageBox.Show("Incongruity at CP" + fbcpindex);
                        ShowDifferenceMatrix(CalculateDifferenceMatrix(fbcp.LeafPositions, sbcp.LeafPositions));
                    }
                }
            }
            if (!incongruityFlag && verbose) 
            { 
                MessageBox.Show("No leaf position incongruities detected."); 
            }

            return incongruityFlag;

            //////////////////////// HELPER FUNTIONS BELOW ///////////////////////
            bool AreLeafPosArraysEqual(Single[,] array1, Single[,] array2)
            {
                int rows = array1.GetLength(0);
                int cols = array1.GetLength(1);

                if (rows != array2.GetLength(0) || cols != array2.GetLength(1))
                    return false; // Arrays have different dimensions

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        if (array1[i, j] != array2[i, j])
                            return false;
                    }
                }

                return true; // Arrays are identical
            }

            void ShowDifferenceMatrix(Single[,] differenceMatrix)
            {
                StringBuilder matrixString = new StringBuilder();
                int rows = differenceMatrix.GetLength(0);
                int cols = differenceMatrix.GetLength(1);

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        matrixString.Append(differenceMatrix[i, j].ToString());
                        if (j < cols - 1)
                            matrixString.Append("\t"); // Tab separator for columns
                    }
                    matrixString.AppendLine(); // Newline separator for rows
                }

                MessageBox.Show(matrixString.ToString());
            }

            Single[,] CalculateDifferenceMatrix(Single[,] array1, Single[,] array2)
            {
                int rows = array1.GetLength(0);
                int cols = array1.GetLength(1);

                if (rows != array2.GetLength(0) || cols != array2.GetLength(1))
                    throw new ArgumentException("Arrays must have the same dimensions");

                Single[,] differenceMatrix = new Single[rows, cols];

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        differenceMatrix[i, j] = array1[i, j] - array2[i, j];
                    }
                }

                return differenceMatrix;
            }
        }

        /// <summary>
        /// Finds differences in the properties between beams. Returns a list of strings, each the name of the differing property. 
        /// </summary>
        /// <param name="firstBeam"></param>
        /// <param name="secondBeam"></param>
        /// <param name="verbose"></param>
        /// <returns></returns>
        public static List<string> BeamDiffer(Beam firstBeam, Beam secondBeam, bool verbose)
        {
            List<string> beamPropertyDiffs = new List<string>();
            foreach (PropertyInfo property in firstBeam.GetType().GetProperties())
            {
                try
                {
                    if (property.GetValue(firstBeam).ToString() != property.GetValue(secondBeam).ToString())
                    {
                        if (verbose)
                        {
                            MessageBox.Show($"Discrepancy for property {property.Name}:\nFirst beam: {property.GetValue(firstBeam).ToString()}\nSecond beam: {property.GetValue(secondBeam).ToString()}");
                        }
                        beamPropertyDiffs.Add(property.Name);
                    }
                }
                catch (Exception)
                {
                    if (verbose)
                    {
                        MessageBox.Show("No instance of " + property.Name);
                    }
                }
            }
            return beamPropertyDiffs;
        }
    }
}
