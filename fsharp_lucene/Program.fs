// Learn more about F# at http://fsharp.org

open Argu
open DocumentStore

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Core.ProgramArguments>()
    let results = parser.ParseCommandLine argv

    let dir = results.GetResult(Core.ProgramArguments.Directory)
    let indexDir = results.GetResult(Core.ProgramArguments.Index_Directory)

    let config = {
        Core.Config.DocumentsDirectory = dir;
        Core.Config.IndexDirectory = indexDir;
        Core.Config.ConcurrentExtractors = 5;
    }

    Agents.start <| config

    if results.Contains(Core.ProgramArguments.Watch) then
        Agents.watch()
    else if results.Contains(Core.ProgramArguments.Search) then
        Agents.search <| results.GetResult(Core.ProgramArguments.Search)
    else if results.Contains(Core.ProgramArguments.Rebuild) then
        Agents.rebuild()
    else
        ()

    Agents.wait() |> ignore

    0
