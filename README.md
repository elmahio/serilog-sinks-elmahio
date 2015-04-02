# Serilog.Sinks.Elmahio

[![Build status](https://ci.appveyor.com/api/projects/status/j4rsru1m0lhkfwc4/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-elmahio/branch/master)

A Serilog sink that writes events to elmah.io. [Elmah.io](http://www.elmah.io] is a cloud hosted solution to capture errors. Register for an account at their website and use the provided GUID in the configuration for serilog.

**Package** - [Serilog.Sinks.ElmahIO](http://nuget.org/packages/serilog.sinks.ElmahIO]
| **Platforms** - .NET 4.5

```csharp
var log = new LoggerConfiguration()
    .WriteTo.ElmahIO(new Guid("{your guid}"))
    .CreateLogger();
```

As elmah.io is primarily used for error tracking, the default level is set to `Error`. You can override this, but not all the data on the site is filled in as not all the Serilog properties can be matched. 
