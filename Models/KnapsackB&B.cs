using System;

namespace LP_Solver.Models
{
    internal class KnapsackItem
    {
        public int Index { get; }
        public int Weight { get; }
        public int Value { get; }
        public double Ratio => (double)Value / Weight;

        public KnapsackItem(int index, int weight, int value)
        {
            if (weight <= 0) throw new ArgumentException("Weight must be > 0");
            if (value < 0) throw new ArgumentException("Value must be >= 0");
            Index = index;
            Weight = weight;
            Value = value;
        }
    }
}