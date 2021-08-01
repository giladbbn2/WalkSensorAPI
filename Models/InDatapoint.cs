using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WalkSensorAPI
{
    public class InDatapoint
    {
        // in milliseconds
        public long start_uts { get; set; }

        // in milliseconds
        public long end_uts { get; set; }

        // distance in meters
        public int distance { get; set; }
    }
}
