using NPoco;

namespace Umbraco.Community.Sanitiser.Models;

[TableName("umbracoCacheInstruction")]
public class UmbracoCacheInstruction
{
    [Column("jsonInstruction")] public string JsonInstruction { get; set; } = string.Empty;
}
