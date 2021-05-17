using System;
using System.Collections.Generic;
using System.Text;

namespace BE_Project
{
    public enum BE_SurveillanceType
    {
        NoSurveillance,
        sEGD
    }
    public class BE_Surveillance
    {
        BE_SurveillanceType type;

        double adherence;
        double cost;
        double mortality;
        double sensitivity;
        double specificity; //  Assumption: Specificity is never used for surveillance.
        double utility;
        double utilityTime;

        int intervalNDBE;
        int intervalLGD;
        double persistentLGD;
        public int[] postIntervalLGD = new int[7];  //  Assumption: Postinterval information is stored in a seven-digit array.
        public int[] postIntervalHGD = new int[7];

        BE_AdverseEvent adverseEvent = new BE_AdverseEvent();
        double totalCostOccured = 0;
        public BE_Surveillance(BE_SurveillanceType typeIn)
        {
            type = typeIn;
            if (type == BE_SurveillanceType.NoSurveillance)
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
        public BE_AdverseEvent AdverseEvent
        {
            get { return adverseEvent; }
        }
        public double Cost
        {
            set { cost = value; }
        }
        public int IntervalLGD
        {
            set { intervalLGD = value; }
        }
        public int IntervalNDBE
        {
            set { intervalNDBE = value; }
        }
        public double Mortality
        {
            set { mortality = value; }
        }
        public double PersistentLGD
        {
            set { persistentLGD = value; }
        }
        public double Sensitivity
        {
            set { sensitivity = value; }
        }
        public double Specificity
        {
            set { specificity = value; }
        }
        public BE_SurveillanceType Type
        {
            get { return type; }
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
        public void SurveillanceDecision(BE_Patient patientIn, BE_Cycle cycleIn, Random rand)   //  Decides whether the patient needs treatment or when is the next surveillance cycle.
        {
            patientIn.Status = BE_PatientStatus.NaturalHistory;
            if (patientIn.ConfirmedState.Type == BE_StateType.NDBE)
            {
                patientIn.NextSurveillance = cycleIn.ID + intervalNDBE;
                patientIn.IsLastSurveillanceLGD = false;    //  two times in a row is required
            }
            else if (patientIn.ConfirmedState.Type == BE_StateType.LGD)
            {
                if (patientIn.IsLastSurveillanceLGD == false)
                {
                    patientIn.NextSurveillance = cycleIn.ID + intervalLGD;
                    patientIn.IsLastSurveillanceLGD = true;
                }
                else
                {
                    if (rand.NextDouble() <= persistentLGD)
                        patientIn.NextTreatment = cycleIn.ID;
                    else
                    {
                        patientIn.NextSurveillance = cycleIn.ID + intervalNDBE;    //  Assumption: If it is not persistent LGD, it is treated as NDBE.
                        patientIn.IsLastSurveillanceLGD = false;
                    }
                }
            }
            else if (patientIn.ConfirmedState.Type == BE_StateType.HGD || patientIn.ConfirmedState.Type == BE_StateType.IMC || patientIn.ConfirmedState.Type == BE_StateType.SC)
            {
                patientIn.NextTreatment = cycleIn.ID;
            }
            else
            {
                //  Shouldn't be here with since there is no natural regression to NoBarretts. But it can happen with false positives.
            }
        }
        public void SurveilPatient(BE_Patient patientIn, BE_Cycle cycleIn, Random rand)
        {
            if (rand.NextDouble() <= adherence) //  The patient comply with surveillance.
            {
                //  Intervention history is added to the patient's records.
                patientIn.interventionHistory.Add(cycleIn.ID, BE_InterventionType.Surveillance);

                //  Cost is recorded on the patient, the surveillance modality, and the cycle.
                patientIn.UpdateCost(cost);
                this.UpdateCost(cost);
                cycleIn.UpdateCost(cost);

                //  Surveillance-related mortality.
                if (rand.NextDouble() <= mortality)
                {
                    patientIn.Die("Surveillance");
                    return; // If the patient dies, the execution of this function is terminated.
                }

                //  Surveillance-related adverse events.
                if (rand.NextDouble() <= adverseEvent.Probability)
                {
                    patientIn.adverseEventHistory.Add(cycleIn.ID, BE_AdverseEventType.Surveillance);

                    patientIn.UpdateCost(adverseEvent.Cost);
                    this.UpdateCost(adverseEvent.Cost);
                    cycleIn.UpdateCost(adverseEvent.Cost);
                }

                if (rand.NextDouble() <= sensitivity)
                {
                    patientIn.ConfirmedState = patientIn.CurrentState;  //  Correct diagnosis of the health state.
                    SurveillanceDecision(patientIn, cycleIn, rand);
                }
                else
                {
                    patientIn.NextSurveillance = cycleIn.ID + intervalNDBE; //  Assumption: In the case of misdiagnosis, it is treated as NDBE.
                }
            }
            else
            {
                patientIn.interventionHistory.Add(cycleIn.ID, BE_InterventionType.SurveillanceNoShow);
                patientIn.NextSurveillance = cycleIn.ID + intervalNDBE; //  Assumption: If there is no adherence, it is treated as NDBE.
            }
        }
        public void FollowUpSurveillanceDecision(BE_Patient patientIn, BE_Cycle cycleIn)
        {
            if (patientIn.ConfirmedState.Type == BE_StateType.IMC || patientIn.ConfirmedState.Type == BE_StateType.SC)
                patientIn.NextTreatment = cycleIn.ID;
            else if (patientIn.ConfirmedState.ID > patientIn.PostEETState.ID && patientIn.CompletedMaxRFA == false && patientIn.FailedEET == false)
                patientIn.NextTreatment = cycleIn.ID;
            else
            {
                if (patientIn.PreEETState.Type == BE_StateType.NDBE || patientIn.PreEETState.Type == BE_StateType.LGD)    //  Assumption: If there is no adherence, the follow-up intervals continue.
                    patientIn.NextSurveillance = cycleIn.ID + postIntervalLGD[Math.Min(patientIn.CountFollowUp, postIntervalLGD.Length - 1)];
                else if (patientIn.PreEETState.Type == BE_StateType.HGD || patientIn.PreEETState.Type == BE_StateType.IMC)
                    patientIn.NextSurveillance = cycleIn.ID + postIntervalHGD[Math.Min(patientIn.CountFollowUp, postIntervalHGD.Length - 1)];
                else
                    Console.WriteLine(new System.ComponentModel.WarningException("Wrong follow-up surveillance!").Message);
            }
        }
        public void FollowUpSurveilPatient(BE_Patient patientIn, BE_Cycle cycleIn, Random rand)
        {
            patientIn.CountFollowUp++;  //  It increases whether the patient complies or not, because the follow-up intervals must be in conjunction with the guideline recommendations.

            if (rand.NextDouble() <= adherence)
            {
                //  Intervention history is added to the patient's records.
                patientIn.interventionHistory.Add(cycleIn.ID, BE_InterventionType.FollowUpSurveillance);

                //  Cost is recorded on the patient, the surveillance modality, and the cycle.
                patientIn.UpdateCost(cost);
                this.UpdateCost(cost);
                cycleIn.UpdateCost(cost);

                //  Surveillance-related mortality.
                if (rand.NextDouble() <= mortality)
                {
                    patientIn.Die("Follow-up surveillance");
                    return; // If the patient dies, the execution of this function is terminated.
                }

                //  Surveillance-related adverse events.
                if (rand.NextDouble() <= adverseEvent.Probability)
                {
                    patientIn.adverseEventHistory.Add(cycleIn.ID, BE_AdverseEventType.FollowUpSurveillance);

                    patientIn.UpdateCost(adverseEvent.Cost);
                    this.UpdateCost(adverseEvent.Cost);
                    cycleIn.UpdateCost(adverseEvent.Cost);
                }

                if (rand.NextDouble() <= sensitivity)  //  Correct diagnosis of the health state.
                {
                    patientIn.ConfirmedState = patientIn.CurrentState;
                    FollowUpSurveillanceDecision(patientIn, cycleIn);
                }
                else
                {
                    //  Assumption: In the case of misdiagnosis, the follow-up intervals continue the same based on patients pre-EET state.
                    if (patientIn.PreEETState.Type == BE_StateType.NDBE || patientIn.PreEETState.Type == BE_StateType.LGD)
                        patientIn.NextSurveillance = cycleIn.ID + postIntervalLGD[Math.Min(patientIn.CountFollowUp, postIntervalLGD.Length - 1)];
                    else if (patientIn.PreEETState.Type == BE_StateType.HGD || patientIn.PreEETState.Type == BE_StateType.IMC)
                        patientIn.NextSurveillance = cycleIn.ID + postIntervalHGD[Math.Min(patientIn.CountFollowUp, postIntervalHGD.Length - 1)];
                    else
                        Console.WriteLine(new System.ComponentModel.WarningException("Wrong follow-up surveillance!").Message);
                }
            }
            else
            {
                patientIn.interventionHistory.Add(cycleIn.ID, BE_InterventionType.FollowUpSurveillanceNoShow);

                //  Assumption: If there is no adherence, the follow-up intervals continue the same based on patients pre-EET state.
                if (patientIn.PreEETState.Type == BE_StateType.NDBE || patientIn.PreEETState.Type == BE_StateType.LGD)
                    patientIn.NextSurveillance = cycleIn.ID + postIntervalLGD[Math.Min(patientIn.CountFollowUp, postIntervalLGD.Length - 1)];
                else if (patientIn.PreEETState.Type == BE_StateType.HGD || patientIn.PreEETState.Type == BE_StateType.IMC)
                    patientIn.NextSurveillance = cycleIn.ID + postIntervalHGD[Math.Min(patientIn.CountFollowUp, postIntervalHGD.Length - 1)];
                else
                    Console.WriteLine(new System.ComponentModel.WarningException("Wrong follow-up surveillance!").Message);
            }
        }
        public void UpdateCost(double costIn)
        {
            totalCostOccured += costIn;
        }
    }
}
