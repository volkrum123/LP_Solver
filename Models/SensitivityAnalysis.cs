using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Models
{
    internal class SensitivityAnalysis
    {
        private LPModel _model;
        private double[,] _optimalTableau;
        private List<int> _basicIndices;

        private int NumVariables => _model.NumVariables;
        private int NumConstraints => _model.Constraints.Count;

        public SensitivityAnalysis(LPModel model, double[,] optimalTableau, List<int> basicIndices)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _optimalTableau = optimalTableau ?? throw new ArgumentNullException(nameof(optimalTableau));
            _basicIndices = basicIndices ?? throw new ArgumentNullException(nameof(basicIndices));
        }

        // ------------------- Display Methods -------------------

        public string DisplayReducedCosts()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Reduced Costs:");

            for (int j = 0; j < NumVariables; j++)
            {
                // Reduced cost = objective row coefficient in final tableau
                double rc = _optimalTableau[0, j];
                sb.AppendLine($"x{j + 1}: {rc}");
            }
            return sb.ToString();
        }

        public string DisplayShadowPrices()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Shadow Prices:");

            for (int i = 0; i < NumConstraints; i++)
            {
                // Shadow price = coefficient of slack variable in objective row
                int slackIndex = NumVariables + i;
                double shadow = _optimalTableau[0, slackIndex];
                sb.AppendLine($"Constraint {i + 1}: {shadow}");
            }
            return sb.ToString();
        }

        public string DisplayObjectiveRanges()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Objective Coefficient Ranges (Allowable Increase/Decrease):");

            for (int j = 0; j < NumVariables; j++)
            {
                double c = _model.ObjectiveCoefficients[j];
                double rc = _optimalTableau[0, j]; // reduced cost
                double lower = c - Math.Max(rc, 0);
                double upper = c + Math.Max(-rc, 0);
                sb.AppendLine($"x{j + 1}: Lower = {lower}, Upper = {upper}");
            }

            return sb.ToString();
        }

        public string DisplayRHSRanges()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("RHS Ranges (Allowable Increase/Decrease):");

            for (int i = 0; i < NumConstraints; i++)
            {
                int slackCol = NumVariables + i;
                double shadow = _optimalTableau[0, slackCol]; // shadow price
                double rhs = _optimalTableau[i + 1, _optimalTableau.GetLength(1) - 1]; // RHS
                double lower = rhs - shadow;
                double upper = rhs + shadow;
                sb.AppendLine($"Constraint {i + 1}: Lower = {lower}, Upper = {upper}");
            }

            return sb.ToString();
        }

        // ------------------- Apply Methods -------------------

        public string ApplyNonBasicVariableChange(int index, double newCoefficient)
        {
            if (index < 0 || index >= NumVariables)
                return "Invalid variable index.";

            double oldValue = _model.ObjectiveCoefficients[index];
            _model.ObjectiveCoefficients[index] = newCoefficient;

            return $"Non-Basic Variable x{index + 1} coefficient changed from {oldValue} to {newCoefficient}. " +
                   "Recalculate optimal solution for updated results.";
        }

        public string ApplyBasicVariableChange(int index, double newValue)
        {
            if (index < 0 || index >= NumVariables)
                return "Invalid variable index.";

            int rowIndex = _basicIndices.IndexOf(index);
            if (rowIndex == -1)
                return "Variable is not currently basic.";

            double oldValue = _optimalTableau[rowIndex + 1, _optimalTableau.GetLength(1) - 1];
            _optimalTableau[rowIndex + 1, _optimalTableau.GetLength(1) - 1] = newValue;

            return $"Basic Variable x{index + 1} value changed from {oldValue} to {newValue}. " +
                   "Recalculate optimal solution for updated results.";
        }

        public string ApplyRHSChange(int constraintIndex, double newRHS)
        {
            if (constraintIndex < 0 || constraintIndex >= NumConstraints)
                return "Invalid constraint index.";

            double oldValue = _optimalTableau[constraintIndex + 1, _optimalTableau.GetLength(1) - 1];
            _optimalTableau[constraintIndex + 1, _optimalTableau.GetLength(1) - 1] = newRHS;

            return $"Constraint {constraintIndex + 1} RHS changed from {oldValue} to {newRHS}. " +
                   "Recalculate optimal solution for updated results.";
        }

        public string ApplyVariableChange(int index, double newValue)
        {
            if (_basicIndices.Contains(index))
                return ApplyBasicVariableChange(index, newValue);
            else
                return ApplyNonBasicVariableChange(index, newValue);
        }
      }
    }
