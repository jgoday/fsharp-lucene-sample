// Learn more about F# at http://fsharp.org

open System
open DocumentStore

[<EntryPoint>]
let main argv =
    Agents.start()
    Agents.rebuild()

    Agents.wait()
        |> ignore
    0 // return an integer exit code
