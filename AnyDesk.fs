module ReConMan.AnyDesk

open System.Diagnostics

let run executable deviceId (passsword: string) =
    let psi =
        ProcessStartInfo(
            FileName = executable,
            Arguments = $"{deviceId} --with-password",
            UseShellExecute = false,
            RedirectStandardInput = true,
            CreateNoWindow = true
        )

    use ps = new Process(StartInfo = psi)
    ps.Start() |> ignore
    ps.StandardInput.WriteLine passsword
    ()
