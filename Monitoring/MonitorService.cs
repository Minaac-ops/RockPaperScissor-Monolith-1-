using System;
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Core;
using Serilog.Enrichers.Span;

namespace Monitoring
{
    public static class MonitorService
    {
        public static readonly string ServiceName = Assembly.GetCallingAssembly().GetName().Name ?? "Unknown";
        public static TracerProvider TracerProvider;
        public static ActivitySource ActivitySource = new ActivitySource(ServiceName);
        
        public static ILogger Log => Serilog.Log.Logger;

        static MonitorService()
        {
            //OpenTelemetry
            TracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddConsoleExporter()
                .AddZipkinExporter()
                .AddSource(ActivitySource.Name)
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName))
                .Build();
            
            //Serilog
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose() //any level, starting from verbose
                .WriteTo.Console() //where to log to, needs the serilog sync console nuget
                .WriteTo.Seq("http://localhost:5341")
                .Enrich.WithSpan()
                .CreateLogger();
        }
    }
}