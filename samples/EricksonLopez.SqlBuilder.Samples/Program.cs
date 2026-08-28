// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Samples.Level01_QuickStart;
using EricksonLopez.SqlBuilder.Samples.Level02_FullConfiguration;
using EricksonLopez.SqlBuilder.Samples.Level03_RealUseCases;
using EricksonLopez.SqlBuilder.Samples.Level04_AdvancedIntegration;
using EricksonLopez.SqlBuilder.Samples.Level05_Processing;
using EricksonLopez.SqlBuilder.Samples.Level06_ErrorHandling;
using EricksonLopez.SqlBuilder.Samples.Level07_Scalability;
using EricksonLopez.SqlBuilder.Samples.Level08_Customization;
using EricksonLopez.SqlBuilder.Samples.Level09_Extensions;
using EricksonLopez.SqlBuilder.Samples.Level10_EnterpriseArchitecture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace EricksonLopez.SqlBuilder.Samples;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=========================================================");
        Console.WriteLine("    ERICKSONLOPEZ.SQLBUILDER - EXECUTABLE SHOWCASE       ");
        Console.WriteLine("=========================================================\n");

        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices((context, services) =>
            {
                // Configure observability
                services.AddOpenTelemetry()
                    .WithTracing(tracerProviderBuilder =>
                    {
                        tracerProviderBuilder
                            .AddSource(SqlBuilderDiagnostics.ActivitySource.Name)
                            .AddConsoleExporter();
                    })
                    .WithMetrics(meterProviderBuilder =>
                    {
                        meterProviderBuilder
                            .AddMeter(SqlBuilderDiagnostics.Meter.Name)
                            .AddConsoleExporter();
                    });
            });

        var host = builder.Build();
        
        // Associate logger factory with framework
        SqlBuilderDiagnostics.LoggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

        // Execute samples
        await QuickStartSample.RunAsync();
        await FullConfigurationSample.RunAsync();
        await RealUseCasesSample.RunAsync();
        await AdvancedIntegrationSample.RunAsync();
        await ProcessingSample.RunAsync();
        await ErrorHandlingSample.RunAsync();
        await ScalabilitySample.RunAsync();
        
        await CustomizationSample.RunAsync();
        await ExtensionsSample.RunAsync();
        await EnterpriseArchitectureSample.RunAsync();

        Console.WriteLine("\n=========================================================");
        Console.WriteLine("  All Showcase samples completed successfully.           ");
        Console.WriteLine("=========================================================");
    }
}




