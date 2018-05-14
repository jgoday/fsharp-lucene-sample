namespace DocumentStore

module ExtractorFactory =
    open Docx
    open Excel
    open PDF
    open HeyRed
    open HeyRed.Mime

    type Extractor = string -> string
    
    let createExtractor (filepath: string): Extractor =
        printfn "createExtractor: %A" (MimeGuesser.GuessMimeType(filepath))
        match filepath with
            | PDF(_) -> getPdfContent
            | DOCX(_) -> getDocxContent
            | XLS(_) | XLSX(_) -> getXlsContent
            | _ -> (fun _ -> "")

