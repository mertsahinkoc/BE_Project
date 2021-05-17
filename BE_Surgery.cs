using System;

namespace BE_Project
{
    public class BE_Surgery
    {
        double costAnnualPostCare;
        double costChemotherapy;
        double costSurgery;

        double chemotherapyRatioSC;
        double mortalityIMC;
        double mortalitySC;
        double surgicalIMC;
        double treatableSC;

        double utility;
        double utilityTime;
        double utilityChemotherapy;
        double utilityChemotherapyTime;
        double utilityPostSurgery;
        double utilityPostSurgeryTime;

        public double[] postMortalityT1a = new double[3];    // Three values for years: 0, 1, 2+
        public double[] postMortalityT1b = new double[3];    // Three values for years: 0, 1, 2+
        public double[] postMortalitySC = new double[3];    // Three values for years: 0, 1, 2+

        public BE_AdverseEvent adverseEvent = new BE_AdverseEvent();
        double totalCostOccured = 0;

        #region GETSET
        public BE_AdverseEvent AdverseEvent
        {
            get { return adverseEvent; }
        }
        public double ChemotherapyRatioSC
        {
            set { chemotherapyRatioSC = value; }
        }
        public double CostAnnualPostCare
        {
            set { costAnnualPostCare = value; }
        }
        public double CostChemotherapy
        {
            set { costChemotherapy = value; }
        }
        public double CostSurgery
        {
            set { costSurgery = value; }
        }
        public double MortalityIMC
        {
            set { mortalityIMC = value; }
        }
        public double MortalitySC
        {
            set { mortalitySC = value; }
        }
        public double SurgicalIMC
        {
            set { surgicalIMC = value; }
        }
        public double TreatableSC
        {
            set { treatableSC = value; }
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
        public double UtilityChemotherapy
        {
            get { return utilityChemotherapy; }
            set { utilityChemotherapy = value; }
        }
        public double UtilityChemotherapyTime
        {
            get { return utilityChemotherapyTime; }
            set { utilityChemotherapyTime = value; }
        }
        public double UtilityPostSurgery
        {
            get { return utilityPostSurgery; }
            set { utilityPostSurgery = value; }
        }
        public double UtilityPostSurgeryTime
        {
            get { return utilityPostSurgeryTime; }
            set { utilityPostSurgeryTime = value; }
        }
        #endregion
        public string DeterminePreSurgeryStateName(BE_Patient patientIn, Random rand)
        {
            if (patientIn.ConfirmedState.Type == BE_StateType.SC)
            {
                return "SC";
            }
            else if (patientIn.ConfirmedState.Type == BE_StateType.IMC)
            {
                if (patientIn.CompletedEET == false)   //  If surgery is the first treatment the patient receives, the health state should be T1b.
                    return "T1b";
                else if (rand.NextDouble() <= surgicalIMC)
                    return "T1b";
                else
                    return "T1a";
            }
            else
            {
                Console.WriteLine(new System.ComponentModel.WarningException("Cannot DeterminePreSurgeryStateName!").Message);
                return null;
            }

        }
        public bool IsSuitableForSurgery(BE_Patient patientIn, Random rand)
        {
            if (patientIn.ConfirmedState.Type == BE_StateType.IMC)
            {
                if (rand.NextDouble() <= surgicalIMC)
                    return true;
            }
            else if (patientIn.ConfirmedState.Type == BE_StateType.SC)
            {
                if (rand.NextDouble() <= treatableSC)
                    return true;
            }
            else
                Console.WriteLine(new System.ComponentModel.WarningException("Cannot decide IsSuitableForSurgery!").Message);
            return false;
        }
        public void PerformSurgery(BE_Patient patientIn, BE_Cycle cycleIn, Random rand)
        {
            patientIn.Status = BE_PatientStatus.Surgery;
            patientIn.ActiveNaturalProgression = false; //  When patients undergo surgery, they no longer move under natural progression probabilities.
            patientIn.ActiveRecurrenceProbability = false; //  When patients undergo surgery, they no longer move under recurrence probabilities.

            patientIn.HasSurgery = true;   //  Assumption: Patient compliance is 100%.

            patientIn.PreSurgeryStateName = DeterminePreSurgeryStateName(patientIn, rand);    //  The pre-surgery state of the patient is recorded.
            patientIn.SurgeryCycle = cycleIn.ID;    //  The cycle in which the patient undergoes surgery is recorded.

            //  Intervention history is added to the patient's records.
            patientIn.interventionHistory.Add(cycleIn.ID, BE_InterventionType.Surgery);

            //  Cost is recorded on the patient, the surgery modality, and the cycle.
            patientIn.UpdateCost(costSurgery);
            this.UpdateCost(costSurgery);
            cycleIn.UpdateCost(costSurgery);

            //  Surgery-related mortality.
            double mortality = 0;
            if (patientIn.ConfirmedState.Type == BE_StateType.IMC)
                mortality = mortalityIMC;
            else
                mortality = mortalitySC;
            if (rand.NextDouble() < mortality)
            {
                patientIn.Die("Surgery_Mortality");
                return; // If the patient dies, the execution of this function is terminated.
            }

            //  Surgery-related adverse events.
            if (rand.NextDouble() <= adverseEvent.Probability)
            {
                patientIn.adverseEventHistory.Add(cycleIn.ID, BE_AdverseEventType.Surgery);

                patientIn.UpdateCost(adverseEvent.Cost);
                this.UpdateCost(adverseEvent.Cost);
                cycleIn.UpdateCost(adverseEvent.Cost);
            }

            //  Chemotherapy for some of the patients with SC.
            if (patientIn.ConfirmedState.Type == BE_StateType.SC)
            {
                if (rand.NextDouble() <= chemotherapyRatioSC)
                {
                    patientIn.interventionHistory.Add(cycleIn.ID, BE_InterventionType.Chemotherapy);

                    patientIn.UpdateCost(costChemotherapy);
                    this.UpdateCost(costChemotherapy);
                    cycleIn.UpdateCost(costChemotherapy);
                }
            }
            patientIn.NextTreatment = cycleIn.ID + 1;   // Assumption: Patients receive postsurgery care in each cycle.
        }
        public void PostSurgeryCare(BE_Patient patientIn, BE_Cycle cycleIn)
        {
            //  Intervention history is added to the patient's records.
            patientIn.interventionHistory.Add(cycleIn.ID, BE_InterventionType.PostSurgery);

            //  Cost is recorded on the patient, the surgery modality, and the cycle.
            patientIn.UpdateCost(costAnnualPostCare / 4);
            this.UpdateCost(costAnnualPostCare / 4);
            cycleIn.UpdateCost(costAnnualPostCare / 4);

            patientIn.NextTreatment = cycleIn.ID + 1;   // Assumption: Patients receive postsurgery care in each cycle.
        }
        public void PostSurgeryMortality(BE_Patient patientIn, BE_Cycle cycleIn, Random rand)
        {
            int yearDifference = (cycleIn.ID - patientIn.SurgeryCycle) / 4; //  Assumption: Cycle length is three months.
            double mortality = 0;
            if (patientIn.PreSurgeryStateName == "T1a")
                mortality = postMortalityT1a[Math.Min(yearDifference, postMortalityT1a.Length - 1)];
            else if (patientIn.PreSurgeryStateName == "T1b")
                mortality = postMortalityT1b[Math.Min(yearDifference, postMortalityT1b.Length - 1)];
            else if (patientIn.PreSurgeryStateName == "SC")
                mortality = postMortalitySC[Math.Min(yearDifference, postMortalitySC.Length - 1)];
            else
                Console.WriteLine(new System.ComponentModel.WarningException("Wrong PostSurgeryMortality!").Message);

            if (rand.NextDouble() <= mortality)
                patientIn.Die("PostSurgery");
        }
        public void UpdateCost(double costIn)
        {
            totalCostOccured += costIn;
        }
    }
}
