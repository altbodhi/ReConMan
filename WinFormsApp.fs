module ReConMan.WinFormsApp

open System
open System.Data
open System.Windows.Forms
open ReConMan

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
            conType.Items.Add("AnyDesk") |> ignore
            conType.Items.Add("RDesktop") |> ignore
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
        member x.ConType = conType.SelectedItem
    end

let authorize () =
    use authForm = new Authorize()

    if authForm.Accept() then
        Db.password <- authForm.Pass
        true
    else
        false

type App(dt: DataTable) as x =
    class
        inherit ApplicationContext()

        let dg =
            new DataGridView(
                DataSource = dt,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true
            )

        let connect ct =
            if dg.CurrentRow = null then
                ()
            else
                let id = dg.CurrentRow.Cells["id"].Value.ToString()
                let rows = dt.Select($"id = '{id}' AND kind='{ct}'")

                if rows.Length = 1 then
                    let row = rows |> Array.item 0
                    let conId = row.Item("con_id").ToString()
                    let pass = row.Item("pass").ToString()
                    let ex = Init.rdesktopExecutable ()
                    RDesktop.run ex conId pass |> ignore

        let mainMenu = new MenuStrip()

        let editMenu =
            mainMenu.Items.Add(new ToolStripMenuItem("&Items"))
            |> fun i -> mainMenu.Items.Item(i) :?> ToolStripMenuItem

        let add = editMenu.DropDownItems.Add("&Add")
        let edit = editMenu.DropDownItems.Add("&Edit")
        let delete = editMenu.DropDownItems.Add("&Delete")

        let conMenu =
            mainMenu.Items.Add(new ToolStripMenuItem("&Connect"))
            |> fun i -> mainMenu.Items.Item(i) :?> ToolStripMenuItem

        let rd = conMenu.DropDownItems.Add("&RDesktop")
        let an = conMenu.DropDownItems.Add("&AnyDesk")

        let quit = mainMenu.Items.Add("&Quit")

        do

            dg.DataBindingComplete.Add(fun _ -> dg.Columns["pass"].Visible <- false)

            add.Click.Add(fun _ ->
                use frm = new EditConnection("", "AnyDesk", "", "")

                if frm.Accept() then
                    dt.Rows.Add([| box frm.Name; box frm.ConType; box frm.ConId; box frm.Pass |])
                    |> ignore)

            edit.Click.Add(fun _ ->
                if dg.CurrentRow = null then
                    ()
                else
                    let cells = dg.CurrentRow.Cells

                    use frm =
                        new EditConnection(
                            cells["id"].Value.ToString(),
                            cells["kind"].Value.ToString(),
                            cells["con_id"].Value.ToString(),
                            cells["pass"].Value.ToString()
                        )

                    if frm.Accept() then
                        dt.Rows.Add([| box frm.Name; box frm.ConType; box frm.ConId; box frm.Pass |])
                        |> ignore)

            delete.Click.Add(fun _ ->
                if dg.CurrentRow = null then
                    ()
                else if MessageBox.Show("delete item?", "Warning", MessageBoxButtons.YesNo) = DialogResult.Yes then
                    dg.Rows.Remove(dg.CurrentRow))

            rd.Click.Add(fun _ -> connect "RDesktop")
            an.Click.Add(fun _ -> connect "AnyDesk")

            x.MainForm <-
                new Form(
                    Text = "Remote Connection Manager v1",
                    Width = 800,
                    Height = 600,
                    StartPosition = FormStartPosition.CenterScreen,
                    ControlBox = false
                )

            let mf = x.MainForm
            mf.Controls.Add dg
            dg.Dock <- DockStyle.Fill
            mf.MainMenuStrip <- mainMenu
            mf.MainMenuStrip |> mf.Controls.Add
            quit.Click |> Event.add (fun _ -> x.StopApp())

        member x.StopApp() =
            Db.writeTable dt "data.edb" Db.password
            Application.Exit()

        static member Run() =
            if authorize () then
                let dt = Db.readTable "data.edb" Db.password
                let app = new App(dt)
                Application.Run(app)
    end
