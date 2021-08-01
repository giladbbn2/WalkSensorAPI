using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WalkSensorAPI
{
    public class OutDatapoint
    {
        public int distance { get; set; }
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
    }
}
