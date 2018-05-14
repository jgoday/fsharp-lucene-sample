namespace DocumentStore

module PDF =
    open System.Text
    open HeyRed.Mime
    open iText.Kernel.Pdf
    open iText.Kernel.Pdf.Canvas.Parser

    let isPdf (filepath: string) =
        MimeGuesser.GuessMimeType(filepath) = "application/pdf"

    let (|PDF|_|) (filepath: string) = if isPdf(filepath) then Some filepath else None

    let getPdfContent (filepath: string) =
        try
            printfn "PDF.getPdfContent(%s)" filepath
            let buffer = new StringBuilder()
            let reader = new PdfReader(filepath)
            let doc = new PdfDocument(reader)
            for i in 1 .. doc.GetNumberOfPages() do
                let page = doc.GetPage(i)
                buffer.Append(PdfTextExtractor.GetTextFromPage(page) + " ") |> ignore

            buffer.ToString()
        with
            | ex -> printfn "PDF.getPdfContent(%s): %A" filepath ex.Message; ""