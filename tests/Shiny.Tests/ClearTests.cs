using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Storage;

namespace Shiny.Tests;



public class ClearTests : AbstractShinyTests
{
    public ClearTests(ITestOutputHelper output) : base(output) { }


    [Fact(DisplayName = "Cleanup - HTTP Transfers")]
    public void CleanupHttpTransfers() 
    {
        this.DeleteFiles("*.transfers");
        this.TryDelete("upload.bin");
    }


    [Fact(DisplayName = "Cleanup - Repository")]
    public void CleanupRepo() => this.DeleteFiles("*.shiny");


    [Fact(DisplayName = "Cleanup - ALL")]
    public void CleanupAll()
    {
        this.CleanupHttpTransfers();
        this.CleanupRepo();
    }


    void DeleteFiles(string pattern, [CallerMemberName] string? func = null)
    {
        var files = Directory.GetFiles(FileSystem.AppDataDirectory, pattern);
        this.Log($"{files.Length} Found for {func} ({pattern})");

        foreach (var file in files)
        {
            var fi = new FileInfo(file);
            this.Log($"Deleting {fi.Name} ({fi.Length} bytes)");
            fi.Delete();
        }
    }


    void TryDelete(string file)
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, file);
        var fi = new FileInfo(path);

        if (fi.Exists)
        {
            this.Log($"Deleting {fi.Name} ({fi.Length} bytes)");
            fi.Delete();
        }
    }
}