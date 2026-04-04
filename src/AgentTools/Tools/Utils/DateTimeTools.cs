using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace RichStokoe.AgentTools.Utils
{
    public static class DateTimeTools
    {
        [Description("Get the current time where the user is. Don't cache the output of this tool, come back each time to get the latest time")]
        internal static string Get_Current_Time()
        => DateTime.Now.ToLongTimeString();

        [Description("Get the current date where the user is. Don't cache the output of this tool, come back each time to get the latest date")]
        internal static string Get_Current_Date()
        => DateTime.Now.ToLongDateString();
    }
}