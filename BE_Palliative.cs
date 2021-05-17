using System;

namespace BE_Project
{
    public class BE_Palliative
    {
        double costAnnual;
        double mortality;
        double utility;
        double utilityTime;

        double totalCostOccured = 0;
        #region GETSET
        public double CostAnnual
        {
            set { costAnnual = value; }
        }
        public double Mortality
        {
            set { mortality = value; }
        }
        public double Utility
        {
            get { return utility; }
            set { utility = value; }
        }
        public double UtilityTime
        {
            get { return utilityTime; }
            set { utilityTime = value; }
        }
        #endregion
        public void GivePalliativeTreatment(BE_Patient patientIn, BE_Cycle cycleIn)
        {
            patientIn.Status = BE_PatientStatus.Palliative;
            patientIn.ActiveNaturalProgression = false; //  When patients receive palliative treatment, they no longer move under natural progression probabilities.
            patientIn.ActiveRecurrenceProbability = false; //  When patients receive palliative treatment, they no longer move under recurrence probabilities.

            patientIn.UnderPalliative = true;   //  Assumption: Patient compliance is 100%.

            //  Intervention history is added to the patient's records.
            patientIn.interventionHistory.Add(cycleIn.ID, BE_InterventionType.Palliative);

            //  Cost is recorded on the patient, the palliative treatment modality, and the cycle.
            patientIn.UpdateCost(costAnnual / 4);   // Assumption: The cycle length is three months, and the palliative treatment cost is provided annually.
            this.UpdateCost(costAnnual / 4);
            cycleIn.UpdateCost(costAnnual / 4);
            
            //  Palliative treatment-related mortality is handled in the mortality part of the code.
            patientIn.NextTreatment = cycleIn.ID + 1;   // Assumption: Patients receive palliative treatment in each cycle.
        }
        public void PalliativeMortality(BE_Patient patientIn, Random rand)
        {
            if (rand.NextDouble() <= mortality)
                patientIn.Die("Palliative");
        }
        public void UpdateCost(double costIn)
        {
            totalCostOccured += costIn;
        }
    }
}
