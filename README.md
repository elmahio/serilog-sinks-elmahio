# Serilog.Sinks.Elmahio

A Serilog sink that writes events to elmah.io. [elmah.io](https://elmah.io) is a cloud hosted solution to capture log messages. Register for an account at their website and use the provided API key and GUID in the configuration for Serilog.

[![Build status](https://ci.appveyor.com/api/projects/status/j4rsru1m0lhkfwc4/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-elmahio/branch/master) [![NuGet Version](http://img.shields.io/nuget/v/Serilog.Sinks.ElmahIO.svg?style=flat)](https://www.nuget.org/packages/Serilog.Sinks.ElmahIO/)

To configure the elmah.io sink, call the `ElmahIo` method as part of your log configuration:

```csharp
var log = new LoggerConfiguration()
    .WriteTo.ElmahIo("{your api key}", new Guid("{your log id}"))
    .CreateLogger();
```

The sink captures all levels, but respect the minimum level configured on LoggerConfiguration. Serilog properties are converted to elmah.io's concept of custom variables, when logging new events.

[Documentation](https://docs.elmah.io/logging-to-elmah-io-from-serilog/)