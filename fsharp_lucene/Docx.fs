namespace DocumentStore

module Docx =
    open System.IO
    open System.Text
    open NPOI.XWPF.UserModel

    let isDocx (filepath: string) =
        FileUtils.fileExtension(filepath) = ".docx"
        
    let (|DOCX|_|) (filepath: string) = if isDocx(filepath) then Some filepath else None

    let getDocxContent (filepath: string) =
        try
            printfn "Docx.getDocxContent(%s)" filepath
            let buffer = new StringBuilder()
            use stream = new StreamReader(filepath)
            let doc = new XWPFDocument(stream.BaseStream)
            doc.Paragraphs
                |> Seq.iter (fun p ->
                    buffer.Append(p.Text + " ") |> ignore)
            buffer.ToString()
        with
            | ex -> printfn "Docx.getDocxContent(%s): %A" filepath ex.Message; ""