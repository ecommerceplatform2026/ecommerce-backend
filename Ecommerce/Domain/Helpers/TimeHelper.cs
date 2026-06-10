using System;

namespace Domain.Helpers
{
    public static class TimeHelper
    {
        public static DateTime GetTime()
        {
            return DateTime.UtcNow;
        }

        public static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Local
                ? value.ToUniversalTime()
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
