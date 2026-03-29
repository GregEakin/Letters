var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Letters>("letters");

builder.Build().Run();
