module ReConMan.WinFormsApp


open System.Data
open System.Windows.Forms
open ReConMan
open ReConMan.Types

type DialogButtons(dialog: Form) as x =
    class
        inherit FlowLayoutPanel()
        let accept = new Button(DialogResult = DialogResult.OK, Text = "OK")
        let cancel = new Button(DialogResult = DialogResult.Cancel, Text = "Cancel")
        let empty = new Label(Width = 45)

        do
            x.AutoSize <- true
            x.AutoSizeMode <- AutoSizeMode.GrowAndShrink
            x.WrapContents <- false
            x.Controls.Add empty

            x.Controls.Add accept
            x.Controls.Add cancel

            dialog.AcceptButton <- accept
            dialog.CancelButton <- cancel
    end

type Authorize() as x =
    class
        inherit Form()
        let pass = new TextBox(UseSystemPasswordChar = true, Width = 200)
        let dialog = new DialogButtons(x)

        do
            x.StartPosition <- FormStartPosition.CenterParent
            x.Text <- "Authorize"
            x.FormBorderStyle <- FormBorderStyle.FixedDialog
            let flow = new FlowLayoutPanel()
            flow.AutoSizeMode <- AutoSizeMode.GrowAndShrink
            flow.AutoSize <- true
            flow.MaximumSize <- System.Drawing.Size(400, 200)
            flow.Dock <- DockStyle.Fill
            flow.Controls.Add(new Label(Text = "Password:"))
            flow.Controls.Add pass
            flow.Controls.Add dialog
            x.Controls.Add flow
            x.ClientSize <- flow.PreferredSize

        member x.Accept() = x.ShowDialog() = DialogResult.OK
        member x.Pass = pass.Text
    end

type EditConnection(name, ct, conId, pass) as x =
    class
        inherit Form()
        let name = new TextBox(Text = name, Width = 200)
        let conType = new ComboBox(Width = 200)
        let conId = new TextBox(Text = conId, Width = 200)
        let pass = new TextBox(Text = pass, Width = 200)
        let dialog = new DialogButtons(x)

        do
            conType.Items.Add(TypeOfCon.AnyDesk.ToString()) |> ignore
            conType.Items.Add(TypeOfCon.RDesktop.ToString()) |> ignore
            conType.SelectedItem <- ct
            x.StartPosition <- FormStartPosition.CenterParent
            x.Text <- "New Connection"
            x.FormBorderStyle <- FormBorderStyle.FixedDialog
            let flow = new FlowLayoutPanel()
            flow.AutoSizeMode <- AutoSizeMode.GrowAndShrink
            flow.AutoSize <- true
            flow.MaximumSize <- System.Drawing.Size(400, 200)
            flow.Dock <- DockStyle.Fill
            flow.Controls.Add(new Label(Text = "Client:"))
            flow.Controls.Add name
            flow.Controls.Add(new Label(Text = "Type:"))
            flow.Controls.Add conType
            flow.Controls.Add(new Label(Text = "ConId:"))
            flow.Controls.Add conId
            flow.Controls.Add(new Label(Text = "Pass:"))
            flow.Controls.Add pass
            flow.Controls.Add dialog
            x.Controls.Add flow
            x.ClientSize <- flow.PreferredSize

        member x.Accept() = x.ShowDialog() = DialogResult.OK
        member x.Name = name.Text
        member x.ConId = conId.Text
        member x.Pass = pass.Text

        member x.AsConnectionType() =
            match x.ConType with
            | TypeOfCon.AnyDesk -> ConnectType.AnyDesk(x.ConId, x.Pass)
            | TypeOfCon.RDesktop -> ConnectType.RDesktop(x.ConId, x.Pass)
            | TypeOfCon.None
            | _ -> ConnectType.None

        member x.ConType =
            match TypeOfCon.TryParse<TypeOfCon>(conType.SelectedItem.ToString()) with
            | true, x -> x
            | false, _ -> TypeOfCon.None
    end

let authorize () =
    use authForm = new Authorize()

    if authForm.Accept() then
        Db.password <- authForm.Pass
        true
    else
        false

type App(xs: list<RemotePoint>) as x =
    class
        inherit ApplicationContext()
        let mutable items = xs
        let dt = new DataTable("dt")

        let dg =
            new DataGridView(AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true)

        let connect () =
            if dg.CurrentRow = null then
                ()
            else
                let id = dg.CurrentRow.Cells["Name"].Value.ToString()
                let kind = dg.CurrentRow.Cells["Kind"].Value.ToString()
                let conId = dg.CurrentRow.Cells["ConId"].Value.ToString()
                let point = items |> List.find (fun x -> x._id = id)

                let (k, c, p) =
                    point.connections
                    |> List.map Db.asStr
                    |> List.find (fun (k, c, p) -> k = kind && c = conId)

                if k = TypeOfCon.AnyDesk.ToString() then
                    let ex = Init.anydeskExecutable ()
                    AnyDesk.run ex conId p |> ignore
                else if k = TypeOfCon.RDesktop.ToString() then
                    let ex = Init.rdesktopExecutable ()
                    RDesktop.run ex conId p |> ignore
                else
                    ()

        let mainMenu = new MenuStrip()

        let editMenu =
            mainMenu.Items.Add(new ToolStripMenuItem("&Items"))
            |> fun i -> mainMenu.Items.Item(i) :?> ToolStripMenuItem

        let add = editMenu.DropDownItems.Add("&Add")
        let edit = editMenu.DropDownItems.Add("&Edit")
        let delete = editMenu.DropDownItems.Add("&Delete")

        let conMenu = mainMenu.Items.Add("&Connect")
        let changePass = mainMenu.Items.Add("&Password")

        do
            dt.Columns.Add("Name") |> ignore
            dt.Columns.Add("Kind") |> ignore
            dt.Columns.Add("ConId") |> ignore

            for i in items do
                for c in i.connections do
                    let row = dt.NewRow()
                    let (kind, cid, _) = Db.asStr c
                    row["Name"] <- i._id
                    row["Kind"] <- kind
                    row["ConId"] <- cid
                    dt.Rows.Add row

            dg.DataBindingComplete.Add(fun _ -> dg.ClearSelection())
            dg.SelectionMode <- DataGridViewSelectionMode.FullRowSelect
            dg.DataSource <- dt
            dg.AutoResizeColumns()

            add.Click.Add(fun _ ->
                use frm = new EditConnection("", TypeOfCon.AnyDesk.ToString(), "", "")

                if frm.Accept() then
                    let con = frm.AsConnectionType()
                    let rp: RemotePoint option = items |> List.tryFind (fun x -> x._id = frm.Name)

                    items <-
                        match rp with
                        | Some v ->
                            { v with
                                connections = con :: v.connections }
                            :: (items |> List.filter (fun x -> x._id <> v._id))
                        | _ ->
                            { RemotePoint._id = frm.Name
                              connections = [ con ] }
                            :: items

                    dt.Rows.Add([| box frm.Name; box frm.ConType; box frm.ConId |]) |> ignore)

            edit.Click.Add(fun _ ->
                if dg.CurrentRow = null then
                    ()
                else
                    let _id = dg.CurrentRow.Cells["Name"].Value.ToString()
                    let conid = dg.CurrentRow.Cells["ConId"].Value.ToString()
                    let rp = items |> List.find (fun x -> x._id = _id)

                    let (kind, cid, pwd) =
                        rp.connections
                        |> List.find (fun x -> let (kind, cid, pwd) = Db.asStr x in cid = conid)
                        |> Db.asStr

                    use frm = new EditConnection(rp._id, kind, cid, pwd)

                    if frm.Accept() then
                        let nc = frm.AsConnectionType()

                        items <-
                            items
                            |> List.map (function
                                | x when x._id = _id ->
                                    { x with
                                        _id = frm.Name
                                        connections =
                                            x.connections
                                            |> List.map (function
                                                | AnyDesk(i, p) when i = conid -> nc
                                                | RDesktop(i, p) when i = conid -> nc
                                                | a -> a) }
                                | x -> x)

                        dg.CurrentRow.Cells["Name"].Value <- frm.Name
                        dg.CurrentRow.Cells["Kind"].Value <- frm.ConType.ToString()
                        dg.CurrentRow.Cells["ConId"].Value <- frm.ConId)

            delete.Click.Add(fun _ ->
                if dg.CurrentRow = null then
                    ()
                else if MessageBox.Show("delete item?", "Warning", MessageBoxButtons.YesNo) = DialogResult.Yes then
                    let _id = dg.CurrentRow.Cells["Name"].Value.ToString()
                    let conId = dg.CurrentRow.Cells["ConId"].Value.ToString()

                    items <-
                        items
                        |> List.map (function
                            | x when x._id = _id ->
                                { x with
                                    connections =
                                        x.connections
                                        |> List.filter (function
                                            | AnyDesk(i, p) when i = conId -> false
                                            | RDesktop(i, p) when i = conId -> false
                                            | a -> true) }
                            | x -> x)

                    dg.Rows.Remove(dg.CurrentRow))

            conMenu.Click.Add(fun _ -> connect ())

            x.MainForm <-
                new Form(
                    Text = "Remote Connection Manager v1",
                    Width = 800,
                    Height = 600,
                    StartPosition = FormStartPosition.CenterScreen
                )

            let mf = x.MainForm
            mf.FormClosed.Add(fun _ -> x.StopApp())
            mf.Controls.Add dg
            dg.Dock <- DockStyle.Fill
            mf.MainMenuStrip <- mainMenu
            mf.MainMenuStrip |> mf.Controls.Add
            changePass.Click |> Event.add (fun _ -> authorize () |> ignore)

        member x.StopApp() =
            Db.writeTable items "data.edb" Db.password
            Application.Exit()

        static member Run() =
            if authorize () then
                let dt = Db.readTable "data.edb" Db.password
                let app = new App(dt)
                Application.Run(app)
    end
