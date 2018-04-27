namespace DocumentStore

open System
module Agents =
    open Core
    open System.Threading

    type Message = Start | Stop | RebuildIndex

    let private latch = new AutoResetEvent(false)

    let private stopIndexer (ctx: Indexer.Context) =
        Indexer.close(ctx)

    let private agent = MailboxProcessor<Message>.Start(fun inbox ->
        let initialContext: Indexer.Context = {
            Indexer.emptyContext with
                Config = { IndexDirectory = "Index";
                           DocumentsDirectory = "/Data/Books"; } }

        let rec loop(ctx) = async {
            let! msg = inbox.Receive()

            let newContext =
                match msg with
                    | Stop -> stopIndexer(ctx)
                    | RebuildIndex ->
                        try
                            FileWalker.traverse
                                ctx.Config.DocumentsDirectory
                                (Indexer.storeDocument ctx)
                            Indexer.commit ctx
                        with
                            | ex -> printfn "DocumentStore.RebuildIndex: %A" ex.Message

                        ctx
                    | Start -> Indexer.initialize(ctx.Config)
                    // | _ -> ctx

            return! loop(newContext)
        }

        loop(initialContext)
    )

    let start   = fun () -> agent.Post Start
    let stop    = fun () -> agent.Post Stop
    let rebuild = fun () -> agent.Post RebuildIndex

    let wait    = fun () -> latch.WaitOne()