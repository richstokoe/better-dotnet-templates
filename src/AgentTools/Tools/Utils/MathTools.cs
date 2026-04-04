using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace RichStokoe.AgentTools.Utils
{
    public class MathTools
    {
        [Description("Adds a sequence of decimal numbers together, returning the sum.")]
        internal static decimal Add_Numbers(IEnumerable<decimal> numbers)
        => numbers.Sum();
    }
}