using System;
using System.Collections.Generic;
using System.Text;

namespace BE_Project
{
    public class BE_Parameter
    {
        string type;
        string name;
        double minValue;
        double baseValue;
        double maxValue;
        double distribution;
        double alpha;
        double beta;
        public BE_Parameter(string typeIn, string nameIn, double minValueIn, double baseValueIn, double maxValueIn, double distributionIn, double alphaIn, double betaIn)
        {
            type = typeIn;
            name = nameIn;
            minValue = minValueIn;
            baseValue = baseValueIn;
            maxValue = maxValueIn;
            distribution = distributionIn;
            alpha = alphaIn;
            beta = betaIn;
        }
        #region GETSET
        public string Type
        {
            get { return type; }
            set { type = value; }
        }
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public double MinValue
        {
            get { return minValue; }
            set { minValue = value; }
        }
        public double BaseValue
        {
            get { return baseValue; }
            set { baseValue = value; }
        }
        public double MaxValue
        {
            get { return maxValue; }
            set { maxValue = value; }
        }
        public double Distribution
        {
            get { return distribution; }
            set { distribution = value; }
        }
        public double Alpha
        {
            get { return alpha; }
            set { alpha = value; }
        }
        public double Beta
        {
            get { return beta; }
            set { beta = value; }
        }
        #endregion
    }
}
