using System;
using System.Collections.Generic;
using System.Text;

namespace BE_Project
{
    public class BE_Cycle
    {
        double cost;
        int id = 0;
        double qaly;
        int[] statePopulations;
        public BE_Cycle(int IDIn)
        {
            id = IDIn;
            cost = 0;
            qaly = 0;
        }

        #region GETSET
        public int ID
        {
            get { return id; }
        }
        #endregion

        public void SaveStatePopulations(BE_State[] statesIn)
        {
            int numberOfStates = statesIn.Length;
            statePopulations = new int[numberOfStates];
            for (int i = 0; i < numberOfStates; i++)
                statePopulations[i] = statesIn[i].Population;
        }
        public void UpdateCost(double costIn)
        {
            cost += costIn;
        }
        public void UpdateCostAndQalyForThisCycle(BE_Patient[] patientsIn, int ci)
        {
            //  fill in this function if needed
        }
        public override string ToString()
        {
            string temp = "Cycle " + id + ":" + "\n";
            for (int i = 0; i < statePopulations.Length; i++)
                temp += ("State " + i + ": " + statePopulations[i] + " - ");
            temp += "QALY: " + qaly + " Cost: " + cost;

            return temp;
        }
    }
}
