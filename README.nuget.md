# INTRODUCTION 
Common.Diagnostics is a .Net package that provides readable log with application execution flow to .Net Log providers such as Log4Net, Serilog or Application Insights, Console, EventLog and Debug DotNet Log providers.<br>
This makes the application flow fully observable, still without compromises on performance.

Articles:
- [HOWTO - Make your application flow observable.md](HOWTO%20-%20Make%20your%20application%20flow%20observable.md): explores how to use diginsight to fully expose our application exeution flow.
- [HOWTO - Avoid performance impacts using diginsight telemetry.md](HOWTO%20-%20Avoid%20performance%20imacts%20using%20diginsight%20telemetry.md): explores how we can do this ensuring no impact on application performance.

add telemetry to your methods with the following instruction 

```c#
using (var sec = this.GetCodeSection())
```

write information to the listeners with the following instructions

```c#
sec.Debug("this is a debug trace", "User");
sec.Information("this is a Information trace", "Raw");
sec.Warning("this is a Warning trace", "User.Report");
sec.Error("this is a error trace", "Resource");
```

Common.Diagnostics component is supported on .Net Framework 4.6.2+ and .Net Core 3.0+.
Visit [telemetry][] for more information.
[telemetry]: https://github.com/diginsight/telemetry/
