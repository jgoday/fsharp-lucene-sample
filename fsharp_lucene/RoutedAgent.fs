namespace DocumentStore

module RoutedAgent =

    let private createAgent consumeFn = MailboxProcessor<'a>.Start(fun inbox ->
        let rec loop() = async {
            let! msg = inbox.Receive()

            consumeFn(msg)

            return! loop()
        }

        loop()
    )

    let create consumeFn limit = MailboxProcessor<'a>.Start(fun inbox ->
        // agents
        let innerAgents = Array.map (fun _ -> createAgent consumeFn) (Array.zeroCreate limit)

        let mutable currentAgent = 0

        let rec loop() = async {
            let! msg = inbox.Receive()

            let targetAgent = innerAgents.[currentAgent]
            targetAgent.Post(msg)
            currentAgent <- (currentAgent + 1) % limit

            return! loop()
        }

        loop()
    )