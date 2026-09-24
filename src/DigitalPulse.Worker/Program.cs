using DigitalPulse.Application;
using DigitalPulse.Infrastructure;
using DigitalPulse.Infrastructure.Configuration;

LocalEnvFile.Load();
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<global::DigitalPulse.Worker.PulseWorker>();
var host = builder.Build();
await host.RunAsync();
