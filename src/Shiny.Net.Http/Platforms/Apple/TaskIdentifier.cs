using System;
using System.IO;

namespace Shiny.Net.Http;


public struct TaskIdentifier
{
    public static readonly TaskIdentifier Invalid = new TaskIdentifier(false, null, null);


    public static TaskIdentifier Create(FileInfo file)
    {
        var id = Guid.NewGuid().ToString();
        return new TaskIdentifier(true, id, file);
    }


    public static TaskIdentifier FromString(string value)
    {
        var values = value.Split('|');
        if (values.Length != 2)
            return Invalid;

        var id = values[0];
        var file = new FileInfo(values[1]);
        return new TaskIdentifier(true, id, file);
    }


    TaskIdentifier(bool valid, string? id, FileInfo? file)
    {
        this.IsValid = valid;
        this.Value = id;
        this.File = file;
    }


    public string? Value { get; }
    public FileInfo? File { get; }
    public bool IsValid { get; }
    public override string ToString() => this.IsValid ? $"{this.Value}|{this.File?.FullName}" : "";
}
