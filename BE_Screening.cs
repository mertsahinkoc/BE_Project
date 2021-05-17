using System;
using System.Collections.Generic;
using System.Text;

namespace BE_Project
{
    public enum BE_ScreeningType
    {
        NoScreening,
        sEGD,
        EXAS,
        hTNE,
        mTNE,
        Cytosponge,
        eVOC
    }
    public enum BE_ScreeningResult
    {
        TruePositive,
        FalsePositive,
        TrueNegative,
        FalseNegative,
        NoShow,
        Death
    }
    public class BE_Screening
    {
        BE_ScreeningType type;

        double adherence;
        double cost;
        double mortality;
        double sensitivity;
        double specificity;

        double totalCostOccured = 0;
        public BE_Screening(BE_ScreeningType typeIn)
        {
            type = typeIn;
            if (type == BE_ScreeningType.NoScreening)
            {
                adherence = 0;
                cost = 0;
                mortality = 0;
                sensitivity = 0;
                specificity = 0;
            }
        }
        #region GETSET
        public double Adherence
        {
            set { adherence = value; }
        }
        public double Cost
        {
            set { cost = value; }
        }
        public double Mortality
        {
            set { mortality = value; }
        }
        public double Sensitivity
        {
            set { sensitivity = value; }
        }
        public double Specificity
        {
            set { specificity = value; }
        }
        public BE_ScreeningType Type
        {
            get { return type; }
        }
        #endregion
        public void ScreenPatient(BE_Patient patientIn, BE_Cycle cycleIn, Random rand)
        {
            //  Six possible outcomes: "true_positive", "false_negative", "true_negative", "false_positive", "death", and "no_show"

            if (rand.NextDouble() <= adherence) //  The patient comply with the screening test.
            {
                //  Intervention history is added to the patient's records.
                patientIn.interventionHistory.Add(cycleIn.ID, BE_InterventionType.Screening);

                //  Cost is recorded on the patient, the screening modality, and the cycle.
                patientIn.UpdateCost(cost);
                this.UpdateCost(cost);
                cycleIn.UpdateCost(cost);

                //  Screening-related mortality.
                if (rand.NextDouble() <= mortality)
                {
                    patientIn.Die("Screening_" + type.ToString());
                    patientIn.screenResult.Add(BE_ScreeningResult.Death);
                    return; // If the patient dies, the execution of this function is terminated.
                }

                //  Screening-related adverse events.
                //  missing AE
                
                if (patientIn.CurrentState.Type != BE_StateType.NoBarretts) //  The patient is not in the NoBarretts state, it means the patient has the disease.
                {
                    if (rand.NextDouble() <= sensitivity)
                    {
                        patientIn.HasPositiveTest = true;
                        patientIn.screenResult.Add(BE_ScreeningResult.TruePositive);   //  Correct diagnosis of the disease.
                    }
                    else
                    {
                        patientIn.screenResult.Add(BE_ScreeningResult.FalseNegative);   //  Wrong diagnosis of the disease.
                    }
                }
                else     //  The patient is in the NoBarretts state, it means the patient is healthy.
                {
                    if (rand.NextDouble() <= specificity)
                    {
                        patientIn.screenResult.Add(BE_ScreeningResult.TrueNegative);   //  Correct diagnosis for the healthy patient.
                    }
                    else
                    {
                        patientIn.HasPositiveTest = true;
                        patientIn.screenResult.Add(BE_ScreeningResult.FalsePositive);   //  Wrong diagnosis for the healthy patient.
                    }
                }
            }
            else    //  The patient does not comply with the screening test.
            {
                patientIn.interventionHistory.Add(cycleIn.ID, BE_InterventionType.ScreeningNoShow);
                patientIn.screenResult.Add(BE_ScreeningResult.NoShow);
            }
        }
        public void UpdateCost(double costIn)
        {
            totalCostOccured += costIn;
        }
    }
}
