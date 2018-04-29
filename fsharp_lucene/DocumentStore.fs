namespace DocumentStore

open System
module Agents =
    open Core
    open System.Threading

    type Message =
        Start of config: Core.Config
        | Stop
        | RebuildIndex
        | Watch
        | Search of term: string

    let private latch = new AutoResetEvent(false)

    let private stopIndexer (ctx: Indexer.Context) =
        Indexer.close(ctx)

    let private agent = MailboxProcessor<Message>.Start(fun inbox ->
        let initialContext: Indexer.Context = {
            Indexer.emptyContext with
                Config = { IndexDirectory = "Index";
                           DocumentsDirectory = "/Data"; } }

        let rec loop(ctx) = async {
            let! msg = inbox.Receive()

            let newContext =
                match msg with
                    | Stop ->
                        let finalContext = stopIndexer(ctx)
                        latch.Set() |> ignore
                        finalContext

                    | RebuildIndex ->
                        try
                            FileWalker.traverse
                                ctx.Config.DocumentsDirectory
                                (Indexer.storeDocument ctx)
                            Indexer.commit ctx
                        with
                            | ex -> printfn "DocumentStore.RebuildIndex: %A" ex.Message
                        printfn "DocumentStore.RebuildIndex: FINISHED"
                        ctx
                    | Watch ->
                        Indexer.watch ctx

                    | Search term ->
                        printfn "DocumentStore.Search: %A" term
                        Indexer.search ctx term |> ignore
                        inbox.Post Stop
                        ctx

                    | Start config ->
                        Indexer.initialize(config)

            return! loop(newContext)
        }

        loop(initialContext)
    )

    let start config = agent.Post (Start config)

    let stop    = fun () -> agent.Post Stop

    let rebuild = fun () -> agent.Post RebuildIndex

    let search term = agent.Post (Search term)

    let watch   = fun () -> agent.Post Watch

    let wait    = fun () -> latch.WaitOne()