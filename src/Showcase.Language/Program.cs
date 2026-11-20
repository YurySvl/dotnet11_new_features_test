using Showcase.Language;

// Run every C# 15 demo, or one by name: dotnet run -- unions|closed|collections|labels
var only = args.FirstOrDefault()?.ToLowerInvariant();

if (only is null or "unions") UnionsDemo.Run();
if (only is null or "closed") ClosedHierarchiesDemo.Run();
if (only is null or "collections") CollectionExpressionArgumentsDemo.Run();
if (only is null or "labels") LabeledBreakContinueDemo.Run();
