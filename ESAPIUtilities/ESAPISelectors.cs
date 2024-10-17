using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using VMS.TPS.Common.Model.API;

namespace ESAPIUtilities
{
    public class ESAPISelectors
    {
        // yes. I know I could reuse some code.
        // I wanted to separate them into their own blocks because
        // a i'm not that good at coding yet 
        // b it's difficult to write generic code in c# and
        // c it's very easy to ctrl-c ctrl-v. sometimes simpler is better ;p

        /// <summary>
        /// Get user input to select courses.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static List<Course> CourseSelector(Patient p)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Please select courses enumerated below in the format \"#\", space-delimited, no trailing whitespace.");
            int courseCounter = 1;
            foreach (Course course in p.Courses)
            {
                sb.AppendLine($"{courseCounter} {course.Id} {course.ClinicalStatus}");
                courseCounter++;
            }

            string selectedCoursesString;
            string pattern = @"^(\d+)( \d+)*$";
            List<Course> selectedCourses = new List<Course>();
            while (true)
            {
                selectedCoursesString = Interaction.InputBox(sb.ToString(), "Select Courses");
                if (Regex.IsMatch(selectedCoursesString, pattern))
                {
                    bool failFlag = false;
                    foreach (string entry in selectedCoursesString.Split(' '))
                    {
                        int courseIndex = Int32.Parse(entry);
                        if (courseIndex - 1 >= 0 && courseIndex - 1 < p.Courses.Count())
                        {
                            Course selectedCourse = p.Courses.ElementAt(courseIndex - 1);
                            selectedCourses.Add(selectedCourse);
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
                else if (selectedCoursesString == "")
                {
                    return selectedCourses;
                }
                else
                {
                    MessageBox.Show("Improperly formatted selection. Ensure that all course selections are of the form # and separated only by spaces.");
                }
            }
            return selectedCourses;
        }

        /// <summary>
        /// Get user input to select plans.
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
        /// Get user input to select beams.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static List<Beam> BeamSelector(Patient p)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Please select beams enumerated below in the format \"#.#\", space-delimited, no trailing whitespace.");
            int courseCounter = 1;
            foreach (Course course in p.Courses)
            {
                int planCounter = 1;
                sb.AppendLine($"{courseCounter} {course.Id} {course.ClinicalStatus}");
                foreach (PlanSetup plan in course.PlanSetups)
                {
                    sb.AppendLine($"- {courseCounter}.{planCounter} {plan.Id}");
                    foreach (Beam beam in plan.Beams)
                    {
                        int beamCounter = 1;
                        sb.AppendLine($"-- {courseCounter}.{planCounter}.{beamCounter} {beam.Id}");
                        beamCounter++;
                    }
                    planCounter++;
                }
                courseCounter++;
            }

            string selectedBeamsString;
            string pattern = @"^(\d+\.\d+\.\d+)( \d+\.\d+\.\d+)*$";
            List<Beam> selectedBeams = new List<Beam>();
            while (true)
            {
                selectedBeamsString = Interaction.InputBox(sb.ToString(), "Select Beams");
                if (Regex.IsMatch(selectedBeamsString, pattern))
                {
                    bool failFlag = false;
                    foreach (var entry in selectedBeamsString.Split(' '))
                    {
                        var indices = entry.Split('.');
                        int courseIndex = Int32.Parse(indices[0]);
                        int planIndex = Int32.Parse(indices[1]);
                        int beamIndex = Int32.Parse(indices[2]);
                        if (courseIndex - 1 >= 0 &&
                            courseIndex - 1 < p.Courses.Count() &&
                            planIndex - 1 >= 0 &&
                            planIndex - 1 < p.Courses.ElementAt(courseIndex - 1).PlanSetups.Count() &&
                            beamIndex - 1 >= 0 &&
                            beamIndex - 1 < p.Courses.ElementAt(courseIndex - 1).PlanSetups.ElementAt(planIndex - 1 ).Beams.Count())
                        {
                            Course selectedCourse = p.Courses.ElementAt(courseIndex - 1);
                            ExternalPlanSetup selectedPlan = selectedCourse.ExternalPlanSetups.ElementAt(planIndex - 1);
                            Beam selectedBeam = selectedPlan.Beams.ElementAt(beamIndex - 1);
                            selectedBeams.Add(selectedBeam);
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
                else if (selectedBeamsString == "")
                {
                    return selectedBeams;
                }
                else
                {
                    MessageBox.Show("Improperly formatted selection. Ensure that all beam selections are of the form #.#.# and separated only by spaces.");
                }
            }
            return selectedBeams;
        }
    }
}
