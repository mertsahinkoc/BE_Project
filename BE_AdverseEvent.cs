using System;
using System.Collections.Generic;
using System.Text;

namespace BE_Project
{
    public class BE_AdverseEvent
    {
        double cost;
        double probability;
        double utility;
        double utilityTime;
        #region GETSET
        public double Cost
        {
            get { return cost; }
            set { cost = value; }
        }
        public double Probability
        {
            get { return probability; }
            set { probability = value; }
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
    }
}
