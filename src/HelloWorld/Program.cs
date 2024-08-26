﻿using Samples.HelloWorld;
using static System.Console;

var services = new ServiceCollection()
    // IncrementalBuilder service is the most important piece, check it out.
    .AddFusion(f => f.AddService<IncrementalBuilder>())
    .BuildServiceProvider();

var builder = services.GetRequiredService<IncrementalBuilder>();

// Creating projects
Project pAbstractions = new("Abstractions");
Project pClient = new("Client", pAbstractions.Id);
Project pUI = new("UI", pClient.Id);
Project pServer = new("Server", pUI.Id);
Project pConsoleClient = new("ConsoleClient", pClient.Id);
Project pAll = new("All", pServer.Id, pConsoleClient.Id);
var projects = new [] { pAbstractions, pClient, pUI, pServer, pConsoleClient, pAll };

WriteLine("Projects:");
var index = 1;
foreach (var project in projects) {
    WriteLine($"{index++}. {project}");
    await builder.AddOrUpdate(project);
}

Project InputProject(string prompt)
{
    while (true) {
        WriteLine($"{prompt} Type 1...{projects.Length}:");
        var input = ReadLine();
        if (int.TryParse(input, out var index) && index >= 1 && index <= projects.Length)
            return projects[index - 1];
        WriteLine("Wrong input.");
    }
}

var watchedProject = InputProject("Which project do you want to continuously rebuild? Pick 6 if you don't know.");
_ = Task.Run(async () => {
    WriteLine($"Watching: {watchedProject}");
    var computed = await Computed.Capture(() => builder.GetOrBuild(watchedProject.Id, default));
    while (true) {
        WriteLine($"* Build result: {computed.Value}");
        await computed.WhenInvalidated();
        // Computed instances are ~ immutable, so update means getting a new one
        computed = await computed.Update();
    }
});

// Notice the code below doesn't even know there are some IComputed, etc.
while (true) {
    await Task.Delay(1000); // Let's give a chance for building task to do its job
    var invProject = InputProject("Project to invalidate?");
    builder.InvalidateGetOrBuildResult(invProject.Id);
}
