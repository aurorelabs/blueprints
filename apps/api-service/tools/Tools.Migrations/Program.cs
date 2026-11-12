using System;

Console.WriteLine("Tools @ Migrations: use it through the EF tools, for example:");
Console.WriteLine("  dotnet ef migrations add InitialDb --project tools/Tools.Migrations --output-dir History -- \"<connection string>\"");
Console.WriteLine("  dotnet ef database update --project tools/Tools.Migrations -- \"<connection string>\"");
