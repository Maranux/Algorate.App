#tool "nuget:?package=GitReleaseNotes"
#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=gitlink"

var target = Argument("target", "Default");
var outputDir = "./artifacts/";
var solutionPath = "./Algorate.App.sln";
var projectJson = "./src/Algorate/project.json";
var testProjectJson = "./src/Algorate.XUnitTests/project.json";

Task("Clean")
    .Does(() => {
        if (DirectoryExists(outputDir))
        {
            DeleteDirectory(outputDir, recursive:true);
        }
        CreateDirectory(outputDir);
    });

Task("Restore")
    .Does(() => {
        DotNetCoreRestore("src");
    });

GitVersion versionInfo = null;
Task("Version")
    .Does(() => {
        GitVersion(new GitVersionSettings{
            UpdateAssemblyInfo = true,
            OutputType = GitVersionOutput.BuildServer
        });
        versionInfo = GitVersion(new GitVersionSettings{ OutputType = GitVersionOutput.Json });
        // Update project.json
        var updatedProjectJson = System.IO.File.ReadAllText(projectJson)
            .Replace("1.0.0-*", versionInfo.NuGetVersion);

        System.IO.File.WriteAllText(projectJson, updatedProjectJson);
    });

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Version")
    .IsDependentOn("Restore")
    .Does(() => {
        MSBuild(solutionPath);
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() => {
        DotNetCoreTest("./src/Algorate.XUnitTests/");
    });

Task("Package")
    .IsDependentOn("Test")
    .Does(() => {
        //GitLink("./", new GitLinkSettings { ArgumentCustomization = args => args.Append("-include Specify,Specify.Autofac") });

        GenerateReleaseNotes();

        PackageProject("Algorate", projectJson);
        PackageProject("Algorate.XUnitTests", testProjectJson);

        if (AppVeyor.IsRunningOnAppVeyor)
        {
            foreach (var file in GetFiles(outputDir + "**/*"))
                AppVeyor.UploadArtifact(file.FullPath);
        }
    });

private void PackageProject(string projectName, string projectJsonPath)
{
    var settings = new DotNetCorePackSettings
        {
            OutputDirectory = outputDir,
            NoBuild = true
        };

    DotNetCorePack(projectJsonPath, settings);

    System.IO.File.WriteAllLines(outputDir + "artifacts", new[]{
        "nuget:" + projectName + "." + versionInfo.NuGetVersion + ".nupkg",
        "nugetSymbols:" + projectName + "." + versionInfo.NuGetVersion + ".symbols.nupkg",
        "releaseNotes:releasenotes.md"
    });
}    

private void GenerateReleaseNotes()
{
        var releaseNotesExitCode = StartProcess(
        @"tools\GitReleaseNotes\tools\gitreleasenotes.exe", 
        new ProcessSettings { Arguments = ". /o artifacts/releasenotes.md" });
    if (string.IsNullOrEmpty(System.IO.File.ReadAllText("./artifacts/releasenotes.md")))
        System.IO.File.WriteAllText("./artifacts/releasenotes.md", "No issues closed since last release");

    if (releaseNotesExitCode != 0) throw new Exception("Failed to generate release notes");
}

Task("Default")
    .IsDependentOn("Package");

RunTarget(target);