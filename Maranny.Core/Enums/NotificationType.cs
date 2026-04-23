using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maranny.Core.Enums
{
    public enum NotificationType
    {
        General = 1,
        BookingConfirmation = 2,
        BookingCancellation = 3,
        PaymentReceived = 4,
        SessionReminder = 5,
        NewReview = 6,
        VerificationUpdate = 7
    }
}