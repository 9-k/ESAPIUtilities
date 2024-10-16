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

namespace ESAPIUtilities
{
    public class ESAPIUtility
    {
        /// <summary>
        /// Get user input for which plans corresponding verification plans must be made.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static List<ExternalPlanSetup> PlanSelector(Patient p)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Please select plans enumerated below in the format \"#.#\", space-delimited, no trailing whitespace.");
            int courseCounter = 1;
            foreach (Course course in p.Courses)
            {
                int planCounter = 1;
                sb.AppendLine($"{courseCounter} {course.Id} {course.ClinicalStatus}");
                foreach (PlanSetup plan in course.PlanSetups)
                {
                    sb.AppendLine($"- {courseCounter}.{planCounter} {plan.Id}");
                    planCounter++;
                }
                courseCounter++;
            }

            string selectedPlansString;
            string pattern = @"^(\d+\.\d+)( \d+\.\d+)*$";
            List<ExternalPlanSetup> selectedPlans = new List<ExternalPlanSetup>();
            while (true)
            {
                selectedPlansString = Interaction.InputBox(sb.ToString(), "Select Plans");
                if (Regex.IsMatch(selectedPlansString, pattern))
                {
                    bool failFlag = false;
                    foreach (var entry in selectedPlansString.Split(' '))
                    {
                        var indices = entry.Split('.');
                        int courseIndex = Int32.Parse(indices[0]);
                        int planIndex = Int32.Parse(indices[1]);
                        if (courseIndex - 1 >= 0 && courseIndex - 1 < p.Courses.Count() &&
                            planIndex - 1 >= 0 && planIndex - 1 < p.Courses.ElementAt(courseIndex - 1).PlanSetups.Count())
                        {
                            Course selectedCourse = p.Courses.ElementAt(courseIndex - 1);
                            ExternalPlanSetup selectedPlan = selectedCourse.ExternalPlanSetups.ElementAt(planIndex - 1);
                            selectedPlans.Add(selectedPlan);
                        }
                        else
                        {
                            MessageBox.Show($"Index out of bounds for entry {entry}. Please try again.");
                            failFlag = true;
                        }
                    }
                    if (failFlag) { continue; }
                    break;
                }
                else if (selectedPlansString == "")
                {
                    return selectedPlans;
                }
                else
                {
                    MessageBox.Show("Improperly formatted selection. Ensure that all plan selections are of the form #.# and separated only by spaces.");
                }
            }
            return selectedPlans;
        }

        /// <summary>
        /// Iterates over verification plans and creates a verification plan for each.
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="selectedPlans"></param>
        /// <param name="QAcourseId"></param>
        /// <param name="QADetails"></param>
        /// <param name="calcOptions"></param>
        /// <exception cref="ApplicationException"></exception>
        public static void CreateVerificationPlans(Patient patient,
                                                   List<ExternalPlanSetup> selectedPlans,
                                                   string QAcourseId,
                                                   Dictionary<string, Dictionary<string, string>> QADetails,
                                                   Dictionary<string, Dictionary<string, string>> calcOptions)
        {
            foreach (ExternalPlanSetup plan in selectedPlans)
            {
                string model = plan.PhotonCalculationModel;
                if (!QADetails.ContainsKey(model)) { throw new ApplicationException($"Plan {plan.Name}'s calculation model {model} doesn't have a QA structure set defined. Modify the code to create the necessary behavior and recompile."); }
                string QAPatientID = QADetails[model]["QAPatientID"];
                string QAStudyID = QADetails[model]["QAStudyID"];
                string QAImageID = QADetails[model]["QAImageID"];

                Course QAcourse = patient.Courses.Where(o => o.Id == QAcourseId).SingleOrDefault();
                if (QAcourse == null) // if there isn't a qa course, make one.
                {
                    QAcourse = patient.AddCourse();
                    QAcourse.Id = QAcourseId;
                }
                else // only if a prior qa course does exist
                {
                    if (!QAcourse.ClinicalStatus.Equals(CourseClinicalStatus.Active)) // anything other than active
                    {
                        throw new ApplicationException("QA course is not active. Please activate QA course.");
                    }
                }

                StructureSet ssQA = patient.CopyImageFromOtherPatient(QAPatientID, QAStudyID, QAImageID);

                ExternalPlanSetup verificationPlan = CreateVerificationPlan(QAcourse, plan, ssQA, calcOptions);

                PlanSetup verifiedPlan = verificationPlan.VerifiedPlan;
                if (plan != verifiedPlan) { throw new ApplicationException($"Error: verified plan {verifiedPlan.Id} != loaded plan {plan.Id}!"); }
            }
        }

        /// <summary>
        /// Create verifications plans for a given treatment plan. This was mostly edited from LDClark's CreateVerificationPlan.
        /// </summary>
        /// <param name="course"></param>
        /// <param name="verifiedPlan"></param>
        /// <param name="verificationStructures"></param>
        /// <param name="calcOptions"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ApplicationException"></exception>
        public static ExternalPlanSetup CreateVerificationPlan(Course course,
                                                               ExternalPlanSetup verifiedPlan,
                                                               StructureSet verificationStructures,
                                                               Dictionary<string, Dictionary<string, string>> calcOptions)
        {
            var verificationPlan = course.AddExternalPlanSetupAsVerificationPlan(verificationStructures, verifiedPlan);

            int QANum = course.ExternalPlanSetups
            .Where(eps => eps.Id.Contains("_QA"))
            .Select(eps =>
            {
                // Split by "_QA" as a string array and take the numeric part after it
                string qaNumberPart = eps.Id.Split(new string[] { "_QA" }, StringSplitOptions.None).Last();
                int.TryParse(qaNumberPart, out int qaNum);
                return qaNum;
            })
            .DefaultIfEmpty(0)
            .Max() + 1;

            // rename plan with incremented counter
            course.ExternalPlanSetups.Where(o => o.Id.Equals(verificationPlan.Id)).SingleOrDefault();
            verificationPlan.Id = verifiedPlan.Id.Substring(0, Math.Min((10 - QANum.ToString().Length), verifiedPlan.Id.Length)) + "_QA" + QANum.ToString();

            // Put isocenter to the center of the QAdevice
            VVector isocenter = verificationPlan.StructureSet.Image.UserOrigin;
            var verifiedBeams = verifiedPlan.Beams.ToList(); //used for looping later
            foreach (Beam verifiedBeam in verifiedPlan.Beams)
            {
                if (verifiedBeam.IsSetupField) continue;

                Beam verificationBeam;

                string PrimaryFluenceModelId = string.Empty;
                if (verifiedBeam.EnergyModeDisplayName.Contains("FFF")) PrimaryFluenceModelId = "FFF";
                if (verifiedBeam.EnergyModeDisplayName.Contains("SRS")) PrimaryFluenceModelId = "SRS";
                ExternalBeamMachineParameters MachineParameters =
                    new ExternalBeamMachineParameters(verifiedBeam.TreatmentUnit.Id, 
                                                      verifiedBeam.EnergyModeDisplayName, 
                                                      verifiedBeam.DoseRate, 
                                                      verifiedBeam.Technique.Id, 
                                                      PrimaryFluenceModelId);

                double collimatorAngle = verifiedBeam.ControlPoints.First().CollimatorAngle;
                double gantryAngle = verifiedBeam.ControlPoints.First().GantryAngle;
                IEnumerable<double> metersetWeights = verifiedBeam.ControlPoints.Select(cp => cp.MetersetWeight);
                GantryDirection gantryDirection = verifiedBeam.GantryDirection;

                if (verifiedBeam.MLCPlanType.ToString() == "VMAT")
                {
                    // Create a new VMAT verificationBeam.
                    double gantryAngleEnd = verifiedBeam.ControlPoints.Last().GantryAngle;
                    verificationBeam = verificationPlan.AddVMATBeam(MachineParameters, 
                        metersetWeights, collimatorAngle, gantryAngle,
                        gantryAngleEnd, gantryDirection, 0.0, isocenter);

                }
                else if (verifiedBeam.MLCPlanType.ToString() == "DoseDynamic")
                {
                    // Create a new IMRT verificationBeam.
                    verificationBeam = verificationPlan.AddSlidingWindowBeam(MachineParameters, metersetWeights, collimatorAngle, gantryAngle,
                        0.0, isocenter);
                }
                else
                {
                    var message = string.Format("Treatment field {0} is not VMAT or IMRT.", verifiedBeam);
                    throw new Exception(message);
                }

                verificationBeam.Comment = verifiedBeam.Comment;
                verificationBeam.Id = verifiedBeam.Id;
                verificationBeam.Name = verifiedBeam.Name;
                if (verifiedBeam.GetOptimalFluence() != null) verificationBeam.SetOptimalFluence(verifiedBeam.GetOptimalFluence());
                verificationBeam.ApplyParameters(verifiedBeam.GetEditableParameters());
            }

            verificationPlan.PlanNormalizationValue = verifiedPlan.PlanNormalizationValue;

            // Set presciption
            const int numberOfFractions = 1;
            verificationPlan.SetPrescription(numberOfFractions, verifiedPlan.DosePerFraction, verifiedPlan.TreatmentPercentage);

            verificationPlan.SetCalculationModel(CalculationType.PhotonVolumeDose, verifiedPlan.GetCalculationModel(CalculationType.PhotonVolumeDose));
            foreach (KeyValuePair<String, String> verifiedPlanCalcOptionAndValue in verifiedPlan.GetCalculationOptions(verifiedPlan.PhotonCalculationModel))
            {
                verificationPlan.SetCalculationOption(verificationPlan.PhotonCalculationModel, verifiedPlanCalcOptionAndValue.Key, verifiedPlanCalcOptionAndValue.Value);
            }
            if (calcOptions.ContainsKey(verificationPlan.GetCalculationModel(CalculationType.PhotonVolumeDose)))
            {
                foreach (var ModelAndOptionValuePairPair in calcOptions)
                {
                    string model = ModelAndOptionValuePairPair.Key;
                    foreach (var OptionValuePair in ModelAndOptionValuePairPair.Value)
                    {
                        string option = OptionValuePair.Key;
                        string value = OptionValuePair.Value;
                        // SetCalculationOption returns false if it can't find the setting to set, and we want that to throw an error because we can't have that slip by.
                        bool optionSetResult = verificationPlan.SetCalculationOption(model, option, value);
                        if (!optionSetResult) { throw new ApplicationException($"Couldn't set setting {model},{option},{value}. Exiting."); }
                    }
                }
            }

            CalculationResult res;
            if (verificationPlan.Beams.FirstOrDefault().MLCPlanType.ToString() == "DoseDynamic") //imrt
            {
                var getCollimatorAndGantryAngleFromBeam = verifiedPlan.Beams.Count() > 1;
                var presetValues = (from beam in verifiedPlan.Beams
                                    select new KeyValuePair<string, MetersetValue>(beam.Id, beam.Meterset)).ToList();
                res = verificationPlan.CalculateDoseWithPresetValues(presetValues);
            }
            else if (verificationPlan.Beams.FirstOrDefault().MLCPlanType.ToString() == "VMAT")
            {
                res = verificationPlan.CalculateDose();
            }
            else
            {
                throw new ApplicationException("Are you trying to calculate dose for a plan that's not IMRT or VMAT?");
            }
            if (!res.Success)
            {
                var message = string.Format("Dose calculation failed for verification plan. Output:\n{0}", res);
                throw new Exception(message);
            }
            return verificationPlan;
        }

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
    }
}
