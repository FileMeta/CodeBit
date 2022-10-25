using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace CodeBit
{
    static internal class Helpers
    {
        public static string ToStringConcise(this DateTimeOffset date)
        {
            if (date.TimeOfDay.Ticks == 0 && date.Offset.Ticks == 0)
            {
                return date.ToString("yyyy-MM-dd");
            }
            if (date.Ticks % 10000000 == 0)
            {
                return date.ToString("yyyy-MM-ddTHH:mm:ssK");
            }
            return date.ToString("o");
        }
    }
}
