var builder = DistributedApplication.CreateBuilder(args);

var tcpServer = builder.AddProject<Projects.Teltonika_AVL_Server>("server");

builder.AddProject<Projects.Teltonika_AVL_Simulator>("simulator")
    .WithEnvironment("TELTONIKA_HOST", "localhost")
    .WithEnvironment("TELTONIKA_PORT", "1234")
    .WithReference(tcpServer)
    .WaitFor(tcpServer);

builder.Build().Run();