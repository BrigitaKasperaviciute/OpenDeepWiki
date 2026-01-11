// This file is used to configure code analysis and suppress warnings
// that are specific to the integration test project

using System.Diagnostics.CodeAnalysis;

// Suppress warnings from FastService Analyzer-generated code
[assembly: SuppressMessage("Compiler", "CS0103", Justification = "FastService analyzer-generated code issue")]
[assembly: SuppressMessage("Compiler", "CS1955", Justification = "FastService analyzer-generated code issue")]
[assembly: SuppressMessage("Compiler", "CS7014", Justification = "FastService analyzer-generated code issue")]
[assembly: SuppressMessage("Compiler", "CS0121", Justification = "FastService analyzer-generated code issue")]
