using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BE_Project
{
    public class BE_EET
    {
        double costPerSession;
        double costRFA;
        int maxTouchUpRFA;
        double mortality;
        int sessionCount;
        double utility;
        double utilityTime;

        public double[,] efficacy;
        public double[] recurrence;
        public double[,] postRecurrence;
        
        double[,] cumulativeEfficacy;
        double[,] cumulativePostRecurrence;

        public BE_AdverseEvent adverseEvent = new BE_AdverseEvent();
        double totalCostOccured = 0;
        public BE_EET(int stateSizeIn)
        {
            efficacy = new double[stateSizeIn, stateSizeIn];
            recurrence = new double[stateSizeIn];
            postRecurrence = new double[stateSizeIn, stateSizeIn];

            cumulativeEfficacy = new double[stateSizeIn, stateSizeIn];
            cumulativePostRecurrence = new double[stateSizeIn, stateSizeIn];
        }
        public void InitializeParameters()  //  To construct two matrices: "cumulativeEfficacy" and "cumulativePostRecurrence".
        {
            for (int i = 0; i < recurrence.Length; i++)
            {
                cumulativeEfficacy[i, 0] = efficacy[i, 0];
                cumulativePostRecurrence[i, 0] = postRecurrence[i, 0];
                for (int j = 1; j < recurrence.Length; j++)
                {
                    cumulativeEfficacy[i, j] = Math.Round(cumulativeEfficacy[i, j - 1] + efficacy[i, j], 3);    //  Math.Round function is used to ensure row sums are equal to one.
                    cumulativePostRecurrence[i, j] = Math.Round(cumulativePostRecurrence[i, j - 1] + postRecurrence[i, j], 3);
                }
            }
        }
        #region GETSET
        public BE_AdverseEvent AdverseEvent
        {
            get { return adverseEvent; }
        }
        public double CostPerSession
        {
            set { costPerSession = value; }
        }
        public double CostRFA
        {
            set { costRFA = value; }
        }
        public int MaxTouchUpRFA
        {
            set { maxTouchUpRFA = value; }
        }
        public double Mortality
        {
            set { mortality = value; }
        }
        public int SessionCount
        {
            set { sessionCount = value; }
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
        public int FindEfficacyState(int IDIn, Random rand)
        {
            double d = rand.NextDouble();
            int index = -1;
            for (int i = 0; i < cumulativeEfficacy.GetLength(1); i++)
            {
                if (d <= cumulativeEfficacy[IDIn, i])
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
        public int FindRecurrenceState(int IDIn, Random rand)
        {
            double d = rand.NextDouble();
            int index = -1;
            for (int i = 0; i < cumulativePostRecurrence.GetLength(1); i++)
            {
                if (d <= cumulativePostRecurrence[IDIn, i])
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
        public void GiveEETSession(BE_Patient patientIn, BE_Cycle cycleIn, BE_State[] statesIn, Random rand)
        {
            if (patientIn.CountEET == 0)
            {
                patientIn.Status = BE_PatientStatus.EET;
                patientIn.PreEETState = patientIn.ConfirmedState;
                patientIn.RealPreEETState = patientIn.CurrentState;
                patientIn.ActiveNaturalProgression = false;
            }
            patientIn.CountEET++;   //  Assumption: Patient compliance is 100%.

            //  Intervention history is added to the patient's records.
            patientIn.interventionHistory.Add(cycleIn.ID, BE_InterventionType.EET);

            //  Cost is recorded on the patient, the EET modality, and the cycle.
            patientIn.UpdateCost(costPerSession);
            this.UpdateCost(costPerSession);
            cycleIn.UpdateCost(costPerSession);

            //  EET-related mortality.
            if (rand.NextDouble() <= mortality)
            {
                patientIn.Die("EET_Mortality");
                return; // If the patient dies, the execution of this function is terminated.
            }

            //  EET-related adverse events.
            if (rand.NextDouble() <= adverseEvent.Probability)
            {
                patientIn.adverseEventHistory.Add(cycleIn.ID, BE_AdverseEventType.EET);

                patientIn.UpdateCost(adverseEvent.Cost);
                this.UpdateCost(adverseEvent.Cost);
                cycleIn.UpdateCost(adverseEvent.Cost);
            }

            if (patientIn.CountEET == sessionCount)
            {
                int nextStateID = FindEfficacyState(patientIn.RealPreEETState.ID, rand);
                patientIn.MoveEETEfficacy(statesIn[nextStateID], cycleIn);
            }
            else
                patientIn.NextTreatment = cycleIn.ID + 2;   //  Assumption: EET sessions are held at six-month intervals.
        }
        public void GiveTouchUpRFA(BE_Patient patientIn, BE_Cycle cycleIn, BE_State[] statesIn, Random rand)
        {          
            patientIn.CountRFA++;   // Assumption: Patient compliance is 100 %.
            if (patientIn.CountRFA == maxTouchUpRFA)
                patientIn.CompletedMaxRFA = true;
            
            patientIn.CountFollowUp = 0;    //  The patient's follow-up intervals are reset.

            patientIn.PreEETState = patientIn.ConfirmedState;
            patientIn.RealPreEETState = patientIn.CurrentState;
            patientIn.ActiveNaturalProgression = false;

            //  Intervention history is added to the patient's records.
            patientIn.interventionHistory.Add(cycleIn.ID, BE_InterventionType.RFA);

            //  Cost is recorded on the patient, the EET modality, and the cycle.
            patientIn.UpdateCost(costRFA);
            this.UpdateCost(costRFA);
            cycleIn.UpdateCost(costRFA);

            //  EET-related mortality.
            if (rand.NextDouble() <= mortality)
            {
                patientIn.Die("RFA_Mortality");
                return; // If the patient dies, the execution of this function is terminated.
            }

            //  EET-related adverse events.
            if (rand.NextDouble() <= adverseEvent.Probability)
            {
                patientIn.adverseEventHistory.Add(cycleIn.ID, BE_AdverseEventType.RFA);

                patientIn.UpdateCost(adverseEvent.Cost);
                this.UpdateCost(adverseEvent.Cost);
                cycleIn.UpdateCost(adverseEvent.Cost);
            }

            int nextStateID = FindEfficacyState(patientIn.RealPreEETState.ID, rand);
            patientIn.MoveEETEfficacy(statesIn[nextStateID], cycleIn);
        }
        public void UpdateCost(double costIn)
        {
            totalCostOccured += costIn;
        }
    }
}
