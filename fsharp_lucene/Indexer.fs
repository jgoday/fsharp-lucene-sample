namespace DocumentStore

module Indexer =
    open Lucene.Net.Analysis
    open Lucene.Net.Analysis.Standard
    open Lucene.Net.Documents
    open Lucene.Net.Index
    open Lucene.Net.Store
    open Lucene.Net.Util
    open Lucene.Net.Search

    open FileUtils

    type Context = {
        Indexer: Option<IndexWriter>;
        Directory: Option<FSDirectory>;
        Searcher: Option<IndexSearcher>;
        Config: Core.Config;
    }

    let emptyContext = {
        Indexer = None; Directory = None; Searcher = None;
        Config = Core.defaultConfig
    }

    let private matchDocumentQuery (filepath: string) =
        let q = new BooleanQuery()
        q.Add(new TermQuery(new Term("Path", filepath)), Occur.SHOULD)
        q

    let luceneVersion = LuceneVersion.LUCENE_48

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

    let storeDocument (ctx: Context) (filepath: string) =
        let filename = fileName(filepath)

        if not (documentExists ctx filepath) then
            printfn "Indexer.storeDocument: %A" filename

            let content = if PDF.isPdf filepath then PDF.getPdfContent(filepath) else ""

            let doc = new Document()
            doc.Add(new StringField("Path", filepath, Field.Store.YES))
            doc.Add(new StringField("Name", filename, Field.Store.YES))
            doc.Add(new TextField("Content", content, Field.Store.YES))
            // doc.Add(new Field("Content", content, Field.Store.YES, Field.Index.ANALYZED))

            ctx.Indexer |> Option.iter (fun w -> w.AddDocument doc)

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
        let q = new TermQuery(new Term("Content", term))
        let res = ctx.Searcher.Value.Search(q, 10)
        printfn "Results: %i" res.TotalHits
        Array.iter (fun (scoreDoc: ScoreDoc) ->
                        let doc = ctx.Searcher.Value.Doc(scoreDoc.Doc)
                        let nameField = doc.GetField("Name")
                        let pathField = doc.GetField("Path")
                        printfn "%s: %s" (nameField.GetStringValue()) (pathField.GetStringValue()))
                    res.ScoreDocs

    let initialize (config: Core.Config) =
        let dir = FSDirectory.Open(config.IndexDirectory)
        let analyzer = new StandardAnalyzer(luceneVersion)
        let indexConfig = new IndexWriterConfig(luceneVersion, analyzer)
        // analyzer.MaxTokenLength <- UNLIMITED
        let indexer = new IndexWriter(dir, indexConfig)
        indexer.Commit() // makes sures that the index structure exists before we create the searcher

        let searcher = new IndexSearcher(DirectoryReader.Open(dir))

        { Indexer = Some indexer; Directory = Some dir; Searcher = Some searcher; Config = config; }

    let watch (ctx: Context) =
        let config = ctx.Config
        printfn "Indexer.watch: %A" config

        FileWalker.watchForNewFiles
            config.DocumentsDirectory
            (fun p -> storeDocument ctx p)
            (fun p -> removeDocument ctx p)

        ctx