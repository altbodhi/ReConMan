module ReConMan.Db

open System.Data
open System.Text
open System.Security.Cryptography
open System.IO

let mutable password = ""

let makeTable () =
    let dt = new DataTable("connections")
    dt.Columns.Add "id" |> ignore
    dt.Columns.Add "kind" |> ignore
    dt.Columns.Add "con_id" |> ignore
    dt.Columns.Add "pass" |> ignore
    dt

let createEas (password: string) =
    let eas = Aes.Create()
    let passwordBytes = Encoding.UTF8.GetBytes password
    eas.Key <- SHA256.Create().ComputeHash(passwordBytes)
    eas.IV <- MD5.Create().ComputeHash(passwordBytes)
    eas

let enc password =
    let eas = createEas password
    eas.CreateEncryptor()

let decr password =
    let eas = createEas password
    eas.CreateDecryptor()

let writeTable (dt: DataTable) path password =

    use fs = File.OpenWrite(path)

    use cs = new CryptoStream(fs, enc password, CryptoStreamMode.Write)

    dt.WriteXml(cs)
    cs.FlushFinalBlock()


let readTable path password =
    if File.Exists path then
        use rs = File.OpenRead(path)

        use ds = new CryptoStream(rs, decr password, CryptoStreamMode.Read)
        let dt = makeTable ()

        dt.ReadXml ds |> ignore
        dt
    else
        makeTable ()
