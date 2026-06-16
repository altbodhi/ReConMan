module ReConMan.Db

open System.Data
open System.Text
open System.Security.Cryptography
open System.IO
open ReConMan.Types
open System.Text.Json
open System.Text.Json.Serialization

let options = JsonFSharpOptions.Default().ToJsonSerializerOptions()

let asStr (x: ConnectType) =
    match x with
    | RDesktop(a, b) -> (TypeOfCon.RDesktop.ToString(), a, b)
    | AnyDesk(a, b) -> (TypeOfCon.AnyDesk.ToString(), a, b)
    | None -> (TypeOfCon.None.ToString(), "", "")

let mutable password = ""

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

let writeTable (dt: list<RemotePoint>) path password =

    use fs = File.Create(path)

    use cs = new CryptoStream(fs, enc password, CryptoStreamMode.Write)

    JsonSerializer.Serialize(cs, dt, options)
    cs.FlushFinalBlock()


let readTable path password =
    if File.Exists path then
        use rs = File.OpenRead(path)

        use ds = new CryptoStream(rs, decr password, CryptoStreamMode.Read)

        JsonSerializer.Deserialize<list<RemotePoint>>(ds, options)
    else
        []
