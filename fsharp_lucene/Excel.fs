namespace DocumentStore

module Excel =
    open System.IO
    open System.Text
    open HeyRed.Mime
    open NPOI.XWPF.UserModel
    open NPOI.SS.UserModel

    let isXls (filepath: string) =
        MimeGuesser.GuessMimeType(filepath) = "application/vnd.ms-excel"
        
    let isXlsx (filepath: string) =
        FileUtils.fileExtension(filepath) = ".xlsx"

    let (|XLS|_|) (filepath: string) = if isXls(filepath) then Some filepath else None

    let (|XLSX|_|) (filepath: string) = if isXlsx(filepath) then Some filepath else None

    let private getCellContent (cell: ICell) =
        cell.StringCellValue

    let private getCellsContent (row: IRow) =
        row.Cells
            |> Seq.map getCellContent

    let getXlsContent (filepath: string) =
        try
            printfn "Excel.getXlsContent(%s)" filepath
            let buffer = new StringBuilder()
            use stream = new StreamReader(filepath)
            let doc = WorkbookFactory.Create(stream.BaseStream)
            let sheet = doc.GetSheetAt(0)
            for i in 0 .. sheet.LastRowNum + 1 do
                sheet.GetRow(i)
                    |> Option.ofObj
                    |> Option.map getCellsContent
                    |> Option.iter (Seq.iter (fun cell ->
                            buffer.Append(cell + " ") |> ignore))
            buffer.ToString()
        with
            | ex -> printfn "Docx.getDocxContent(%s): %A" filepath ex.Message; ""