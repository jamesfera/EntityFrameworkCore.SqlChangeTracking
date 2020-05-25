using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace EntityFrameworkCore.SqlChangeTracking.Logging
{
    public static class LoggingUtils
    {
        public static bool LogErrorWithContext(this ILogger logger, Exception ex, string message, params object[] args)
        {
            logger.LogError(ex, message, args);
            return true;
        }
        
    }
}
