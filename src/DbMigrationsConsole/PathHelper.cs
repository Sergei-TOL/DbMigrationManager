namespace DbMigrationsConsole;

public static class PathHelper
{
    public static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var githubWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        if (!string.IsNullOrEmpty(githubWorkspace))
        {
            return Path.Combine(githubWorkspace, path);
        }

        return Path.Combine(Directory.GetCurrentDirectory(), path);
    }
}
