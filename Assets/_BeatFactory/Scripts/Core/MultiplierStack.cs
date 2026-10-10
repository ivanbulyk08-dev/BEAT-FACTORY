using System;
using System.Collections.Generic;

namespace BeatFactory
{
    /// <summary>
    /// Combines income multipliers from many systems (fame, genre, studio, artists, boosts).
    /// Each system registers a function that returns its current multiplier and calls
    /// NotifyChanged() when that value changes, so the UI can refresh income numbers.
    /// </summary>
    public class MultiplierStack
    {
        readonly List<KeyValuePair<string, Func<double>>> sources = new List<KeyValuePair<string, Func<double>>>();

        /// <summary>Fired when any multiplier in the stack changed.</summary>
        public event Action Changed;

        public void Register(string name, Func<double> source) =>
            sources.Add(new KeyValuePair<string, Func<double>>(name, source));

        /// <summary>All multipliers multiplied together.</summary>
        public double Total
        {
            get
            {
                double total = 1.0;
                for (int i = 0; i < sources.Count; i++) total *= sources[i].Value();
                return total;
            }
        }

        /// <summary>Name and value of every source (for a stats screen).</summary>
        public IEnumerable<KeyValuePair<string, double>> Breakdown()
        {
            foreach (var s in sources) yield return new KeyValuePair<string, double>(s.Key, s.Value());
        }

        public void NotifyChanged() => Changed?.Invoke();
    }
}
