module ReConMan.RDesktop

open System.Diagnostics

let run executable deviceId passsword =
    Process.Start(executable, [ "--connect"; deviceId; passsword ])
