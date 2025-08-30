using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Models
{

    internal class CuttingPlaneSolver
    {
        public string Solve(LPModel model, Action<string> logOutput)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("=== Cutting Plane Method for Integer Programming ===");
            result.AppendLine($"Problem Type: {model.ObjectiveType}");

            // Ensure we have integer variables
            List<int> integerVariableIndices = EnsureIntegerVariables(model, result);
            result.AppendLine($"Integer Variables: {string.Join(", ", integerVariableIndices.Select(i => $"x{i + 1}"))}");

            int iteration = 0;
            bool integerSolutionFound = false;
            bool infeasible = false;

            // Create and solve initial tableau
            var (tableau, constraintTypes, numVariables, numConstraints) = CreateAndSolveInitialTableau(model, result);

            // Display initial solution
            var canonicalForm = new CanonicalForm();
            result.AppendLine(canonicalForm.TableauToString(tableau, numVariables, numConstraints, constraintTypes));

            // Check if current solution is integer feasible
            integerSolutionFound = IsIntegerFeasible(tableau, integerVariableIndices, result);
            if (integerSolutionFound)
            {
                result.AppendLine("✅ Initial solution is already integer feasible!");
                ExtractSolution(tableau, integerVariableIndices, model, result);
                return result.ToString();
            }

            while (!integerSolutionFound && !infeasible && iteration < 100)
            {
                iteration++;
                result.AppendLine($"\n--- Iteration {iteration} ---");

                // Generate and add Gomory cut
                result.AppendLine("Generating Gomory cut...");
                if (!GenerateAndAddCut(ref tableau, ref constraintTypes, ref numConstraints, integerVariableIndices, result))
                {
                    infeasible = true;
                    result.AppendLine("❌ Problem is infeasible.");
                    break;
                }

                // Display tableau with cut added
                result.AppendLine("Tableau with cut added:");
                result.AppendLine(canonicalForm.TableauToString(tableau, numVariables, numConstraints, constraintTypes));

                // Solve with dual simplex
                try
                {
                    var dualSolver = new DuelSimplexSolver();
                    tableau = dualSolver.SolveDual(tableau, constraintTypes, s => { }, // Don't log during dual simplex
                        numVariables, numConstraints, model.ObjectiveType);

                    // Display updated tableau
                    result.AppendLine("Tableau after dual simplex:");
                    result.AppendLine(canonicalForm.TableauToString(tableau, numVariables, numConstraints, constraintTypes));

                    // Check if current solution is integer feasible
                    integerSolutionFound = IsIntegerFeasible(tableau, integerVariableIndices, result);
                }
                catch (Exception ex)
                {
                    result.AppendLine($"Error in dual simplex: {ex.Message}");
                    break;
                }
            }

            if (integerSolutionFound)
            {
                result.AppendLine("✅ Integer feasible solution found!");
                ExtractSolution(tableau, integerVariableIndices, model, result);
            }
            else if (infeasible)
            {
                result.AppendLine("❌ Problem is infeasible.");
            }
            else if (iteration >= 100)
            {
                result.AppendLine("❌ Iteration limit reached without finding integer solution.");
            }

            return result.ToString();
        }

        private List<int> EnsureIntegerVariables(LPModel model, StringBuilder result)
        {
            List<int> integerVariableIndices = new List<int>();

            // Check IntegerIndices
            if (model.IntegerIndices != null && model.IntegerIndices.Count > 0)
            {
                integerVariableIndices.AddRange(model.IntegerIndices);
            }

            // Check Variables for integer type
            for (int i = 0; i < model.Variables.Count; i++)
            {
                if (model.Variables[i].Type == VariableType.Integer && !integerVariableIndices.Contains(i))
                {
                    integerVariableIndices.Add(i);
                }
            }

            // If still no integer variables, assume all are integer
            if (integerVariableIndices.Count == 0)
            {
                for (int i = 0; i < model.ObjectiveCoefficients.Count; i++)
                {
                    integerVariableIndices.Add(i);
                }
                result.AppendLine("Assuming all variables are integer for this integer programming problem.");
            }

            return integerVariableIndices;
        }

        private (double[,] tableau, List<string> constraintTypes, int numVariables, int numConstraints)
            CreateAndSolveInitialTableau(LPModel model, StringBuilder result)
        {
            int numVariables = model.NumVariables;
            int numConstraints = model.Constraints.Count;

            // For minimization with >= constraints, use dual simplex
            if (model.ObjectiveType.Equals("Min", StringComparison.OrdinalIgnoreCase) &&
                model.Constraints.Any(c => c.Contains(">=")))
            {
                result.AppendLine("Using dual simplex for minimization with >= constraints");
                var dualSolver = new DuelSimplexSolver();
                var (tableau, constraintTypes) = dualSolver.CreateTableau(model);

                // Solve with dual simplex
                tableau = dualSolver.SolveDual(tableau, constraintTypes, s => { },
                    numVariables, numConstraints, model.ObjectiveType);

                return (tableau, constraintTypes, numVariables, numConstraints);
            }
            else
            {
                // Use standard simplex for other cases
                var simplexSolver = new SimplexSolver();
                var (tableau, constraintTypes) = simplexSolver.CreateTableau(model);

                // Solve the LP relaxation
                tableau = simplexSolver.Solve(tableau, constraintTypes, s => { },
                    numVariables, numConstraints, model.ObjectiveType);

                return (tableau, constraintTypes, numVariables, numConstraints);
            }
        }

        private bool IsIntegerFeasible(double[,] tableau, List<int> integerVariableIndices, StringBuilder result)
        {
            int rhsColumn = tableau.GetLength(1) - 1;
            int rows = tableau.GetLength(0);

            // Create a dictionary to store variable values
            Dictionary<int, double> variableValues = new Dictionary<int, double>();

            // Initialize all variables to 0
            for (int i = 0; i < integerVariableIndices.Max() + 1; i++)
            {
                variableValues[i] = 0;
            }

            // Find basic variables and their values
            for (int row = 1; row < rows; row++)
            {
                for (int col = 0; col < rhsColumn; col++)
                {
                    if (Math.Abs(tableau[row, col] - 1) < 1e-6)
                    {
                        // This is a basic variable
                        variableValues[col] = tableau[row, rhsColumn];
                        break;
                    }
                }
            }

            // Check integer feasibility
            foreach (int varIndex in integerVariableIndices)
            {
                double value = variableValues[varIndex];
                if (Math.Abs(value - Math.Round(value)) > 1e-6)
                {
                    result.AppendLine($"Variable x{varIndex + 1} is fractional: {value}");
                    return false;
                }
            }

            return true;
        }

        private bool GenerateAndAddCut(ref double[,] tableau, ref List<string> constraintTypes, ref int numConstraints,
    List<int> integerVariableIndices, StringBuilder result)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            int rhsColumn = cols - 1;

            // Find the most fractional basic integer variable using hybrid strategy
            int selectedRow = SelectCutRow(tableau, integerVariableIndices, result);

            if (selectedRow == -1)
            {
                result.AppendLine("No fractional integer variables found for cut generation.");
                return false;
            }

            // Create new tableau with additional row and column
            int newRows = rows + 1;
            int newCols = cols + 1;
            double[,] newTableau = new double[newRows, newCols];

            // Copy existing tableau to new tableau, preserving RHS as the last column
            for (int i = 0; i < rows; i++)
            {
                // Copy all columns except RHS
                for (int j = 0; j < cols - 1; j++)
                {
                    newTableau[i, j] = tableau[i, j];
                }

                // Set the new slack column to 0 for existing rows
                newTableau[i, newCols - 2] = 0;

                // Copy RHS value to the new last column
                newTableau[i, newCols - 1] = tableau[i, rhsColumn];
            }

            // Generate Gomory cut coefficients
            for (int j = 0; j < cols - 1; j++) // Exclude RHS column
            {
                double value = tableau[selectedRow, j];
                double fractionalPart = value - Math.Floor(value);
                newTableau[rows, j] = -fractionalPart;
            }

            // Set RHS for the cut (in the last column)
            double rhsValue = tableau[selectedRow, rhsColumn];
            double rhsFractional = rhsValue - Math.Floor(rhsValue);
            newTableau[rows, newCols - 1] = -rhsFractional;

            // Set slack variable coefficient (in the new column)
            newTableau[rows, newCols - 2] = 1;

            // Update the tableau and constraint types
            tableau = newTableau;
            constraintTypes.Add("<=");
            numConstraints++;

            result.AppendLine($"Generated Gomory cut from row {selectedRow}");
            result.AppendLine($"Cut: {FormatCut(newTableau, rows, integerVariableIndices, numConstraints, constraintTypes)}");

            return true;
        }
        private int SelectCutRow(double[,] tableau, List<int> integerVariableIndices, StringBuilder result)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            int rhsColumn = cols - 1;

            List<CutRowCandidate> candidates = new List<CutRowCandidate>();

            for (int row = 1; row < rows; row++)
            {
                // Find the basic variable in this row
                int basicVarIndex = -1;
                for (int col = 0; col < rhsColumn; col++)
                {
                    if (Math.Abs(tableau[row, col] - 1) < 1e-6)
                    {
                        basicVarIndex = col;
                        break;
                    }
                }

                // Check if this is an integer variable
                if (basicVarIndex != -1 && integerVariableIndices.Contains(basicVarIndex))
                {
                    double rhsValue = tableau[row, rhsColumn];
                    double fractionalPart = rhsValue - Math.Floor(rhsValue);

                    // Calculate distance from integer
                    double distance = Math.Min(fractionalPart, 1 - fractionalPart);

                    // Calculate sum of fractional parts of coefficients
                    double sumFractionalParts = 0;
                    for (int j = 0; j < rhsColumn; j++)
                    {
                        double coeff = tableau[row, j];
                        sumFractionalParts += coeff - Math.Floor(coeff);
                    }

                    candidates.Add(new CutRowCandidate
                    {
                        Row = row,
                        BasicVarIndex = basicVarIndex,
                        DistanceFromInteger = distance,
                        SumFractionalParts = sumFractionalParts
                    });
                }
            }

            if (candidates.Count == 0)
                return -1;

            // Select row with largest distance from integer
            var selectedCandidate = candidates
                .OrderByDescending(c => c.DistanceFromInteger)
                .ThenByDescending(c => c.SumFractionalParts) // For ties, use stronger cut
                .ThenBy(c => c.Row) // For further ties, use lowest row index
                .First();

            result.AppendLine($"Selected row {selectedCandidate.Row} for variable x{selectedCandidate.BasicVarIndex + 1}");
            result.AppendLine($"Distance from integer: {selectedCandidate.DistanceFromInteger:0.####}");
            result.AppendLine($"Sum of fractional parts: {selectedCandidate.SumFractionalParts:0.####}");

            return selectedCandidate.Row;
        }

        private class CutRowCandidate
        {
            public int Row { get; set; }
            public int BasicVarIndex { get; set; }
            public double DistanceFromInteger { get; set; }
            public double SumFractionalParts { get; set; }
        }

        private string FormatCut(double[,] tableau, int cutRow, List<int> integerVariableIndices, int numConstraints, List<string> constraintTypes)
        {
            StringBuilder cut = new StringBuilder();
            bool firstTerm = true;
            int cols = tableau.GetLength(1);
            int rhsColumn = cols - 1;

            // Add decision variables
            for (int j = 0; j < integerVariableIndices.Count; j++)
            {
                int varIndex = integerVariableIndices[j];
                if (varIndex >= rhsColumn) continue;

                double coeff = tableau[cutRow, varIndex];
                if (Math.Abs(coeff) > 1e-6)
                {
                    if (!firstTerm && coeff > 0) cut.Append(" + ");
                    if (coeff < 0) cut.Append(" - ");

                    cut.Append($"{Math.Abs(coeff):0.###}x{varIndex + 1}");
                    firstTerm = false;
                }
            }

            // Add slack/surplus variables
            int numSlacks = rhsColumn - integerVariableIndices.Count;
            for (int j = integerVariableIndices.Count; j < rhsColumn; j++)
            {
                double coeff = tableau[cutRow, j];
                if (Math.Abs(coeff) > 1e-6)
                {
                    if (!firstTerm && coeff > 0) cut.Append(" + ");
                    if (coeff < 0) cut.Append(" - ");

                    // Determine if it's a slack or surplus variable
                    string varType = "s"; // default to slack
                    int constraintIndex = j - integerVariableIndices.Count;
                    if (constraintTypes != null && constraintIndex < constraintTypes.Count)
                    {
                        varType = constraintTypes[constraintIndex] == ">=" ? "e" : "s";
                    }
                    int varNum = constraintIndex + 1;

                    cut.Append($"{Math.Abs(coeff):0.###}{varType}{varNum}");
                    firstTerm = false;
                }
            }

            cut.Append($" ≤ {tableau[cutRow, rhsColumn]:0.###}");

            return cut.ToString();
        }
        private void ExtractSolution(double[,] tableau, List<int> integerVariableIndices, LPModel model, StringBuilder result)
        {
            int cols = tableau.GetLength(1);
            int rhsColumn = cols - 1;
            int rows = tableau.GetLength(0);

            result.AppendLine("\n=== Final Solution ===");

            // Create a dictionary to store variable values
            Dictionary<int, double> variableValues = new Dictionary<int, double>();

            // Initialize all variables to 0
            for (int i = 0; i < model.NumVariables; i++)
            {
                variableValues[i] = 0;
            }

            // Find basic variables and their values
            for (int row = 1; row < rows; row++)
            {
                for (int col = 0; col < rhsColumn; col++)
                {
                    if (Math.Abs(tableau[row, col] - 1) < 1e-6)
                    {
                        // This is a basic variable
                        variableValues[col] = tableau[row, rhsColumn];
                        break;
                    }
                }
            }

            // Extract values of decision variables
            for (int i = 0; i < model.NumVariables; i++)
            {
                result.AppendLine($"x{i + 1} = {variableValues[i]:0.####}");
            }

            // Calculate objective value from the solution and model coefficients
            double objectiveValue = 0;
            for (int i = 0; i < model.NumVariables; i++)
            {
                objectiveValue += model.ObjectiveCoefficients[i] * variableValues[i];
            }

            // For minimization problems, ensure positive objective value
            if (model.ObjectiveType.Equals("Min", StringComparison.OrdinalIgnoreCase))
            {
                objectiveValue = Math.Abs(objectiveValue);
            }

            result.AppendLine($"Objective Value: {objectiveValue:0.####}");
        }
    }
}

