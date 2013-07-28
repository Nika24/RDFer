using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JoshanMahmud.SemanticWeb.RdfConversion
{
    public enum eCounterIteration
    {
        Increment,
        Decrement
    } ;

    public class Counter
    {
        /// <summary>
        /// The initial value the counter started at
        /// </summary>
        public int InitialValue { get; private set; }

        /// <summary>
        /// The current counter value
        /// </summary>
        public int CounterValue;

        /// <summary>
        /// The nature of the iteration (increment / decrement)
        /// </summary>
        public eCounterIteration Iteration;

        /// <summary>
        /// The name of this Counter Variable
        /// </summary>
        public string CounterName { get; private set; }

        public Counter(string counterName)
        {
            CounterName = counterName;
            InitialValue = 0;
            CounterValue = InitialValue;
            Iteration = eCounterIteration.Increment;
        }

        public Counter(string counterName, int initialValue, eCounterIteration iteration)
        {
            CounterName = counterName;
            InitialValue = initialValue;
            CounterValue = InitialValue;
            Iteration = iteration;
        }

        public void IterateCounter()
        {
            switch(Iteration)
            {
                case eCounterIteration.Increment:
                    CounterValue++;
                    break;
                case eCounterIteration.Decrement:
                    CounterValue--;
                    break;
            }
        }
    }
}
