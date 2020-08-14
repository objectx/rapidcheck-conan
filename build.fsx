#r "paket:
    nuget FSharp.Core ~> 4 prerelease
    nuget Fake.Core.Target ~> 5 prerelease
    nuget BlackFox.CommandLine ~> 1 prerelease
    nuget BlackFox.Fake.BuildTask
//"

#if !FAKE
#load ".fake/build.fsx/intellisense.fsx"
#endif

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open BlackFox.CommandLine
open BlackFox.Fake

Target.initEnvironment ()

let buildRoot = __SOURCE_DIRECTORY__ </> "00.BUILD"
let srcDir = buildRoot </> "source"

let cleanTask =
    BuildTask.createFn "Clean" []
    <| fun _ ->
        let extensions = [ ".oldest"; ".older"; ".old"; "" ]
        extensions
        |> Seq.pairwise
        |> Seq.iter (fun (dst, src) ->
            let dstDir = buildRoot + dst
            let srcDir = buildRoot + src
            if System.IO.Directory.Exists(dstDir) then
                Trace.logfn "# delete %s" dstDir
                dstDir |> Directory.delete
            if System.IO.Directory.Exists(srcDir) then
                Trace.logfn "# rename %s -> %s" srcDir dstDir
                System.IO.Directory.Move(srcDir, dstDir))

let ensureBuildRootTask =
    BuildTask.createFn "EnsureBuildRoot" [ cleanTask.IfNeeded ]
    <| fun _ ->
        Directory.ensure buildRoot
        let tagFile = buildRoot </> ".BUILDDIR.TAG"
        use f = System.IO.File.CreateText(tagFile)
        f.WriteLine("build-dir: 19BBC6F7-3FBF-49BD-BAA4-EA60CE5A0735")
        f.WriteLine("# Tag for `restic`")

let sourceTask =
    BuildTask.createFn "Source" [ cleanTask; ensureBuildRootTask ]
    <| fun _ ->
        srcDir |> Directory.ensure
        CmdLine.empty
        |> CmdLine.append "source"
        |> CmdLine.append __SOURCE_DIRECTORY__
        |> CmdLine.appendPrefix "--source-folder" srcDir
        |> CmdLine.toArray
        |> CreateProcess.fromRawCommand "conan"
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore

type ConanTasks =
    { Install: BuildTask.TaskInfo
      Build: BuildTask.TaskInfo
      Package: BuildTask.TaskInfo
      Export: BuildTask.TaskInfo
      Test: BuildTask.TaskInfo
      Create: BuildTask.TaskInfo }

let createTargets prof: ConanTasks =
    let targetName x = sprintf "%s.%s" x prof
    let buildDir = buildRoot </> (sprintf "build-%s" prof)

    let packageDir =
        buildRoot </> (sprintf "package-%s" prof)

    let installTask =
        BuildTask.createFn ("Install" |> targetName) [ sourceTask ]
        <| fun _ ->
            buildDir |> Directory.ensure
            CmdLine.empty
            |> CmdLine.append "install"
            |> CmdLine.append __SOURCE_DIRECTORY__
            |> CmdLine.appendPrefix "--install-folder" buildDir
            |> CmdLine.appendPrefix "--profile" prof
            |> CmdLine.toArray
            |> CreateProcess.fromRawCommand "conan"
            |> CreateProcess.ensureExitCode
            |> Proc.run
            |> ignore

    let buildTask =
        BuildTask.createFn ("Build" |> targetName) [ installTask ]
        <| fun _ ->
            CmdLine.empty
            |> CmdLine.append "build"
            |> CmdLine.append __SOURCE_DIRECTORY__
            |> CmdLine.appendPrefix "--source-folder" srcDir
            |> CmdLine.appendPrefix "--build-folder" buildDir
            |> CmdLine.toArray
            |> CreateProcess.fromRawCommand "conan"
            |> CreateProcess.ensureExitCode
            |> Proc.run
            |> ignore

    let packageTask =
        BuildTask.createFn ("Package" |> targetName) [ buildTask ]
        <| fun _ ->
            CmdLine.empty
            |> CmdLine.append "package"
            |> CmdLine.append __SOURCE_DIRECTORY__
            |> CmdLine.appendPrefix "--source-folder" srcDir
            |> CmdLine.appendPrefix "--build-folder" buildDir
            |> CmdLine.appendPrefix "--package-folder" packageDir
            |> CmdLine.toArray
            |> CreateProcess.fromRawCommand "conan"
            |> CreateProcess.ensureExitCode
            |> Proc.run
            |> ignore

    let exportTask =
        BuildTask.createFn (targetName "Export") [ packageTask ]
        <| fun _ ->
            CmdLine.empty
            |> CmdLine.append "export-pkg"
            |> CmdLine.append __SOURCE_DIRECTORY__
            |> CmdLine.append "objectx/testing"
            |> CmdLine.appendPrefix "--source-folder" srcDir
            |> CmdLine.appendPrefix "--build-folder" buildDir
            |> CmdLine.append "--force"
            |> CmdLine.toArray
            |> CreateProcess.fromRawCommand "conan"
            |> CreateProcess.ensureExitCode
            |> Proc.run
            |> ignore

    let testTask =
        BuildTask.createFn ("Test" |> targetName) [ exportTask ]
        <| fun _ ->
            CmdLine.empty
            |> CmdLine.append "test"
            |> CmdLine.append (__SOURCE_DIRECTORY__ </> "test_package")
            |> CmdLine.append "rapidcheck/1.0.8@objectx/testing"
            |> CmdLine.appendPrefix "--profile" prof
            |> CmdLine.toArray
            |> CreateProcess.fromRawCommand "conan"
            |> CreateProcess.ensureExitCode
            |> Proc.run
            |> ignore

    let createTask =
        BuildTask.createFn ("Create" |> targetName) [ testTask ]
        <| fun _ ->
            CmdLine.empty
            |> CmdLine.append "create"
            |> CmdLine.append __SOURCE_DIRECTORY__
            |> CmdLine.append "objectx/testing"
            |> CmdLine.appendPrefix "--profile" prof
            |> CmdLine.toArray
            |> CreateProcess.fromRawCommand "conan"
            |> CreateProcess.ensureExitCode
            |> Proc.run
            |> ignore

    { Install = installTask
      Build = buildTask
      Package = packageTask
      Export = exportTask
      Test = testTask
      Create = createTask }

let targetDefault = createTargets "default"
let targetClang10 = createTargets "clang-10"
let targetGcc9 = createTargets "gcc-10"

let exportTask =
    BuildTask.createEmpty
        "Export"
        [ targetDefault.Export
          targetClang10.Export
          targetGcc9.Export ]

let testTask =
    BuildTask.createEmpty
        "Test"
        [ targetDefault.Test
          targetClang10.Test
          targetGcc9.Test ]

let createTask =
    BuildTask.createEmpty
        "Create"
        [ targetDefault.Create
          targetClang10.Create
          targetGcc9.Create ]

BuildTask.runOrList ()
