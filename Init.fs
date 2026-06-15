module ReConMan.Init

open System.Xml.Linq
open System.IO

let mutable config = "config.xml"
let rdesktop_exe = @"D:\Programs\rudesktop-2.9.927-x32.exe"
let anydesk_exe = @"C:\Program Files (x86)\AnyDesk\AnyDesk.exe"

let readConfig (path: string) =
    if File.Exists path then
        let x = XDocument.Load path in

        let m =
            x.Root.Elements()
            |> Seq.map (fun el -> el.Name.LocalName, el.Value)
            |> Map.ofSeq in

        m
    else
        Map.empty

let rdesktopExecutable () =
    match readConfig config |> Map.tryFind "rdesktop_exe" with
    | Some v -> v
    | None -> rdesktop_exe

let anydeskExecutable () =
    match readConfig config |> Map.tryFind "anydesk_exe" with
    | Some v -> v
    | None -> anydesk_exe
