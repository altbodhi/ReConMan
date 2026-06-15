namespace ReConMan.Types

type TypeOfCon =
    | AnyDesk = 1
    | RDesktop = 2

type ConnectType =
    | RDesktop of connectionId: string * password: string
    | AnyDesk of connectionId: string * password: string
    | None

[<CLIMutable>]
type RemotePoint =
    { _id: string
      connections: ConnectType list }
