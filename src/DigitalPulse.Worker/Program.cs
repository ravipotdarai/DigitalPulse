var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<global::DigitalPulse.Worker.PulseWorker>();
var host = builder.Build();
await host.RunAsync();
