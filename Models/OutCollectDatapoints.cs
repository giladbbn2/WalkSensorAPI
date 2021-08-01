using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WalkSensorAPI
{
    public class OutCollectDatapoints
    {
        public string device { get; set; }
        public long totalPoints { get; set; }
        public IList<OutDatapoint> points { get; set; }

    }
}
