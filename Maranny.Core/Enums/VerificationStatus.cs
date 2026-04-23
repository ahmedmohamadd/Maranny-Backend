using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maranny.Core.Enums
{
    public enum VerificationStatus
    {
        Pending = 1,
        Verified = 2,
        Rejected = 3,
        Approved = 2  // Alias for Verified
    }
}