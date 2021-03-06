#r @"packages/FAKE/tools/FakeLib.dll"
open System
open Fake
open Fake.AssemblyInfoFile

let config = getBuildParamOrDefault "config" "Release"
let baseVersion = getBuildParamOrDefault "baseVersion" "1.10.2"
let preRelease = getBuildParamOrDefault "preRelease" "local"
let getComputedBuildNumber() = 
    let result = Git.CommandHelper.runSimpleGitCommand currentDirectory "describe HEAD^1 --tags --long --match \"v[0-9].[0-9].[0-9]*\""
    let m = System.Text.RegularExpressions.Regex.Match(result, @"-(\d+)-")
    m.Groups.[1].Value

let buildNumber = getBuildParamOrDefault "buildNumber" (getComputedBuildNumber())
let version = baseVersion + "." + buildNumber
let semVersion = 
    match preRelease with
    | "build" | "local" -> baseVersion + "-" + preRelease + "-" + buildNumber.PadLeft(4, '0')
    | "#release#" -> baseVersion
    | _ -> baseVersion + "-" + preRelease

let baseDir = currentDirectory
let buildDir = baseDir
let srcDir = baseDir
let toolsDir = baseDir @@ "tools"
let artifactsDir = baseDir @@ "artifacts"
let binDir = artifactsDir @@ "bin"
let binDir35 = binDir @@ "net35"
let testResultsDir = artifactsDir @@ "test_results"
let docsDir = artifactsDir @@ "docs"

let slnFile = 
    match isMono with
    | true -> srcDir @@ "CSharpDriver-Mono.sln"
    | false -> srcDir @@ "CSharpDriver.sln"

let asmFile = srcDir @@ "GlobalAssemblyInfo.cs"
let docsFile = baseDir @@ "Docs" @@ "Api" @@ "CSharpDriverDocs.shfbproj"
let versionFile = artifactsDir @@ "version.txt"

let nuspecFile = buildDir @@ "mongocsharpdriver.nuspec"
let nuspecBuildFile = buildDir @@ "MongoDB.Driver-Build.nuspec"
let licenseFile = baseDir @@ "License.txt"
let releaseNotesFile = baseDir @@ "Release Notes" @@ "Release Notes v" + (NormalizeVersion baseVersion) + ".md"

let versionArtifactFile = artifactsDir @@ "version.txt"
let docsArtifactFile = artifactsDir @@ "CSharpDriver-" + semVersion + ".chm"
let docsArtifactZipFile = artifactsDir @@ "CSharpDriverDocs-" + semVersion + "-html.zip"
let zipArtifactFile = artifactsDir @@ "CSharpDriver-" + semVersion + ".zip"

MSBuildDefaults <- { MSBuildDefaults with Verbosity = Some(Minimal) }

// Targets
Target "Clean" (fun _ ->
    CleanDir artifactsDir
    DeleteDir artifactsDir
)

Target "OutputVersion" (fun _ ->
    ensureDirectory artifactsDir

    let lines = 
        [ sprintf "baseVersion=%s" baseVersion 
          sprintf "preRelease=%s" preRelease 
          sprintf "buildNumber=%s" buildNumber 
          sprintf "semVersion=%s" semVersion ]

    WriteFile versionFile lines
)

Target "AssemblyInfo" (fun _ ->
    let commitish = Git.Information.getCurrentSHA1 currentDirectory
    let commitDate = 
        let dt = Git.CommandHelper.runSimpleGitCommand baseDir "log -1 --date=iso --pretty=format:%ad"
        System.DateTime.Parse(dt).ToString("yyyy-MM-dd HH:mm:ss")
    
    let info = "{ version: '" + version + "', semver: '" + semVersion.ToString() + "', commit: '" + commitish + "', commitDate: '" + commitDate + "' }"
    
    ActivateFinalTarget "Teardown"
    ReplaceAssemblyInfoVersions (fun p ->
        { p with
            OutputFileName = asmFile
            AssemblyVersion = version
            AssemblyInformationalVersion = info
            AssemblyFileVersion = version
            AssemblyConfiguration = config })
)

Target "Build" (fun _ ->
    !! "./**/packages.config"
    |> Seq.iter (RestorePackage (fun x -> { x with OutputPath = srcDir @@ "packages" }))


    let properties = ["Configuration", config
                      "TargetFrameworkVersion", "v3.5"]

    [slnFile]
        |> MSBuild binDir35 "Build" properties
        |> Log "Build: "
)

Target "Test" (fun _ ->
    if not <| directoryExists binDir35 then new Exception(sprintf "Directory %s does not exist." binDir35) |> raise
    ensureDirectory testResultsDir

    let framework = ref "net-3.5"
    let mutable testsDir = !! (binDir35 @@ "*Tests*.dll")
    if isMono then
        testsDir <- testsDir -- (binDir35 @@ "*VB.Tests*.dll")
        framework := "mono-4.0"

    testsDir
        |> NUnit (fun p -> 
            { p with 
                OutputFile = testResultsDir @@ getBuildParamOrDefault "testResults" "test-results.xml"
                DisableShadowCopy = true
                ShowLabels = false
                Framework = !framework
                IncludeCategory = getBuildParamOrDefault "testInclude" ""
                ExcludeCategory = getBuildParamOrDefault "testExclude" ""
            })
)

Target "Docs" (fun _ ->
    DeleteFile docsArtifactFile
    DeleteFile docsArtifactZipFile
    ensureDirectory docsDir
    CleanDir docsDir

    let preliminary =
        match preRelease with
        | "#release#" -> "False"
        | _ -> "True"

    let properties = ["Configuration", config
                      "OutputPath", docsDir
                      "CleanIntermediate", "True"
                      "Preliminary", preliminary
                      "HelpFileVersion", version]

    [docsFile]
        |> MSBuild artifactsDir "" properties
        |> Log "Docs: "

    Rename docsArtifactFile (docsDir @@ "CSharpDriverDocs.chm")
    Rename (docsDir @@ "index.html") (docsDir @@ "Index.html")

    !! (docsDir @@ "**/**.*")
        |> CreateZip docsDir docsArtifactZipFile "" DefaultZipLevel false
)

Target "Zip" (fun _ ->
    DeleteFile zipArtifactFile

    checkFileExists docsArtifactFile
    checkFileExists licenseFile
    checkFileExists releaseNotesFile

    let files =
        [ binDir35 @@ "MongoDB.Bson.dll"
          binDir35 @@ "MongoDB.Bson.pdb"
          binDir35 @@ "MongoDB.Bson.xml"
          binDir35 @@ "MongoDB.Driver.dll"
          binDir35 @@ "MongoDB.Driver.pdb"
          binDir35 @@ "MongoDB.Driver.xml"
          licenseFile
          releaseNotesFile 
          docsArtifactFile ]

    files
        |> CreateZip artifactsDir zipArtifactFile "" DefaultZipLevel true
)

let createNuGetPackage file deps symbols =
    NuGetPack (fun p ->
      { p with
          Dependencies = deps
          Version = semVersion
          OutputPath = artifactsDir
          WorkingDir = baseDir
          SymbolPackage = if symbols then NugetSymbolPackage.Nuspec else NugetSymbolPackage.None })
      file

Target "NuGetPack" (fun _ ->
    !!(artifactsDir @@ "*.nupkg") |> DeleteFiles

    match preRelease with
    | "build" -> createNuGetPackage nuspecBuildFile [] true
    | _ -> createNuGetPackage nuspecFile [] true
)

let pushNugetPackage project =
    NuGetPublish (fun x -> 
      { x with 
          PublishUrl = getBuildParamOrDefault "nugetSource" "https://www.myget.org/F/mongodb/api/v2/package"
          AccessKey = getBuildParam "nugetApiKey"
          OutputPath = artifactsDir
          WorkingDir = baseDir
          Project = project
          Version = semVersion })

Target "NuGetPush" (fun _ ->
    if not <| hasBuildParam "nugetApiKey" then new Exception("nugetApiKey must be specified to push nuget files.") |> raise

    match preRelease with
    | "build" -> pushNugetPackage (fileNameWithoutExt nuspecBuildFile)
    | _ -> pushNugetPackage (fileNameWithoutExt nuspecFile)
            
)

FinalTarget "Teardown" (fun _ ->
    let cmd = sprintf "checkout %s" asmFile
    let result = Git.CommandHelper.runSimpleGitCommand baseDir cmd
    ()
)


Target "NoOp" DoNothing
Target "Package" DoNothing
Target "Publish" DoNothing

"Clean"
    ==> "AssemblyInfo"
    ==> "Build"

"Docs"
    ==> "Zip"
    ==> "NuGetPack"
    ==> "Package"

"NuGetPush"
    ==> "Publish"

RunTargetOrDefault "Build"