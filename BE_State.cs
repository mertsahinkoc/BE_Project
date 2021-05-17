using System;
using System.Collections.Generic;
using System.Linq;

namespace BE_Project
{
    public enum BE_StateType    //  missing
    {
        NoBarretts,
        NDBE,
        LGD,
        HGD,
        IMC,
        SC
    }
    public class BE_State
    {
        int id = -1;
        BE_StateType type;
        double utility = -1;
        double initialPrevalence = -1;
        
        public double[] transitionProbability; //  From this state to others
        double[] cumulativeProbability; //  From this state to others

        int population = 0;
        public List<int> populationHistory = new List<int>();  // Stores population size for each cycle.
        int diedPopulation = 0;

        public BE_State(BE_StateType typeIn, int numberOfStates)
        {
            type = typeIn;
            id = (int)type;

            transitionProbability = new double[numberOfStates];
            cumulativeProbability = new double[numberOfStates];
        }
        public void InitializeParameters()  //  To construct two arrays: "transitionProbability" and "cumulativeProbability".
        {
            double totalProb = transitionProbability.Sum();
            if (totalProb < 0 || totalProb > 1)
                Console.WriteLine(new System.ComponentModel.WarningException("Sum of transition probabilities not between 0 and 1!").Message);

            transitionProbability[id] = 1 - totalProb;

            cumulativeProbability[0] = transitionProbability[0];
            for (int i = 1; i < transitionProbability.Length; i++)
                cumulativeProbability[i] += cumulativeProbability[i - 1] + transitionProbability[i];
        }
        #region GETSET
        public int DiedPopulation
        {
            get { return diedPopulation; }
            set { diedPopulation = value; }
        }
        public int ID
        {
            get { return id; }
            set { id = value; }
        }
        public double InitialPrevalence
        {
            get { return initialPrevalence; }
            set { initialPrevalence = value; }
        }
        public int Population
        {
            get { return population; }
            set { population = value; }
        }
        public BE_StateType Type
        {
            get { return type; }
            set { type = value; }
        }
        public double Utility
        {
            get { return utility; }
            set { utility = value; }
        }
        #endregion
        public void AddPopulation()
        {
            populationHistory.Add(population);
        }
        public double CalculateAveragePopulation()
        {
            int total = 0;
            foreach (int i in populationHistory)
                total += i;
            return (double)total / populationHistory.Count;
        }
        public int FindNextStateID(Random rand)
        {
            double d = rand.NextDouble();
            int index = 0;
            for (int i = 0; i < cumulativeProbability.Length; i++)
            {
                if (d < cumulativeProbability[i])
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
        public override string ToString()
        {
            string temp = "State ID: " + id + ", Type: " + type.ToString() + ", Prevalence: " + initialPrevalence + ", Utility: " + utility + ", TP: ";
            for (int i = 0; i < transitionProbability.Length; i++)
                temp += (transitionProbability[i] + " - ");
            return temp;
        }
    }
}
