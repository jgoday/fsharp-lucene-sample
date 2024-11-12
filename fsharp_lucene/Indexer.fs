namespace DocumentStore

module Indexer =
    open Lucene.Net.Analysis
    open Lucene.Net.Analysis.Standard
    open Lucene.Net.Documents
    open Lucene.Net.Index
    open Lucene.Net.Store
    open Lucene.Net.Util
    open Lucene.Net.Search
    open Lucene.Net.QueryParsers.Classic
    open Lucene.Net.Search.Highlight

    open FileUtils

    type ExtractAndStoreMessage<'a> = {
        Context: 'a;
        FilePath: string;
    }

    type Context = {
        Indexer: IndexWriter option;
        Directory: FSDirectory option;
        Searcher: IndexSearcher option;
        Config: Core.Config;
        ExtractorAgent: MailboxProcessor<ExtractAndStoreMessage<Context>> option;
    }

    let private matchDocumentQuery (filepath: string) =
        let q = new BooleanQuery()
        q.Add(new TermQuery(new Term("Path", filepath)), Occur.SHOULD)
        q

    let luceneVersion = LuceneVersion.LUCENE_48
    let searchMaxDocuments = 10

    let close (ctx: Context) =
        ctx.Directory
            |> Option.iter (fun d -> d.Dispose())
        ctx.Indexer
            |> Option.iter (fun i -> i.Dispose())
        FileWalker.close()
        { ctx with  Directory = None; Indexer = None }

    let commit (ctx: Context) =
        ctx.Indexer |> Option.iter (fun w -> w.Commit())

    let documentExists (ctx: Context) (filepath: string) =
        ctx.Searcher
            |> Option.map
                (fun searcher ->
                    let q = matchDocumentQuery(filepath)
                    let res = searcher.Search(q, 1)
                    res.TotalHits >= 1)
            |> Option.defaultValue false

    let getDocumentByPath (ctx: Context) (filepath: string) =
        ctx.Searcher
            |> Option.map
                (fun searcher ->
                    let q = matchDocumentQuery(filepath)
                    let res = searcher.Search(q, 1)
                    if res.TotalHits = 1 then Some(res.ScoreDocs.[0]) else None)
            |> Option.flatten

    let private extractAndStoreDocument msg =
        match msg with
            | { ExtractAndStoreMessage.Context = ctx; ExtractAndStoreMessage.FilePath = filepath } ->
                let filename = fileName(filepath)
                if not (documentExists ctx filepath) then
                    printfn "Indexer.storeDocument: %A" filename

                    let extractor = ExtractorFactory.createExtractor filepath
                    let content = extractor filepath

                    let doc = new Document()
                    doc.Add(new StringField("Path", filepath, Field.Store.YES))
                    doc.Add(new StringField("Name", filename, Field.Store.YES))
                    doc.Add(new TextField("Content", content, Field.Store.YES))
                    doc.Add(new StringField("Hash", md5 filepath, Field.Store.YES))
                    doc.Add(new Int64Field("FileSize", fileSize filepath, Field.Store.YES))

                    ctx.Indexer |> Option.iter (fun w ->
                        w.AddDocument doc
                        w.Commit())

    let storeDocument (ctx: Context) (filepath: string) =
        ctx.ExtractorAgent
            |> Option.iter (fun ex -> ex.Post { Context = ctx; FilePath = filepath; })

    let removeDocument (ctx: Context) (filepath: string) =
        if documentExists ctx filepath then
            match (getDocumentByPath ctx filepath, ctx.Indexer) with
                | (Some _, Some writer) ->
                    printfn "Indexer.removeDocument: %A" filepath
                    let q = matchDocumentQuery filepath
                    writer.DeleteDocuments q
                    writer.Commit()
                | _ -> ()

    let search (ctx: Context) (term: string) =
        let analyzer = new StandardAnalyzer(luceneVersion)
        let queryParser = new QueryParser(luceneVersion, "Content", analyzer)
        queryParser.DefaultOperator <- QueryParser.AND_OPERATOR;

        let query = queryParser.Parse(term)
        let scorer = new QueryScorer(query, "Content")

        let highlighter = new Highlighter(scorer)

        ctx.Searcher |>
            Option.iter (fun searcher ->
                let res = searcher.Search(query, searchMaxDocuments)
                printfn "Results: %i" res.TotalHits
                Array.iter (fun (scoreDoc: ScoreDoc) ->
                                let doc = ctx.Searcher.Value.Doc(scoreDoc.Doc)
                                let name = doc.GetField("Name").GetStringValue()
                                let path = doc.GetField("Path").GetStringValue()
                                let content = doc.GetField("Content").GetStringValue()
                                let fragment = highlighter.GetBestFragment(analyzer,
                                                                           "Content",
                                                                           content)
                                let md5Sum = doc.GetField("Hash").GetStringValue()
                                let fileSize = doc.GetField("FileSize").GetInt64Value()

                                printfn "-------------------------------"
                                printfn "Name: %s" name
                                printfn "Path: %s" path
                                printfn "MD5: %s" md5Sum
                                printfn "%s" fragment
                                printfn "Size: %i" (if fileSize.HasValue then fileSize.Value else 0)
                                printfn "-------------------------------")
                            res.ScoreDocs)

    let initialize (config: Core.Config) =
        let dir = FSDirectory.Open(config.IndexDirectory)
        let analyzer = new StandardAnalyzer(luceneVersion)
        let indexConfig = new IndexWriterConfig(luceneVersion, analyzer)
        let indexer = new IndexWriter(dir, indexConfig)
        indexer.Commit() // makes sures that the index structure exists before we create the searcher

        let searcher = new IndexSearcher(DirectoryReader.Open(dir))

        { Indexer = Some indexer;
          Directory = Some dir;
          Searcher = Some searcher;
          Config = config;
          ExtractorAgent = Some (RoutedAgent.create extractAndStoreDocument
                                                    config.ConcurrentExtractors) }

    let watch (ctx: Context) =
        let config = ctx.Config
        printfn "Indexer.watch: %A" config

        FileWalker.watchForNewFiles
            config.DocumentsDirectory
            (fun p -> storeDocument ctx p)
            (fun p -> removeDocument ctx p)

        ctx

    let emptyContext = {
        Indexer = None;
        Directory = None;
        Searcher = None;
        Config = Core.defaultConfig;
        ExtractorAgent = None;
    }
