# ESAPIUtilities

A C# Class library to contain random reuseable methods and such for the purposes of ESAPI scripting.
Include this as a git submodule to use it in other projects! Here's the process:
1. If your main project is a git repo, go to powershell and call `git submodule add https:\\github.com\9-k\ESAPIUtilities`.
2. If it's not a git repo, do `git clone https:\\github.com\9-k\ESAPIUtilities`.
3. Right click the solution file, go to Add > Existing Project and navigate to the ESAPIUtilities `.csproj` file. Select it.
4. In your main project, right click "References" > Add reference > Projects > ESAPIUtilities.
5. In your main project, also Manage NuGet projects > download ILMerge and ILMerge.MSBuild.Task. (yes, ILMerge is deprecated, but I'm on Eclipse v16, and .NET Framework 4.6.2 is super deprecated too... so it cancels out, right?)
6. Now when building your project, everything will get rolled up into a nice single .esapi.dll!
