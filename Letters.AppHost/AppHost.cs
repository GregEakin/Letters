var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

var blobs = storage.AddBlobs("blobs");
var tables = storage.AddTables("tables");

builder.AddProject<Projects.Letters>("letters")
    .WithReference(blobs)
    .WithReference(tables)
    .WaitFor(blobs)
    .WaitFor(tables);

builder.Build().Run();