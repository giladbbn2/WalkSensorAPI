using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WalkSensorAPI
{
    public class InCollectDatapoints
    {
        // 32 chars
        public string device_id { get; set; }
        
        public InDatapoint[] datapoints { get; set; }
        
        // in seconds, when this message was signed, signature valid for one hour
        public long message_uts { get; set; }
        
        // 32 chars of an md5 digest
        public string hmacmd5_digest { get; set; }

        // random 32 chars used to sign the message
        public string hmacmd5_salt { get; set; }
    }

}
