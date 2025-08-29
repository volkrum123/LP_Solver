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

        private int NumRows => _optimalTableau.GetLength(0);
        private int NumCols => _optimalTableau.GetLength(1);
        private int NumVariables => _model.NumVariables;
        private int NumConstraints => NumRows - 1;

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
                sb.AppendLine($"x{j + 1}: {_optimalTableau[0, j]}");
            return sb.ToString();
        }

        public string DisplayShadowPrices()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Shadow Prices:");
            for (int i = 0; i < NumConstraints; i++)
            {
                int slackCol = NumVariables + i;
                sb.AppendLine($"Constraint {i + 1}: {_optimalTableau[0, slackCol]}");
            }
            return sb.ToString();
        }

        public string DisplayObjectiveRanges()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Objective Coefficient Ranges (Allowable Increase/Decrease):");
            for (int j = 0; j < NumVariables; j++)
            {
                double rc = _optimalTableau[0, j]; // reduced cost
                sb.AppendLine($"x{j + 1}: Lower = {-Math.Max(rc, 0)}, Upper = {Math.Max(-rc, 0)}");
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
                double shadow = _optimalTableau[0, slackCol];
                double rhs = _optimalTableau[i + 1, NumCols - 1];
                sb.AppendLine($"Constraint {i + 1}: Lower = {rhs - shadow}, Upper = {rhs + shadow}");
            }
            return sb.ToString();
        }

        // ------------------- Apply Methods -------------------

        // 1️⃣ Non-basic variable (objective coefficient)
        public string ApplyNonBasicCoefficientChange(int index, double newCoefficient)
        {
            if (index < 0 || index >= NumVariables)
                return "Invalid variable index.";

            double oldCoefficient = _model.ObjectiveCoefficients[index];
            _model.ObjectiveCoefficients[index] = newCoefficient;

            if (_basicIndices.Contains(index))
                return $"Variable x{index + 1} is basic; use ApplyBasicVariableChange instead.";

            // Update z-row for this non-basic variable
            _optimalTableau[0, index] += newCoefficient - oldCoefficient;

            // Recompute z-RHS
            RecomputeZRowRHS();

            return $"Non-Basic Variable x{index + 1} coefficient changed from {oldCoefficient} to {newCoefficient}. Tableau updated.";
        }
        public string ApplyConstraintCoefficientChange(int constraintIndex, int variableIndex, double newValue)
        {
            if (constraintIndex < 0 || constraintIndex >= NumConstraints)
                return "Invalid constraint index.";
            if (variableIndex < 0 || variableIndex >= NumVariables)
                return "Invalid variable index.";

            _optimalTableau[constraintIndex + 1, variableIndex] = newValue;

            // Recompute z-row after modifying the tableau
            RecomputeZRow();

            return $"Constraint {constraintIndex + 1} variable x{variableIndex + 1} coefficient set to {newValue}. Recalculated optimal solution.";
        }
        // 2️⃣ Basic variable (value in RHS)
        public string ApplyBasicVariableChange(int basicVarIndex, double newValue)
        {
            if (!_basicIndices.Contains(basicVarIndex))
                return "Variable is not currently basic.";

            int rowIndex = _basicIndices.IndexOf(basicVarIndex) + 1;
            double oldValue = _optimalTableau[rowIndex, NumCols - 1];
            _optimalTableau[rowIndex, NumCols - 1] = newValue;

            // Recompute entire z-row after RHS change
            RecomputeZRow();

            return $"Basic Variable x{basicVarIndex + 1} value changed from {oldValue} to {newValue}. Tableau updated.";
        }

        // 3️⃣ RHS change for constraint
        public string ApplyRHSChange(int constraintIndex, double newRHS)
        {
            if (constraintIndex < 0 || constraintIndex >= NumConstraints)
                return "Invalid constraint index.";

            double oldValue = _optimalTableau[constraintIndex + 1, NumCols - 1];
            _optimalTableau[constraintIndex + 1, NumCols - 1] = newRHS;

            // Recompute z-row for RHS changes
            RecomputeZRow();

            return $"Constraint {constraintIndex + 1} RHS changed from {oldValue} to {newRHS}. Tableau updated.";
        }

        // ------------------- Z-row recomputation -------------------
        public void RecomputeZRow()
        {
            // Reset all z-row values
            for (int j = 0; j < NumVariables; j++)
                _optimalTableau[0, j] = 0;

            // Add contributions of basic variables
            for (int i = 0; i < _basicIndices.Count; i++)
            {
                int basicVar = _basicIndices[i];
                double cB = _model.ObjectiveCoefficients[basicVar];
                for (int j = 0; j < NumVariables; j++)
                    _optimalTableau[0, j] += cB * _optimalTableau[i + 1, j];
            }

            // Subtract objective coefficients
            for (int j = 0; j < NumVariables; j++)
                _optimalTableau[0, j] = _model.ObjectiveCoefficients[j] - _optimalTableau[0, j];

            // Update z-RHS
            RecomputeZRowRHS();
        }

        private void RecomputeZRowRHS()
        {
            double zRHS = 0;
            for (int i = 0; i < _basicIndices.Count; i++)
            {
                int basicVar = _basicIndices[i];
                double cB = _model.ObjectiveCoefficients[basicVar];
                zRHS += cB * _optimalTableau[i + 1, NumCols - 1];
            }
            _optimalTableau[0, NumCols - 1] = zRHS;
        }

        // ------------------- Update after resolve -------------------
        public void UpdateAfterResolve(double[,] newTableau, List<int> newBasicIndices)
        {
            _optimalTableau = newTableau;
            _basicIndices = newBasicIndices;
        }

        // ------------------- Accessors -------------------
        public double[,] GetOptimalTableau() => _optimalTableau;
        public List<int> GetBasicIndices() => _basicIndices;
        
    }
}

