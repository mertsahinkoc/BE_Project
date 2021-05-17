using System;
using System.Collections.Generic;
using System.Text;

namespace BE_Project
{
    public enum BE_PatientStatus
    {
        NotScreened,
        NaturalHistory,
        EET,
        Recurrence,
        Surgery,
        Palliative,
        Dead
    }
    public enum BE_InterventionType
    {
        Screening,
        ScreeningNoShow,
        Surveillance,
        SurveillanceNoShow,
        FollowUpSurveillance,
        FollowUpSurveillanceNoShow,
        EET,
        RFA,
        Surgery,
        Chemotherapy,
        PostSurgery,
        Palliative
    }
    public enum BE_AdverseEventType
    {
        Surveillance,
        FollowUpSurveillance,
        EET,
        RFA,
        Surgery
    }
    public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
    {
        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);
            if (result == 0) { return -1; }   // Handle equality as being smaller. The second key is greater.
            return result;
        }
    }
    public class BE_Patient
    {
        int id;
        string gender;
        int age;
        BE_PatientStatus status = BE_PatientStatus.NotScreened;

        BE_State currentState;
        BE_State confirmedState;
        BE_State preEETState;
        BE_State postEETState;
        BE_State realPreEETState;

        bool activeNaturalProgression = true;
        bool activeRecurrenceProbability = false;
        bool hasPositiveTest = false;
        bool completedEET = false;
        bool completedMaxRFA = false;
        bool isDead = false;
        bool isLastSurveillanceLGD = false;
        bool needPostEETDecision = false;
        bool hasSurgery = false;
        bool underPalliative = false;
        string preSurgeryStateName;

        public SortedList<int, BE_StateType> stateHistory = new SortedList<int, BE_StateType>();
        public SortedList<int, BE_PatientStatus> patientStatusHistory = new SortedList<int, BE_PatientStatus>();
        public SortedList<int, BE_InterventionType> interventionHistory = new SortedList<int, BE_InterventionType>(new DuplicateKeyComparer<int>());
        public SortedList<int, BE_AdverseEventType> adverseEventHistory = new SortedList<int, BE_AdverseEventType>(new DuplicateKeyComparer<int>());
        public List<BE_ScreeningResult> screenResult = new List<BE_ScreeningResult>();

        double costPaid = 0;
        int countEET = 0;
        int countFollowUp = 0;
        int countRFA = 0;
        bool concludedAsFailedEET = false;
        int nextTreatment = -1;
        int nextSurveillance = -1;
        int surgeryCycle = -1;

        int deathAge;
        string deathCause;
        double qaly = 0;
        public BE_Patient(int patientIDIn, BE_State stateIn)
        {
            id = patientIDIn;
            gender = "Male";
            age = 50;

            currentState = stateIn;
            stateHistory.Add(0, currentState.Type);
        }
        #region GETSET
        public bool ActiveNaturalProgression
        {
            get { return activeNaturalProgression; }
            set { activeNaturalProgression = value; }
        }
        public bool ActiveRecurrenceProbability
        {
            get { return activeRecurrenceProbability; }
            set { activeRecurrenceProbability = value; }
        }
        public int Age
        {
            get { return age; }
            set { age = value; }
        }
        public bool CompletedEET
        {
            get { return completedEET; }
            set { completedEET = value; }
        }
        public bool CompletedMaxRFA
        {
            get { return completedMaxRFA; }
            set { completedMaxRFA = value; }
        }
        public BE_State ConfirmedState
        {
            get { return confirmedState; }
            set { confirmedState = value; }
        }
        public int CountEET
        {
            get { return countEET; }
            set { countEET = value; }
        }
        public int CountFollowUp
        {
            get { return countFollowUp; }
            set { countFollowUp = value; }
        }
        public int CountRFA
        {
            get { return countRFA; }
            set { countRFA = value; }
        }
        public BE_State CurrentState
        {
            get { return currentState; }
            set { currentState = value; }
        }
        public int DeathAge
        {
            get { return deathAge; }
            set { deathAge = value; }
        }
        public string DeathCause
        {
            get { return deathCause; }
            set { deathCause = value; }
        }
        public bool FailedEET
        {
            get { return concludedAsFailedEET; }
            set { concludedAsFailedEET = value; }
        }
        public string Gender
        {
            get { return gender; }
            set { gender = value; }
        }
        public bool HasPositiveTest
        {
            get { return hasPositiveTest; }
            set { hasPositiveTest = value; }
        }
        public bool HasSurgery
        {
            get { return hasSurgery; }
            set { hasSurgery = value; }
        }
        public int ID
        {
            get { return id; }
            set { id = value; }
        }
        public bool IsDead
        {
            get { return isDead; }
            set { isDead = value; }
        }
        public bool IsLastSurveillanceLGD
        {
            get { return isLastSurveillanceLGD; }
            set { isLastSurveillanceLGD = value; }
        }
        public bool NeedPostEETDecision
        {
            get { return needPostEETDecision; }
            set { needPostEETDecision = value; }
        }
        public int NextSurveillance
        {
            get { return nextSurveillance; }
            set { nextSurveillance = value; }
        }
        public int NextTreatment
        {
            get { return nextTreatment; }
            set { nextTreatment = value; }
        }
        public BE_State PostEETState
        {
            get { return postEETState; }
            set { postEETState = value; }
        }
        public BE_State PreEETState
        {
            get { return preEETState; }
            set { preEETState = value; }
        }
        public string PreSurgeryStateName
        {
            get { return preSurgeryStateName; }
            set { preSurgeryStateName = value; }
        }
        public double Qaly
        {
            get { return qaly; }
            set { qaly = value; }
        }
        public BE_State RealPreEETState
        {
            get { return realPreEETState; }
            set { realPreEETState = value; }
        }
        public BE_PatientStatus Status
        {
            get { return status; }
            set { status = value; }
        }
        public int SurgeryCycle
        {
            get { return surgeryCycle; }
            set { surgeryCycle = value; }
        }
        public bool UnderPalliative
        {
            get { return underPalliative; }
            set { underPalliative = value; }
        }
        #endregion
        public void Die(string deathCauseIn)
        {
            status = BE_PatientStatus.Dead;
            activeNaturalProgression = false;
            activeRecurrenceProbability = false;
            isDead = true;
            deathAge = age;
            deathCause = deathCauseIn;
            currentState.Population--;
            currentState.DiedPopulation++;
        }
        public void MoveNaturalProgression(BE_State[] states, BE_Cycle cycleIn, Random rand)
        {
            int newIndex = currentState.FindNextStateID(rand);

            currentState.Population--;
            currentState = states[newIndex];
            stateHistory.Add(cycleIn.ID, currentState.Type);
            currentState.Population++;
        }
        public void MoveRecurrence(BE_EET EETIn, BE_State[] states, BE_Cycle cycleIn, Random rand)
        {
            if (rand.NextDouble() <= EETIn.recurrence[preEETState.ID])
            {
                status = BE_PatientStatus.NaturalHistory;
                activeRecurrenceProbability = false;
                activeNaturalProgression = true;

                int newIndex = EETIn.FindRecurrenceState(preEETState.ID, rand);

                currentState.Population--;
                currentState = states[newIndex];
                stateHistory.Add(cycleIn.ID, currentState.Type);
                currentState.Population++;
            }
            else
            {
                stateHistory.Add(cycleIn.ID, currentState.Type);
            }
        }
        public void MoveEETEfficacy(BE_State nextStateIn, BE_Cycle cycleIn)
        {
            status = BE_PatientStatus.Recurrence;
            completedEET = true;
            activeRecurrenceProbability = true;
            
            postEETState = nextStateIn;
            confirmedState = nextStateIn;   //  Assumption popst EET state is always confirmed.

            currentState.Population--;
            currentState = nextStateIn;
            currentState.Population++;

            needPostEETDecision = true;
            if (postEETState.ID >= realPreEETState.ID)  //  Treatment failure. If in reality NoBarretts, it must be switched to naturel progression.
            {
                status = BE_PatientStatus.NaturalHistory;
                activeRecurrenceProbability = false;
                activeNaturalProgression = true;
            }
            if (postEETState.ID >= preEETState.ID)  //  Treatment failure is observed
            {
                concludedAsFailedEET = true;
            }

        }
        public void UpdateCost (double costIn)
        {
            costPaid += costIn;
        }
    }
}
