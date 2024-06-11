# INTRODUCTION 
Often, the __most tricky__ or the __most critical__ __bugs__ can be hidden in the __application startup sequence__ or __application static methods__.<br>
As an example, startup configurations such as __connection string__ or __resources access keys__ may be wrong or missing.<br>
Also, __static contructors__ within the application or its dependencies may hide tricky bugs that are difficult to troubleshoot. 

Those places are difficult to troubleshoot as telemetry bey be not already active at the moment of execution.

Diginsight telemetry supports __full observability also for these parts__ by means of the `DeferredLoggerFactory` that enables __recording the application flow__ untill the telemetry infrastructure is set up.

Upon setup completion, telemetry recording is flushed right before the standard telemetry flow gathering.


# INTRODUCTION 

