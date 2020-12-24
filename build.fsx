#r "paket:
    nuget FSharp.Core ~> 5 prerelease
    nuget Fake.Core.Target ~> 5 prerelease
    nuget Fake.IO.FileSystem ~> 5 prerelease
    nuget BlackFox.CommandLine ~> 1 prerelease //"

#if !FAKE
#load ".fake/build.fsx/intellisense.fsx"
#endif

open BlackFox.CommandLine
open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open System.Runtime.InteropServices

Target.initEnvironment ()

// let profile = "clang-11"
// let profile = "vs2019-preview"

let workingDir = __SOURCE_DIRECTORY__ </> "tmp"

module BuildEnv =
    type BuildType =
        | Debug
        | Release
        member self.AsString =
            match self with
            | Debug -> "Debug"
            | Release -> "Release"
    type Flavor =
        { Profile: string
          Type: BuildType }

    let makeFolderName (pfx: string) (flavor: Flavor) =
        workingDir </> (sprintf "%s.%s-%s" pfx flavor.Profile flavor.Type.AsString)
    let buildFolder (flavor: Flavor): string =
        makeFolderName "Build" flavor
    let packageFolder (flavor: Flavor): string =
        makeFolderName "Package" flavor
    let taskName (pfx: string) (flavor: Flavor): string = sprintf "%s.%s-%s" pfx flavor.Profile flavor.Type.AsString

    let toSetting (flavor: Flavor) (c: CmdLine): CmdLine =
        c
        |> CmdLine.appendPrefix "--profile" flavor.Profile
        |> CmdLine.appendPrefix "--settings" (sprintf "build_type=%s" flavor.Type.AsString)

    let buildTypes = [| Debug; Release |]
    let profiles =
        if RuntimeInformation.IsOSPlatform OSPlatform.OSX then
            [| "default"; "clang-11"; "gcc-10" |]
        elif RuntimeInformation.IsOSPlatform OSPlatform.Linux then
            [| "default" |]
        elif RuntimeInformation.IsOSPlatform OSPlatform.Windows then
            [| "vs2019-preview" |]
        else
            failwith "unknown host platform"

    let flavors =
        Seq.allPairs profiles buildTypes
        |> Seq.map (fun (p, t) -> { Profile = p; Type = t })
//let buildTypes = [|"Debug"|]

Target.create "Rebuild" ignore

Target.create "Clean"
<| fun _ -> Shell.rm_rf workingDir

"Clean" ==> "Rebuild"

Target.create "EnsureWorkingDir"
<| fun _ -> Directory.ensure workingDir

"Clean" ?=> "EnsureWorkingDir"

Target.create "Source"
<| fun _ ->
    let srcDir = workingDir </> "source"
    Shell.rm_rf srcDir
    CmdLine.empty
    |> CmdLine.append "source"
    |> CmdLine.append __SOURCE_DIRECTORY__
    |> CmdLine.appendPrefix "--source-folder" srcDir
    |> CmdLine.toArray
    |> CreateProcess.fromRawCommand "conan"
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

"EnsureWorkingDir" ==> "Source"

Target.create "Install" ignore

let installTask (flavor: BuildEnv.Flavor) =
    let taskName = flavor |> BuildEnv.taskName "Install"
    let installDir = flavor |> BuildEnv.buildFolder

    Target.create taskName
    <| fun _ ->
        CmdLine.empty
        |> CmdLine.append "install"
        |> CmdLine.append __SOURCE_DIRECTORY__
        |> CmdLine.appendPrefix "--install-folder" installDir
        // |> CmdLine.appendPrefix "--build" "fmt"
        |> BuildEnv.toSetting flavor
        |> CmdLine.toArray
        |> CreateProcess.fromRawCommand "conan"
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore

    "Source" ?=> taskName |> ignore
    taskName ==> "Install" |> ignore

BuildEnv.flavors |> Seq.iter installTask

Target.create "Build" ignore

let buildTask (flavor: BuildEnv.Flavor) =
    let taskName = flavor |> BuildEnv.taskName "Build"
    let sourceDir = workingDir </> "source"
    let buildDir = flavor |> BuildEnv.buildFolder

    Target.create taskName
    <| fun _ ->
        CmdLine.empty
        |> CmdLine.append "build"
        |> CmdLine.append __SOURCE_DIRECTORY__
        |> CmdLine.appendPrefix "--source-folder" sourceDir
        |> CmdLine.appendPrefix "--build-folder" buildDir
        |> CmdLine.toArray
        |> CreateProcess.fromRawCommand "conan"
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore

    (flavor |> BuildEnv.taskName "Install")
    ==> taskName
    |> ignore

    taskName ==> "Build" |> ignore

BuildEnv.flavors |> Seq.iter buildTask

Target.create "Package" ignore

let packageTask (flavor: BuildEnv.Flavor) =
    let taskName = flavor |> BuildEnv.taskName "Package"
    let sourceDir = workingDir </> "source"
    let buildDir = flavor |> BuildEnv.buildFolder
    let packageDir = flavor |> BuildEnv.packageFolder

    Target.create taskName
    <| fun _ ->
        CmdLine.empty
        |> CmdLine.append "package"
        |> CmdLine.append __SOURCE_DIRECTORY__
        |> CmdLine.appendPrefix "--source-folder" sourceDir
        |> CmdLine.appendPrefix "--build-folder" buildDir
        |> CmdLine.appendPrefix "--package-folder" packageDir
        |> CmdLine.toArray
        |> CreateProcess.fromRawCommand "conan"
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore

    (flavor |> BuildEnv.taskName "Build")
    ==> taskName
    |> ignore

    taskName ==> "Package" |> ignore

BuildEnv.flavors |> Seq.iter packageTask

Target.create "Export" ignore

let exportTask (flavor: BuildEnv.Flavor) =
    let taskName = flavor |> BuildEnv.taskName "Export"
    let sourceDir = workingDir </> "source"
    let buildDir = flavor |> BuildEnv.buildFolder
    let packageDir = flavor |> BuildEnv.packageFolder

    Target.create taskName
    <| fun _ ->
        CmdLine.empty
        |> CmdLine.append "export-pkg"
        |> CmdLine.append "--force"
        |> CmdLine.append __SOURCE_DIRECTORY__
        |> CmdLine.append "objectx/testing"
        // |> CmdLine.appendPrefix "--source-folder" sourceDir
        // |> CmdLine.appendPrefix "--build-folder" buildDir
        |> CmdLine.appendPrefix "--package-folder" packageDir
        |> BuildEnv.toSetting flavor
        |> CmdLine.toArray
        |> CreateProcess.fromRawCommand "conan"
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore

    (flavor |> BuildEnv.taskName "Package")
    ==> taskName
    |> ignore

    taskName ==> "Export" |> ignore

BuildEnv.flavors |> Seq.iter exportTask

Target.create "Test" ignore

let testTask (flavor: BuildEnv.Flavor) =
    let taskName = flavor |> BuildEnv.taskName "Test"

    Target.create taskName
    <| fun _ ->
        CmdLine.empty
        |> CmdLine.append "test"
        |> CmdLine.append "test_package"
        |> CmdLine.append "rapidcheck/1.0.9@objectx/testing"
        |> BuildEnv.toSetting flavor
        |> CmdLine.toArray
        |> CreateProcess.fromRawCommand "conan"
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore

    (flavor |> BuildEnv.taskName "Export")
    ==> taskName
    |> ignore

    taskName ==> "Test" |> ignore

BuildEnv.flavors |> Seq.iter testTask

Target.create "Create" ignore

let createTask (flavor: BuildEnv.Flavor) =
    let taskName = flavor |> BuildEnv.taskName "Create"

    Target.create taskName
    <| fun _ ->
        CmdLine.empty
        |> CmdLine.append "create"
        |> CmdLine.append __SOURCE_DIRECTORY__
        |> CmdLine.append "objectx/testing"
        |> BuildEnv.toSetting flavor
        |> CmdLine.toArray
        |> CreateProcess.fromRawCommand "conan"
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore

    taskName ==> "Create" |> ignore
// "Test" ==> "Create" |> ignore

BuildEnv.flavors |> Seq.iter createTask
"Source" ==> "Rebuild"
"Test" ==> "Rebuild"
// "Create" ==> "Rebuild"

Target.runOrDefaultWithArguments "Rebuild"
