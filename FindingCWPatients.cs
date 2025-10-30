using DataBaseStructure;
using DataBaseStructure.AriaBase;
using DataWritingTools;
using DataBaseFileManager;

namespace DataMiningChestwall
{
    public class OutPatient
    {
        public string PatientID { get; set; }
        public string DateTreated { get; set; }
        public string CourseName { get; set; }
        public string Diagnosis { get; set; }
        public string PlanName { get; set; }
        public double DosePerFraction { get; set; }
        public int NumberOfFractions { get; set; }
        public double TotalDose { get; set; }
        public string ArtifactName { get; set; } = "";
        public double ArtifactVolume { get; set; } = 0.0;
        public double ArtifactDose { get; set; } = 0.0;
    }
    class Program
    {
        static List<OutPatient> FindCWpatients(List<PatientClass> patients)
        {
            List<OutPatient> outPatients = new List<OutPatient>();
            foreach (PatientClass patient in patients)
            {
                foreach (CourseClass course in patient.Courses)
                {
                    foreach (TreatmentPlanClass planClass in course.TreatmentPlans)
                    {
                        string planNameLower = planClass.PlanName.ToLower();
                        if (planNameLower.Contains("scl") || planNameLower.Contains("scv")
                            || planNameLower.Contains("lung") || planNameLower.Contains("eso")
                            || planNameLower.Contains("stom") || planNameLower.Contains("panc")
                            || planNameLower.Contains("junc") || planNameLower.Contains("ge")
                            || planNameLower.Contains("abd") || planNameLower.Contains("medi") || planNameLower.Contains("pelv"))
                        {
                            continue;
                        }
                        if (planClass.PlanType == "ExternalBeam")
                        {
                            ExaminationClass referencedExam = patient.Examinations.FirstOrDefault(e => e.ExamName == planClass.Referenced_Exam_Name);
                            if (referencedExam == null)
                            {
                                continue;
                            }
                            foreach (BeamSetClass beamSet in planClass.BeamSets)
                            {
                                PrescriptionClass prescription = beamSet.Prescription;
                                if (prescription == null)
                                {
                                    continue;
                                }
                                bool correct_rx = false;
                                foreach (var target in prescription.PrescriptionTargets)
                                {
                                    if (target.DosePerFraction != 200 || target.NumberOfFractions != 25)
                                    {
                                        continue;
                                    }
                                    correct_rx = true;
                                }
                                if (correct_rx)
                                {
                                    bool has_tumorbed = false;
                                    List<string> roiNames = new List<string>();
                                    foreach (RegionOfInterestDose roiDose in beamSet.FractionDose.DoseROIs)
                                    {
                                        if (roiDose.AbsoluteDose.Count > 0)
                                        {
                                            roiNames.Add(roiDose.Name.ToLower());
                                            if (roiDose.Name.ToLower().Contains("bed"))
                                            {
                                                has_tumorbed = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (has_tumorbed || !roiNames.Contains("heart"))
                                    {
                                        continue;
                                    }
                                    OutPatient outPatient = new OutPatient()
                                    {
                                        PatientID = patient.MRN,
                                        DateTreated = $"{planClass.Review.ReviewTime.Month:D2}" +
                                        $"/{planClass.Review.ReviewTime.Day:D2}" +
                                        $"/{planClass.Review.ReviewTime.Year}",
                                        CourseName = course.Name,
                                        Diagnosis = string.Join(",", course.DiagnosisCodes.Select(d => d.DiagnosisCode.Trim()).ToList()),
                                        PlanName = planClass.PlanName,
                                        DosePerFraction = 200,
                                        NumberOfFractions = planClass.FractionNumber,
                                        TotalDose = 200 * planClass.FractionNumber
                                    };
                                    List<RegionOfInterest> rois = referencedExam.ROIs.Where(r => r.ROI_Material != null).OrderByDescending(r => r.Volume).ToList();
                                    foreach (RegionOfInterest roi in referencedExam.ROIs)
                                    {
                                        RegionOfInterestDose doseRoi = beamSet.FractionDose.DoseROIs.FirstOrDefault(r => r.Name == roi.Name);
                                        if (doseRoi == null)
                                        {
                                            continue;
                                        }
                                        if (doseRoi.AbsoluteDose[0] < 500)
                                        {
                                            continue;
                                        }
                                        if (roi.Type != "")
                                        {
                                            continue;
                                        }
                                        if (roi.ROI_Material != null && roi.ROI_Material.Name == "CustomHU")
                                        {
                                            outPatient.ArtifactName = roi.Name;
                                            outPatient.ArtifactVolume = roi.Volume;
                                            outPatient.ArtifactDose = doseRoi.AbsoluteDose[0];
                                        }
                                    }

                                    outPatients.Add(outPatient);
                                }
                            }
                        }
                    }
                }
            }
            return outPatients;
        }
        static void Main(string[] args)
        {
            string dataDirectory = @"\\ad.ucsd.edu\ahs\CANC\RADONC\BMAnderson\DataBases";
            List<string> jsonFiles = new List<string>();
            jsonFiles = AriaDataBaseJsonReader.ReturnPatientFileNames(@"C:\Users\BRA008\Modular_Projects\LocalDatabases\2025", jsonFiles, "*.json", SearchOption.AllDirectories);
            //jsonFiles = AriaDataBaseJsonReader.ReturnPatientFileNames(@"C:\Users\BRA008\Modular_Projects\LocalDatabases\2024", jsonFiles, "*.json", SearchOption.AllDirectories);
            //jsonFiles = AriaDataBaseJsonReader.ReturnPatientFileNames(@"C:\Users\BRA008\Modular_Projects\LocalDatabases\2023", jsonFiles, "*.json", SearchOption.AllDirectories);
            //jsonFiles = AriaDataBaseJsonReader.ReturnPatientFileNames(@"C:\Users\BRA008\Modular_Projects\LocalDatabases\2022", jsonFiles, "*.json", SearchOption.AllDirectories);
            List<PatientClass> allPatients = new List<PatientClass>();
            allPatients = AriaDataBaseJsonReader.ReadPatientFiles(jsonFiles);
            var cylinderPatients = FindCWpatients(allPatients);
            string outputCsvPath = Path.Combine(dataDirectory, "ChestWallPatients.csv");
            CsvTools.WriteToCsv<OutPatient>(cylinderPatients, outputCsvPath);
        }
    }
}