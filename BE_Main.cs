using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BE_Project
{
    public class BE_Main
    {
        #region ***Change the following according to the project-only one***
        static bool runBasecase = true;
        static bool runDeterministicSA = false;
        static bool runProbabilisticSA = false;
        static string projectName = "BE_Screening";
        static bool writeConsole = true;
        static int stateSize = 6;
        #endregion

        #region ***You can change the following to each run***
        static int cycleLength = 3;         //  The cycle length is three months. 
        static int totalCycleCount = 200;   //  The cycle length is three months. => 200 cycles = 50 years
        static int patientSize = 1000000;
        static BE_Screening screening = new BE_Screening(BE_ScreeningType.NoScreening); //  BE_ScreeningType can be: NoScreening, sEGD, EXAS, hTNE, mTNE, Cytosponge, eVOC
        static BE_Screening confirmation = new BE_Screening(BE_ScreeningType.NoScreening); //  BE_ScreeningType can be: NoScreening, sEGD, EXAS, hTNE, mTNE, Cytosponge, eVOC
        static BE_Surveillance surveillance = new BE_Surveillance(BE_SurveillanceType.NoSurveillance);   //  BE_SurveillanceType can be: NoSurveillance, sEGD
        static BE_EET EET = new BE_EET(stateSize);
        static BE_Surgery surgery = new BE_Surgery();
        static BE_Palliative palliative = new BE_Palliative();
        static string payerApproach = "Medicare";   //  Commercial, Medicare
        static double discountFactor = 0.00246627;    //  0.00246627 per month, 0.03 per year
        #endregion

        static Random rand = new Random(911);   //  random number generation with a seed
        static string directoryPath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
        static double[,] backgroundMortality = new double[2, 101];    //  index 0: female, 1: male - between age 0-100

        static int cycleIndex = 1;

        static BE_Cycle[] cycles = new BE_Cycle[totalCycleCount + 1];   //  The cycle index starts form zero.
        static BE_Patient[] patients = new BE_Patient[patientSize];
        static BE_State[] states = new BE_State[stateSize];
        static List<BE_Parameter> modelParameters = new List<BE_Parameter>();
        static void TracePatients()  //  missing: can be enriched
        {
            foreach (BE_Patient currentPatient in patients)
                if (currentPatient.stateHistory.Count != 201)
                    Console.WriteLine(new System.ComponentModel.WarningException("Wrong stateHistory.Count!").Message);
            double averageDeathAge = 0;
            double averageQALY = 0;
            foreach (BE_Patient currentPatient in patients)
            {
                averageDeathAge += currentPatient.DeathAge;
                currentPatient.Qaly = CalculatePatientTotalUtility(currentPatient);
                averageQALY += currentPatient.Qaly;
            }
            averageDeathAge = averageDeathAge / patientSize;
            averageQALY = averageQALY / patientSize;
        }
        static double CalculatePatientTotalUtility(BE_Patient patientIn)
        {
            double totalUtility = 0;
            for (int i = 0; i < patientIn.DeathAge - 50; i++)
            {
                totalUtility += states[(int)patientIn.stateHistory.Values[i]].Utility;
            }
            for (int i = 0; i < patientIn.interventionHistory.Count; i++)
            {
                if (patientIn.interventionHistory.Values[i] == BE_InterventionType.Surveillance || patientIn.interventionHistory.Values[i] == BE_InterventionType.FollowUpSurveillance)
                {
                    totalUtility = totalUtility + surveillance.UtilityTime * (surveillance.Utility - states[(int)patientIn.stateHistory.Values[patientIn.interventionHistory.Keys[i]]].Utility);
                }
                else if (patientIn.interventionHistory.Values[i] == BE_InterventionType.EET || patientIn.interventionHistory.Values[i] == BE_InterventionType.RFA)
                {
                    totalUtility = totalUtility + EET.UtilityTime * (EET.Utility - states[(int)patientIn.stateHistory.Values[patientIn.interventionHistory.Keys[i]]].Utility);
                }
                else if (patientIn.interventionHistory.Values[i] == BE_InterventionType.Surgery)
                {
                    totalUtility = totalUtility + surgery.UtilityTime * surgery.Utility;
                }
                else if (patientIn.interventionHistory.Values[i] == BE_InterventionType.Chemotherapy)
                {
                    totalUtility = totalUtility + surgery.UtilityChemotherapyTime * surgery.UtilityChemotherapy;
                }
                else if (patientIn.interventionHistory.Values[i] == BE_InterventionType.PostSurgery)
                {
                    totalUtility = totalUtility + surgery.UtilityPostSurgeryTime * (surgery.UtilityPostSurgery - states[(int)patientIn.stateHistory.Values[patientIn.interventionHistory.Keys[i]]].Utility);
                }
                else if (patientIn.interventionHistory.Values[i] == BE_InterventionType.Palliative)
                {
                    totalUtility = totalUtility + palliative.UtilityTime * (palliative.Utility - states[(int)patientIn.stateHistory.Values[patientIn.interventionHistory.Keys[i]]].Utility);
                }
            }

            for (int i = 0; i < patientIn.adverseEventHistory.Count; i++)
            {
                if (patientIn.adverseEventHistory.Values[i] == BE_AdverseEventType.Surveillance || patientIn.adverseEventHistory.Values[i] == BE_AdverseEventType.FollowUpSurveillance)
                {
                    totalUtility = totalUtility + surveillance.AdverseEvent.UtilityTime * (surveillance.AdverseEvent.Utility - states[(int)patientIn.stateHistory.Values[patientIn.adverseEventHistory.Keys[i]]].Utility);
                }
                else if (patientIn.adverseEventHistory.Values[i] == BE_AdverseEventType.EET || patientIn.adverseEventHistory.Values[i] == BE_AdverseEventType.RFA)
                {
                    totalUtility = totalUtility + EET.AdverseEvent.UtilityTime * (EET.AdverseEvent.Utility - states[(int)patientIn.stateHistory.Values[patientIn.adverseEventHistory.Keys[i]]].Utility);
                }
                else if (patientIn.adverseEventHistory.Values[i] == BE_AdverseEventType.Surgery)
                {
                    totalUtility = totalUtility + surgery.AdverseEvent.UtilityTime * (surgery.AdverseEvent.Utility - states[(int)patientIn.stateHistory.Values[patientIn.adverseEventHistory.Keys[i]]].Utility);
                }
            }
            return totalUtility;
        }
        static void ControlInputs() //  missing
        {
            // control parameters are like when no input data is read related to that parameter
            //control paramters should also include write all paramaters
            // We can also write all variables and their values to a .txt file to see them working
            foreach (BE_State currentState in states)
            {
                //  missing: control state transition probabilities here.
            }
        }
        static void ReadAllParametersFromFile() //  Reads all model input parameters from "Parameters.csv" and saves them to "modelParameters".
        {
            string[] lines = File.ReadAllLines(directoryPath + "\\INPUT\\Parameters.csv");

            for (int i = 1; i < lines.Length; i++)  //  Row zero contains the headers, ignore it.
            {
                string[] currentLine = lines[i].Split(";");

                string type = currentLine[0];
                string name = currentLine[1];
                double minValue = double.Parse(currentLine[2], CultureInfo.InvariantCulture);       //  Be careful about decimal seperators.
                double baseValue = double.Parse(currentLine[3], CultureInfo.InvariantCulture);      //  Be careful about decimal seperators.
                double maxValue = double.Parse(currentLine[4], CultureInfo.InvariantCulture);       //  Be careful about decimal seperators.
                double distribution = double.Parse(currentLine[5], CultureInfo.InvariantCulture);   //  Be careful about decimal seperators.
                double alpha = double.Parse(currentLine[6], CultureInfo.InvariantCulture);          //  Be careful about decimal seperators.
                double beta = double.Parse(currentLine[7], CultureInfo.InvariantCulture);           //  Be careful about decimal seperators.

                modelParameters.Add(new BE_Parameter(type, name, minValue, baseValue, maxValue, distribution, alpha, beta));
            }
        }
        static void WriteIntoVariables()
        {
            for (int i = 0; i < modelParameters.Count; i++)
            {
                switch (modelParameters[i].Type)
                {
                    case "Transition_Probability":
                        BE_StateType fromTransition = (BE_StateType)Enum.Parse(typeof(BE_StateType), modelParameters[i].Name.Split("_to_")[0]);
                        BE_StateType toTransition = (BE_StateType)Enum.Parse(typeof(BE_StateType), modelParameters[i].Name.Split("_to_")[1]);
                        states[(int)fromTransition].transitionProbability[(int)toTransition] = modelParameters[i].BaseValue;
                        break;
                    case "Prevalence":
                        states[(int)(BE_StateType)Enum.Parse(typeof(BE_StateType), modelParameters[i].Name)].InitialPrevalence = modelParameters[i].BaseValue;
                        break;
                    case "Utility":
                        states[(int)(BE_StateType)Enum.Parse(typeof(BE_StateType), modelParameters[i].Name)].Utility = modelParameters[i].BaseValue;
                        break;
                    case "Adherence":
                        if (modelParameters[i].Name == screening.Type.ToString())
                            screening.Adherence = modelParameters[i].BaseValue;
                        if (modelParameters[i].Name == surveillance.Type.ToString())
                            surveillance.Adherence = modelParameters[i].BaseValue;
                        break;
                    case "Adherence_Confirmation":
                        if (modelParameters[i].Name == confirmation.Type.ToString())
                            confirmation.Adherence = modelParameters[i].BaseValue;
                        break;
                    case "Cost":
                        if (modelParameters[i].Name == screening.Type.ToString())
                            screening.Cost = modelParameters[i].BaseValue;
                        if (modelParameters[i].Name == confirmation.Type.ToString())
                            confirmation.Cost = modelParameters[i].BaseValue;
                        if (modelParameters[i].Name == surveillance.Type.ToString())
                            surveillance.Cost = modelParameters[i].BaseValue;
                        break;
                    case "Sensitivity":
                        if (modelParameters[i].Name == screening.Type.ToString())
                            screening.Sensitivity = modelParameters[i].BaseValue;
                        if (modelParameters[i].Name == confirmation.Type.ToString())
                            confirmation.Sensitivity = modelParameters[i].BaseValue;
                        if (modelParameters[i].Name == surveillance.Type.ToString())
                            surveillance.Sensitivity = modelParameters[i].BaseValue;
                        break;
                    case "Specificity":
                        if (modelParameters[i].Name == screening.Type.ToString())
                            screening.Specificity = modelParameters[i].BaseValue;
                        if (modelParameters[i].Name == confirmation.Type.ToString())
                            confirmation.Specificity = modelParameters[i].BaseValue;
                        if (modelParameters[i].Name == surveillance.Type.ToString())
                            surveillance.Specificity = modelParameters[i].BaseValue;
                        break;
                    case "Mortality":
                        if (modelParameters[i].Name == screening.Type.ToString())
                            screening.Mortality = modelParameters[i].BaseValue;
                        if (modelParameters[i].Name == confirmation.Type.ToString())
                            confirmation.Mortality = modelParameters[i].BaseValue;
                        if (modelParameters[i].Name == surveillance.Type.ToString())
                            surveillance.Mortality = modelParameters[i].BaseValue;
                        break;
                    case "Tx_Mortality":
                        switch (modelParameters[i].Name)
                        {
                            case "EET":
                                EET.Mortality = modelParameters[i].BaseValue;
                                break;
                            case "Surgery_IMC":
                                surgery.MortalityIMC = modelParameters[i].BaseValue;
                                break;
                            case "Surgery_SC":
                                surgery.MortalitySC = modelParameters[i].BaseValue;
                                break;
                            case "Palliative":
                                palliative.Mortality = ConvertAnnualProbability(modelParameters[i].BaseValue);
                                break;
                            default:
                                Console.WriteLine(new System.ComponentModel.WarningException(modelParameters[i].Type + " & " + modelParameters[i].Name + " not defined!").Message);
                                break;
                        }
                        break;
                    case "Surveillance":
                        switch (modelParameters[i].Name)
                        {
                            case "NDBE":
                                surveillance.IntervalNDBE = (int)modelParameters[i].BaseValue;
                                break;
                            case "LGD":
                                surveillance.IntervalLGD = (int)modelParameters[i].BaseValue;
                                break;
                            case "Persistent_LGD":
                                surveillance.PersistentLGD = modelParameters[i].BaseValue;
                                break;
                            default:
                                Console.WriteLine(new System.ComponentModel.WarningException(modelParameters[i].Type + " & " + modelParameters[i].Name + " not defined!").Message);
                                break;
                        }
                        break;
                    case "EET":
                        switch (modelParameters[i].Name)
                        {
                            case "Cost_PerSession":
                                EET.CostPerSession = modelParameters[i].BaseValue;
                                break;
                            case "Cost_RFA":
                                EET.CostRFA = modelParameters[i].BaseValue;
                                break;
                            case "Max_TouchUp_RFA":
                                EET.MaxTouchUpRFA = (int)modelParameters[i].BaseValue;
                                break;
                            case "Session_Count":
                                EET.SessionCount = (int)modelParameters[i].BaseValue;
                                break;
                            default:
                                Console.WriteLine(new System.ComponentModel.WarningException(modelParameters[i].Type + " & " + modelParameters[i].Name + " not defined!").Message);
                                break;
                        }
                        break;
                    case "Surgery":
                        switch (modelParameters[i].Name)
                        {
                            case "Cost_Annual_PostCare":
                                surgery.CostAnnualPostCare = modelParameters[i].BaseValue;
                                break;
                            case "Cost_Chemotherapy":
                                surgery.CostChemotherapy = modelParameters[i].BaseValue;
                                break;
                            case "Cost_Surgery":
                                surgery.CostSurgery = modelParameters[i].BaseValue;
                                break;
                            case "Chemotherapy_SC":
                                surgery.ChemotherapyRatioSC = modelParameters[i].BaseValue;
                                break;
                            case "Surgical_IMC":
                                surgery.SurgicalIMC = modelParameters[i].BaseValue;
                                break;
                            case "Treatable_SC":
                                surgery.TreatableSC = modelParameters[i].BaseValue;
                                break;
                            default:
                                Console.WriteLine(new System.ComponentModel.WarningException(modelParameters[i].Type + " & " + modelParameters[i].Name + " not defined!").Message);
                                break;
                        }
                        break;
                    case "Palliative":
                        switch (modelParameters[i].Name)
                        {
                            case "Cost_Annual":
                                palliative.CostAnnual = modelParameters[i].BaseValue;
                                break;
                            default:
                                Console.WriteLine(new System.ComponentModel.WarningException(modelParameters[i].Type + " & " + modelParameters[i].Name + " not defined!").Message);
                                break;
                        }
                        break;
                    case "Efficacy_EET":
                        BE_StateType fromEfficacy = (BE_StateType)Enum.Parse(typeof(BE_StateType), modelParameters[i].Name.Split("_to_")[0]);
                        BE_StateType toEfficacy = (BE_StateType)Enum.Parse(typeof(BE_StateType), modelParameters[i].Name.Split("_to_")[1]);
                        EET.efficacy[(int)fromEfficacy, (int)toEfficacy] = modelParameters[i].BaseValue;
                        break;
                    case "Recurrence_EET":
                        EET.recurrence[(int)(BE_StateType)Enum.Parse(typeof(BE_StateType), modelParameters[i].Name)] = ConvertAnnualProbability(modelParameters[i].BaseValue);
                        break;
                    case "PostRecurrence_EET":
                        BE_StateType fromPostRecurrence = (BE_StateType)Enum.Parse(typeof(BE_StateType), modelParameters[i].Name.Split("_to_")[0]);
                        BE_StateType toPostRecurrence = (BE_StateType)Enum.Parse(typeof(BE_StateType), modelParameters[i].Name.Split("_to_")[1]);
                        EET.postRecurrence[(int)fromPostRecurrence, (int)toPostRecurrence] = modelParameters[i].BaseValue;
                        break;
                    case "AnnualMortality_PostSurgery":
                        switch (modelParameters[i].Name.Split("_")[0])
                        {
                            case "T1a":
                                surgery.postMortalityT1a[int.Parse(modelParameters[i].Name.Split("_")[1])] = ConvertAnnualProbability(modelParameters[i].BaseValue);
                                break;
                            case "T1b":
                                surgery.postMortalityT1b[int.Parse(modelParameters[i].Name.Split("_")[1])] = ConvertAnnualProbability(modelParameters[i].BaseValue);
                                break;
                            case "SC":
                                surgery.postMortalitySC[int.Parse(modelParameters[i].Name.Split("_")[1])] = ConvertAnnualProbability(modelParameters[i].BaseValue);
                                break;
                            default:
                                Console.WriteLine(new System.ComponentModel.WarningException(modelParameters[i].Type + " & " + modelParameters[i].Name + " not defined!").Message);
                                break;
                        }
                        break;
                    case "PostInterval_Surveillance":
                        switch (modelParameters[i].Name.Split("_")[0])
                        {
                            case "LGD":
                                surveillance.postIntervalLGD[int.Parse(modelParameters[i].Name.Split("_")[1])] = (int)modelParameters[i].BaseValue;
                                break;
                            case "HGD":
                                surveillance.postIntervalHGD[int.Parse(modelParameters[i].Name.Split("_")[1])] = (int)modelParameters[i].BaseValue;
                                break;
                            default:
                                Console.WriteLine(new System.ComponentModel.WarningException(modelParameters[i].Type + " & " + modelParameters[i].Name + " not defined!").Message);
                                break;
                        }
                        break;
                    case "Tx_Utility":
                        switch (modelParameters[i].Name)
                        {
                            case "Surveillance":
                                surveillance.Utility = modelParameters[i].BaseValue;
                                break;
                            case "EET":
                                EET.Utility = modelParameters[i].BaseValue;
                                break;
                            case "Surgery":
                                surgery.Utility = modelParameters[i].BaseValue;
                                break;
                            case "Chemotherapy":
                                surgery.UtilityChemotherapy = modelParameters[i].BaseValue;
                                break;
                            case "PostSurgery":
                                surgery.UtilityPostSurgery = modelParameters[i].BaseValue;
                                break;
                            case "Palliative":
                                palliative.Utility = modelParameters[i].BaseValue;
                                break;
                            default:
                                Console.WriteLine(new System.ComponentModel.WarningException(modelParameters[i].Type + " & " + modelParameters[i].Name + " not defined!").Message);
                                break;
                        }
                        break;
                    case "Tx_Utility_Time":
                        switch (modelParameters[i].Name)
                        {
                            case "Surveillance":
                                surveillance.UtilityTime = modelParameters[i].BaseValue;
                                break;
                            case "EET":
                                EET.UtilityTime = modelParameters[i].BaseValue;
                                break;
                            case "Surgery":
                                surgery.UtilityTime = modelParameters[i].BaseValue;
                                break;
                            case "Chemotherapy":
                                surgery.UtilityChemotherapyTime = modelParameters[i].BaseValue;
                                break;
                            case "PostSurgery":
                                surgery.UtilityPostSurgeryTime = modelParameters[i].BaseValue;
                                break;
                            case "Palliative":
                                palliative.UtilityTime = modelParameters[i].BaseValue;
                                break;
                            default:
                                Console.WriteLine(new System.ComponentModel.WarningException(modelParameters[i].Type + " & " + modelParameters[i].Name + " not defined!").Message);
                                break;
                        }
                        break;
                    case "AE_Probability":
                        switch (modelParameters[i].Name)
                        {
                            case "Surveillance":
                                surveillance.AdverseEvent.Probability = modelParameters[i].BaseValue;
                                break;
                            case "EET":
                                EET.AdverseEvent.Probability = modelParameters[i].BaseValue;
                                break;
                            case "Surgery":
                                surgery.AdverseEvent.Probability = modelParameters[i].BaseValue;
                                break;
                            default:
                                Console.WriteLine(new System.ComponentModel.WarningException(modelParameters[i].Type + " & " + modelParameters[i].Name + " not defined!").Message);
                                break;
                        }
                        break;
                    case "AE_Cost":
                        switch (modelParameters[i].Name)
                        {
                            case "Surveillance":
                                surveillance.AdverseEvent.Cost = modelParameters[i].BaseValue;
                                break;
                            case "EET":
                                EET.AdverseEvent.Cost = modelParameters[i].BaseValue;
                                break;
                            case "Surgery":
                                surgery.AdverseEvent.Cost = modelParameters[i].BaseValue;
                                break;
                            default:
                                Console.WriteLine(new System.ComponentModel.WarningException(modelParameters[i].Type + " & " + modelParameters[i].Name + " not defined!").Message);
                                break;
                        }
                        break;
                    case "AE_Utility":
                        switch (modelParameters[i].Name)
                        {
                            case "Surveillance":
                                surveillance.AdverseEvent.Utility = modelParameters[i].BaseValue;
                                break;
                            case "EET":
                                EET.AdverseEvent.Utility = modelParameters[i].BaseValue;
                                break;
                            case "PostSurgery":
                                surgery.AdverseEvent.Utility = modelParameters[i].BaseValue;
                                break;
                            default:
                                Console.WriteLine(new System.ComponentModel.WarningException(modelParameters[i].Type + " & " + modelParameters[i].Name + " not defined!").Message);
                                break;
                        }
                        break;
                    case "AE_Utility_Time":
                        switch (modelParameters[i].Name)
                        {
                            case "Surveillance":
                                surveillance.AdverseEvent.UtilityTime = modelParameters[i].BaseValue;
                                break;
                            case "EET":
                                EET.AdverseEvent.UtilityTime = modelParameters[i].BaseValue;
                                break;
                            case "PostSurgery":
                                surgery.AdverseEvent.UtilityTime = modelParameters[i].BaseValue;
                                break;
                            default:
                                Console.WriteLine(new System.ComponentModel.WarningException(modelParameters[i].Type + " & " + modelParameters[i].Name + " not defined!").Message);
                                break;
                        }
                        break;
                    default:
                        Console.WriteLine(new System.ComponentModel.WarningException(modelParameters[i].Type  + " not defined!").Message);
                        break;
                }
            }
        }
        static void InitializeParameters()
        {
            //  Create cycles.
            for (int i = 0; i < cycles.Count(); i++)
                cycles[i] = new BE_Cycle(i);

            //  Tailor initial prevelances other than NoBarretts
            for (int i = 1; i < stateSize; i++)
                states[i].InitialPrevalence = states[i].InitialPrevalence * (1 - states[0].InitialPrevalence);

            foreach (BE_State currentState in states)
            {
                //  Convert transitionProbability to quarterly
                for (int i = 0; i < stateSize; i++)
                    currentState.transitionProbability[i] = ConvertAnnualProbability(currentState.transitionProbability[i]);
                
                //  Prepare two arrays: "transitionProbability" and "cumulativeProbability
                currentState.InitializeParameters();
                double d = currentState.transitionProbability.Sum();
            }

            EET.InitializeParameters();   // required for cumulative probability arrays

            double[] cumulativePrevalence = new double[stateSize];
            cumulativePrevalence[0] = states[0].InitialPrevalence;
            for (int i = 1; i < stateSize; i++)
                cumulativePrevalence[i] = cumulativePrevalence[i - 1] + states[i].InitialPrevalence;
            
            for (int p = 0; p < patientSize; p++)
            {
                double d = rand.NextDouble();
                int index = -1;
                for (int i = 0; i < stateSize; i++)
                {
                    if (d <= cumulativePrevalence[i])
                    {
                        index = i;
                        break;
                    }
                }
                patients[p] = new BE_Patient(p, states[index]);
                states[index].Population++;
            }
        }
        static void ReadWriteBackgroundMortalititesFromFile() //  Creates a 2D array with 101 rows (age: 0-100) and 2 columns (female, male).
        {
            string[] lines = File.ReadAllLines(directoryPath + "\\INPUT\\" + "USLifeTables2011Female.txt");
            for (int i = 1; i < lines.Length; i++)  //  Row zero contains the headers, ignore it.
                backgroundMortality[0, i - 1] = double.Parse(lines[i].Split("\t")[1], CultureInfo.InvariantCulture);    //  Be careful about decimal seperators.

            lines = File.ReadAllLines(directoryPath + "\\INPUT\\" + "USLifeTables2011Male.txt");
            for (int i = 1; i < lines.Length; i++)  //  Row zero contains the headers, ignore it.
                backgroundMortality[1, i - 1] = double.Parse(lines[i].Split("\t")[1], CultureInfo.InvariantCulture);    //  Be careful about decimal seperators.

            //  Convert annual backgroundMortality probabilities to quarterly probabilities. Assumption: The cycle length is three months.
            for (int i = 0; i <= 1; i++)
                for (int j = 0; j < lines.Length - 1; j++)
                    backgroundMortality[i, j] = ConvertAnnualProbability(backgroundMortality[i, j]);
        }
        static void Main(string[] args)
        {
            //  Health states are created.
            states[0] = new BE_State(BE_StateType.NoBarretts, stateSize);
            states[1] = new BE_State(BE_StateType.NDBE, stateSize);
            states[2] = new BE_State(BE_StateType.LGD, stateSize);
            states[3] = new BE_State(BE_StateType.HGD, stateSize);
            states[4] = new BE_State(BE_StateType.IMC, stateSize);
            states[5] = new BE_State(BE_StateType.SC, stateSize);

            ReadAllParametersFromFile();
            WriteIntoVariables();
            ReadWriteBackgroundMortalititesFromFile();

            InitializeParameters();
            ControlInputs();
            
            DateTime startTime = DateTime.Now;

            if (runBasecase)
                StartSimulation();
            if (runDeterministicSA)
                RunDeterministicSensitivityAnalysis();
            if (runProbabilisticSA)
                RunProbabilisticSensitivityAnalysis();

            DateTime endTime = DateTime.Now;
            TimeSpan elapsedTime = endTime - startTime;
            Console.WriteLine(elapsedTime);
            #region END OF ANALYSIS REPORTING
            //  missing
            #endregion
        }
        static BE_Outcome StartSimulation()
        {
            foreach (BE_Patient currentPatient in patients)
            {
                //  All patients are screened in the first cycle.
                screening.ScreenPatient(currentPatient, cycles[cycleIndex], rand);

                //  If there is a defined confirmation test and the patient has a positive result in the screening test, confirmation is made.
                if (confirmation.Type != BE_ScreeningType.NoScreening && currentPatient.HasPositiveTest == true)
                    confirmation.ScreenPatient(currentPatient, cycles[cycleIndex], rand);   //  Assumption: Confirmation is in the same cycle with screening.

                // Based on the results of the screening/confirmation, screening decision is made.
                ScreeningDecision(currentPatient);

                //  If the patient is confirmed to be in one of the disease states, surveillance decision is made.
                if (currentPatient.ConfirmedState.Type != BE_StateType.NoBarretts)
                    surveillance.SurveillanceDecision(currentPatient, cycles[cycleIndex], rand);

                //////////////////  WE LEFT HERE    //////////////////

                //  If there is treatment assigned in this cycle (cycleIndex = 1), treatment is chosen and initiated.
                if (currentPatient.NextTreatment == cycleIndex)
                    ChooseStartTreatment(currentPatient);   //  Assumption: Treatment is initiated in the same cycle with screening.
                
                //  At the end of each cycle, the patient status is updated.
                currentPatient.patientStatusHistory.Add(cycleIndex, currentPatient.Status);
            }
            #region END OF CYCLE REPORTING
            //  Population information is written to the console at the end of each cycle.
            if (writeConsole)
                WriteCyclePopulationToConsole();

            // To keep track of the necessary information at the end of each cycle.
            cycles[cycleIndex].UpdateCostAndQalyForThisCycle(patients, cycleIndex);
            foreach (BE_State currentState in states)
                currentState.AddPopulation();
            #endregion
            for (cycleIndex = 2; cycleIndex <= totalCycleCount; cycleIndex++)
            {
                foreach (BE_Patient currentPatient in patients)
                {
                    if (!currentPatient.IsDead) //  If the patient is not dead, he can die in this cycle.
                    {
                        //  The patient may get older.
                        if (cycleIndex % 4 == 0)    //  Assumption: The cycle length is 3 months, so four cycles mean one year.
                            currentPatient.Age++;

                        //  The patient may die from postsurgery mortality, palliative treatment mortality, and background mortality.
                        if (currentPatient.HasSurgery) //  Assumption: Postsurgery and palliative mortality override all cause mortality.
                            surgery.PostSurgeryMortality(currentPatient, cycles[cycleIndex], rand);
                        else if (currentPatient.UnderPalliative)
                            palliative.PalliativeMortality(currentPatient, rand);
                        else
                            AllCauseMortality(currentPatient);
                    }

                    //  Two separate "IsDead" checks are necessary, because the patient may die in the above code block.
                    //  Dead patients just remain in the same health state.
                    if (currentPatient.IsDead)
                        currentPatient.stateHistory.Add(cycleIndex, currentPatient.CurrentState.Type);
                    else
                    {
                        //  The patient can move using either natural progression probabilities or recurrence probabilities.
                        //  Or the patient can stay in the same health state: during EET, postsurgery, during palliative.
                        if (currentPatient.ActiveNaturalProgression)
                            currentPatient.MoveNaturalProgression(states, cycles[cycleIndex], rand);
                        else if (currentPatient.ActiveRecurrenceProbability)
                            currentPatient.MoveRecurrence(EET, states, cycles[cycleIndex], rand);
                        else
                            currentPatient.stateHistory.Add(cycleIndex, currentPatient.CurrentState.Type);    //  It means that the patient remains in the same health state.

                        //  Currently, this is only the immediate diagnosis of symptomatic cancer.
                        InformalDetectionOfDisease(currentPatient);
                        
                        //  If the patient has a scheduled surveillance appointment in this cycle
                        if (currentPatient.NextSurveillance == cycleIndex)
                            if (currentPatient.CompletedEET)
                                surveillance.FollowUpSurveilPatient(currentPatient, cycles[cycleIndex], rand);
                            else
                                surveillance.SurveilPatient(currentPatient, cycles[cycleIndex], rand);

                        //  If the patient has a scheduled treatment in this cycle
                        if (currentPatient.NextTreatment == cycleIndex)
                            ChooseStartTreatment(currentPatient);
                        //  After EET is complete, a desicion needs to be made.
                        if (currentPatient.NeedPostEETDecision)
                            DecidePostEETandRFA(currentPatient);
                    }
                    //  At the end of each cycle, the patient status is updated.
                    currentPatient.patientStatusHistory.Add(cycleIndex, currentPatient.Status);
                }
                #region END OF CYCLE REPORTING
                //  Population information is written to the console at the end of each cycle.
                if (writeConsole)
                    WriteCyclePopulationToConsole();

                // To keep track of the necessary information at the end of each cycle.
                cycles[cycleIndex].UpdateCostAndQalyForThisCycle(patients, cycleIndex);
                foreach (BE_State currentState in states)
                    currentState.AddPopulation();
                #endregion
            }
            #region END OF RUN REPORTING
            TracePatients();
            double[] averagePopulation = new double[stateSize];
            for (int i = 0; i < stateSize; i++)
            {
                averagePopulation[i] = states[i].CalculateAveragePopulation();
            }
            #endregion
            return null;    //  missing: return a BE_Outcome
        }
        static void AllCauseMortality(BE_Patient patientIn)
        {
            double mortality = 0;
            if (patientIn.Gender == "Female")
                mortality = backgroundMortality[0, patientIn.Age];
            else
                mortality = backgroundMortality[1, patientIn.Age];

            if (rand.NextDouble() <= mortality)
                patientIn.Die("AllCause");
        }
        static void ChooseStartTreatment(BE_Patient patientIn)  // The patient needs treatment for sure. This function determines which treatment and initiates that treatment.
        {
            if (patientIn.ConfirmedState.Type == BE_StateType.NDBE)
            {
                if (patientIn.CompletedEET == true)
                    EET.GiveTouchUpRFA(patientIn, cycles[cycleIndex], states, rand);
                else
                    Console.WriteLine(new System.ComponentModel.WarningException("Wrong Treatment Algorithm!").Message);
            }
            else if (patientIn.ConfirmedState.Type == BE_StateType.LGD)
            {
                if (patientIn.CompletedEET == true)
                    EET.GiveTouchUpRFA(patientIn, cycles[cycleIndex], states, rand);
                else
                    EET.GiveEETSession(patientIn, cycles[cycleIndex], states, rand);
            }
            else if (patientIn.ConfirmedState.Type == BE_StateType.HGD)
            {
                if (patientIn.CompletedEET == true)
                    EET.GiveTouchUpRFA(patientIn, cycles[cycleIndex], states, rand);
                else
                    EET.GiveEETSession(patientIn, cycles[cycleIndex], states, rand);
            }
            else if (patientIn.ConfirmedState.Type == BE_StateType.IMC)
            {
                if (patientIn.HasSurgery == true)
                    surgery.PostSurgeryCare(patientIn, cycles[cycleIndex]);
                else if (patientIn.CompletedEET == true)
                    surgery.PerformSurgery(patientIn, cycles[cycleIndex], rand);
                else if (patientIn.CountEET > 0)
                    EET.GiveEETSession(patientIn, cycles[cycleIndex], states, rand);
                else if (surgery.IsSuitableForSurgery(patientIn, rand))
                    surgery.PerformSurgery(patientIn, cycles[cycleIndex], rand);
                else
                    EET.GiveEETSession(patientIn, cycles[cycleIndex], states, rand);
            }
            else if (patientIn.ConfirmedState.Type == BE_StateType.SC)
            {
                if (patientIn.HasSurgery == true)
                    surgery.PostSurgeryCare(patientIn, cycles[cycleIndex]);
                else if (patientIn.UnderPalliative == true)
                    palliative.GivePalliativeTreatment(patientIn, cycles[cycleIndex]);
                else if (surgery.IsSuitableForSurgery(patientIn, rand))
                    surgery.PerformSurgery(patientIn, cycles[cycleIndex], rand);
                else
                    palliative.GivePalliativeTreatment(patientIn, cycles[cycleIndex]);
            }
            else
                Console.WriteLine(new System.ComponentModel.WarningException("Wrong Treatment Algorithm!").Message);
        }
        static void DecidePostEETandRFA(BE_Patient patientIn)
        {
            patientIn.NeedPostEETDecision = false;  //  Decision will be made. Flag is switched to false.
            if (patientIn.PostEETState.ID >= patientIn.PreEETState.ID)
            {
                if (patientIn.ConfirmedState.Type == BE_StateType.IMC)
                {
                    surgery.PerformSurgery(patientIn, cycles[cycleIndex], rand);    //  Assumption: EET/RFA and surgery are performed in the same cycle.
                    return;
                }
                if (patientIn.ConfirmedState.Type == BE_StateType.SC)
                {
                    Console.WriteLine(new System.ComponentModel.WarningException("State not possible!").Message);
                    return;
                }
            }

            if (patientIn.PreEETState.Type == BE_StateType.NDBE || patientIn.PreEETState.Type == BE_StateType.LGD)
                patientIn.NextSurveillance = cycleIndex + surveillance.postIntervalLGD[Math.Min(patientIn.CountFollowUp, surveillance.postIntervalLGD.Length - 1)];
            else if (patientIn.PreEETState.Type == BE_StateType.HGD || patientIn.PreEETState.Type == BE_StateType.IMC)
                patientIn.NextSurveillance = cycleIndex + surveillance.postIntervalHGD[Math.Min(patientIn.CountFollowUp, surveillance.postIntervalHGD.Length - 1)];
            else
                Console.WriteLine(new System.ComponentModel.WarningException("Cannot DecidePostEET!").Message);
        }
        static void InformalDetectionOfDisease(BE_Patient patientIn)  //  It can be incidential detection or informal detection.
        {
            if (patientIn.CurrentState.Type == BE_StateType.SC)
            {
                patientIn.ConfirmedState = patientIn.CurrentState;
                patientIn.NextSurveillance = -1; //  Cancel any surveillance appointments scheduled.
                patientIn.NextTreatment = cycleIndex;
            }
        }
        static void ScreeningDecision(BE_Patient patientIn)
        {
            BE_ScreeningResult lastScreeningDecision = patientIn.screenResult[patientIn.screenResult.Count - 1];
            switch (lastScreeningDecision)
            {
                case BE_ScreeningResult.TruePositive:       //  Correct diagnosis of the health state.
                    patientIn.ConfirmedState = patientIn.CurrentState;
                    break;

                case BE_ScreeningResult.FalseNegative:      //  The patient is incorrectly assumed to be healthy. (State 0: NoBarretts)
                    patientIn.ConfirmedState = states[0];
                    break;

                case BE_ScreeningResult.TrueNegative:       //  Correct diagnosis of the health state.
                    patientIn.ConfirmedState = patientIn.CurrentState;
                    break;

                case BE_ScreeningResult.FalsePositive:      //  Assumption: In case of false positive, it is based on the prevelance data.
                    double[] diseaseStatesPrevalence = new double[stateSize - 1];
                    double sum = 0;
                    for (int i = 1; i < stateSize; i++)
                    {
                        sum += states[i].InitialPrevalence;
                        diseaseStatesPrevalence[i - 1] = states[i].InitialPrevalence;
                    }
                    for (int i = 1; i < stateSize; i++)
                        diseaseStatesPrevalence[i - 1] = diseaseStatesPrevalence[i - 1] / sum;
                    double[] cumulativeDiseaseStatesPrevalence = new double[stateSize - 1];
                    cumulativeDiseaseStatesPrevalence[0] = diseaseStatesPrevalence[0];
                    for (int i = 1; i < stateSize - 1; i++)
                        cumulativeDiseaseStatesPrevalence[i] = cumulativeDiseaseStatesPrevalence[i - 1] + diseaseStatesPrevalence[i];
                    double d = rand.NextDouble();
                    int index = 0;
                    for (int i = 0; i < cumulativeDiseaseStatesPrevalence.Length; i++)
                    {
                        if (d < cumulativeDiseaseStatesPrevalence[i])
                        {
                            index = i;
                            break;
                        }
                    }
                    patientIn.ConfirmedState = states[index + 1];
                    break;

                case BE_ScreeningResult.NoShow:
                case BE_ScreeningResult.Death:               //  Assumption: The patient is assumed to be healthy so that no further intervention will be required.
                    patientIn.ConfirmedState = states[0];
                    break;

                default:
                    Console.WriteLine(new System.ComponentModel.WarningException("Cannot give ScreeningDecision!").Message);
                    break;
            }
        }
        static void WriteCyclePopulationToConsole()
        {
            Console.Write("Cycle " + String.Format("{0:D3}", cycleIndex) + ": ");
            int deadPopulation = 0;
            int totalPopulation = 0;
            foreach (BE_State s in states)
            {
                Console.Write(s.Type.ToString() + " " + String.Format("{0:0.000}", (double)s.Population / patientSize) + " - ");
                deadPopulation += s.DiedPopulation;
                totalPopulation += s.Population;
            }
            totalPopulation += deadPopulation;
            Console.Write("Dead: " + String.Format("{0:0.000}", (double)deadPopulation / patientSize) + " - ");
            Console.Write("Total: " + totalPopulation);
            Console.Write("\n");
        }
        static void RunDeterministicSensitivityAnalysis()    //  missing
        {

        }
        static void RunProbabilisticSensitivityAnalysis()    //  missing
        {

        }
        #region some other function to be useful like a library
        static void ApplyTrapezoidalCorrection()    //  missing
        {

        }
        static void ApplySimpsons13()   //  missing
        {

        }
        static double ConvertAnnualProbability(double probabilityIn)
        {
            if (cycleLength == 3)
                return ConvertAnnualProbabilityToQuarterly(probabilityIn);
            else if (cycleLength == 1)
                return ConvertAnnualProbabilityToMonthly(probabilityIn);
            else if (cycleLength == 12)
                return probabilityIn;
            else
                Console.WriteLine(new System.ComponentModel.WarningException("Wrong cycle length!").Message);
            return -1;
        }
        static double ConvertAnnualProbabilityToMonthly(double probabilityIn)
        {
            return 1 - Math.Pow((1 - probabilityIn), (1 / (double)12));
        }
        static double ConvertAnnualProbabilityToQuarterly(double probabilityIn)
        {
            return 1 - Math.Pow((1 - probabilityIn), (1 / (double)4));
        }
        //static double FindICER(BE_Drug drug1, BE_Drug drug2)
        //{
        //    double averageCost = drug1.AverageCostForACohort - drug2.AverageCostForACohort;
        //    double averageQALY = drug1.AverageQALYForACohort - drug2.AverageQALYForACohort;

        //    return (averageCost / averageQALY);
        //}
        #endregion
    }
}
