using System.IO;
internal static class TestPaths
{
 static readonly string Root = FindRoot();
 public static string Artifacts => Path.Combine(Root, "artifacts");
 public static string Artifact(string name) => Path.Combine(Artifacts, name);
 public static string Source(string name) => Path.Combine(Root, "src", "Field", name);
 static string FindRoot()
 {
  for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
   if (File.Exists(Path.Combine(folder.FullName, "src", "Field", "Field.csproj"))) return folder.FullName;
  throw new DirectoryNotFoundException("Run tests from a checkout of the FIELD repository.");
 }
}
