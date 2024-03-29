﻿using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public interface IDiginsightActivitiesOptions
{
    LogLevel DefaultActivityLogLevel { get; }

    bool LogActivities { get; }
    bool RecordActivities { get; }
    bool WriteActivityActionAsPrefix { get; }

    bool RecordSpanDurations { get; }
}
