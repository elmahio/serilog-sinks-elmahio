# Serilog.Sinks.ElmahIo

A Serilog sink that writes events to [elmah.io](https://elmah.io).

[![Build status](https://github.com/elmahio/serilog-sinks-elmahio/workflows/build/badge.svg)](https://github.com/elmahio/serilog-sinks-elmahio/actions?query=workflow%3Abuild) [![NuGet Version](https://img.shields.io/nuget/v/Serilog.Sinks.ElmahIO.svg?style=flat)](https://www.nuget.org/packages/Serilog.Sinks.ElmahIO/)

To configure the elmah.io sink, call the `ElmahIo` method as part of your log configuration:

```csharp
var log = new LoggerConfiguration()
    .WriteTo.ElmahIo(new ElmahIoSinkOptions("API_KEY", new Guid("LOG_ID")))
    .CreateLogger();
```

The sink captures all levels, but respect the minimum level configured on LoggerConfiguration. Serilog properties are converted to elmah.io's concept of custom variables, when logging new events.

[Documentation](https://docs.elmah.io/logging-to-elmah-io-from-serilog/)