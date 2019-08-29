#r "paket: groupref Fake //"

#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open BlackFox.CommandLine

Target.initEnvironment()

let profile = "clang-8"

let buildRoot = __SOURCE_DIRECTORY__ </> "B"

Target.create "EnsureBuildRoot" <| fun _ ->
    Directory.ensure buildRoot
    let tagFile = buildRoot </> ".BUILDDIR.TAG"
    use f = System.IO.File.CreateText(tagFile)
    f.WriteLine("build-dir: 19BBC6F7-3FBF-49BD-BAA4-EA60CE5A0735")
    f.WriteLine("# Tag for `restic`")

let srcDir = buildRoot </> "source"

Target.create "Clean" <| fun _ ->
    let extensions = [".oldest"; ".older"; ".old"; ""]
    extensions
    |> Seq.pairwise
    |> Seq.iter
        (fun (dst, src) ->
            let dstDir = buildRoot + dst
            let srcDir = buildRoot + src
            if System.IO.Directory.Exists (dstDir) then
                Trace.logfn "# delete %s" dstDir
                dstDir |> Directory.delete
            if System.IO.Directory.Exists (srcDir) then
                Trace.logfn "# rename %s -> %s" srcDir dstDir
                System.IO.Directory.Move(srcDir, dstDir))

Target.create "Source" <| fun _ ->
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

"Clean"
    ==> "EnsureBuildRoot"
    ==> "Source"

let createTargets prof =
    let targetName x = sprintf "%s-%s" x prof
    let buildDir = buildRoot </> (sprintf "build-%s" prof)
    let packageDir = buildRoot </> (sprintf "package-%s" prof)
    Target.create (targetName "Install") <| fun _ ->
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

    Target.create (targetName "Build") <| fun _ ->
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

    Target.create (targetName "Package") <| fun _ ->
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

    Target.create (targetName "Export") <| fun _ ->
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

    Target.create (targetName "Test") <| fun _ ->
        CmdLine.empty
        |> CmdLine.append "test"
        |> CmdLine.append (__SOURCE_DIRECTORY__ </> "test_package")
        |> CmdLine.append "rapidcheck/1.0.5@objectx/testing"
        |> CmdLine.appendPrefix "--profile" prof
        |> CmdLine.toArray
        |> CreateProcess.fromRawCommand "conan"
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore

    Target.create (targetName "Create") <| fun _ ->
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

    "Source"
        ==> (targetName "Install")
        ==> (targetName "Build")
        ==> (targetName "Package")
        ==> (targetName "Export")
        ==> (targetName "Test") |> ignore
    (targetName "Export") ==> "Export" |> ignore
    (targetName "Test") ==> "Test" |> ignore
    (targetName "Create") ==> "Create" |> ignore

Target.create "Export" ignore
Target.create "Test" ignore
Target.create "Create" ignore

createTargets "default"
createTargets "clang-8"
createTargets "gcc-9"

Target.runOrList()
