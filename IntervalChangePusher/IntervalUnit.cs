using System;
using System.Collections.Generic;
using System.Text;

namespace IntervalChangePusherLib
{
    public class IntervalUnit
    {
        public int UnitInMilliseconds { get; }

        public IntervalUnit(int unitInMilliseconds)
        {
            UnitInMilliseconds = unitInMilliseconds;
        }
    }
}
