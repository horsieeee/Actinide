Public Class Form1
    Dim dlls As New Dictionary(Of String, String)
    Private Declare Function OpenProcess Lib "kernel32" (ByVal dwDesiredAccess As Integer, ByVal bInheritHandle As Integer, ByVal dwProcessId As Integer) As Integer
    Private Declare Function VirtualAllocEx Lib "kernel32" (ByVal hProcess As Integer, ByVal lpAddress As Integer, ByVal dwSize As Integer, ByVal flAllocationType As Integer, ByVal flProtect As Integer) As Integer
    Private Declare Function WriteProcessMemory Lib "kernel32" (ByVal hProcess As Integer, ByVal lpBaseAddress As Integer, ByVal lpBuffer() As Byte, ByVal nSize As Integer, ByVal lpNumberOfBytesWritten As UInteger) As Boolean
    Private Declare Function GetProcAddress Lib "kernel32" (ByVal hModule As Integer, ByVal lpProcName As String) As Integer
    Private Declare Function GetModuleHandle Lib "kernel32" Alias "GetModuleHandleA" (ByVal lpModuleName As String) As Integer
    Private Declare Function CreateRemoteThread Lib "kernel32" (ByVal hProcess As Integer, ByVal lpThreadAttributes As Integer, ByVal dwStackSize As Integer, ByVal lpStartAddress As Integer, ByVal lpParameter As Integer, ByVal dwCreationFlags As Integer, ByVal lpThreadId As Integer) As Integer
    Private Declare Function WaitForSingleObject Lib "kernel32" (ByVal hHandle As Integer, ByVal dwMilliseconds As Integer) As Integer
    Private Declare Function CloseHandle Lib "kernel32" (ByVal hObject As Integer) As Integer
    Private Function Inject(ByVal pID As Integer, ByVal dllLocation As String) As Boolean
        Dim hProcess As Integer = OpenProcess(&H1F0FFF, 1, pID)
        If hProcess = 0 Then Return False
        Dim dllBytes As Byte() = System.Text.Encoding.ASCII.GetBytes(dllLocation)
        Dim allocAddress As Integer = VirtualAllocEx(hProcess, 0, dllBytes.Length, &H1000, &H4)
        If allocAddress = Nothing Then Return False
        Dim kernelMod As Integer = GetModuleHandle("kernel32.dll")
        Dim loadLibAddr = GetProcAddress(kernelMod, "LoadLibraryA")
        If kernelMod = 0 OrElse loadLibAddr = 0 Then Return False
        WriteProcessMemory(hProcess, allocAddress, dllBytes, dllBytes.Length, 0)
        Dim libThread As Integer = CreateRemoteThread(hProcess, 0, 0, loadLibAddr, allocAddress, 0, 0)

        If libThread = 0 Then
            Return False
        Else
            WaitForSingleObject(libThread, 5000)
            CloseHandle(libThread)
        End If
        CloseHandle(hProcess)
        Label3.Text = "Success!"
        If CheckBox1.Checked = True Then
            Me.Close()
        End If

        Return True
    End Function
    Private Sub Form1_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
        If My.Settings.inject = "auto" Then
            RadioButton2.Checked = True
        ElseIf My.Settings.inject = "manual" Then
            RadioButton1.Checked = True
        Else
            RadioButton2.Checked = True
        End If
        If My.Settings.close = 1 Then
            RadioButton2.Checked = True
        Else
            RadioButton2.Checked = False
        End If
    End Sub

    Private Sub OpenFileDialog1_FileOk(sender As System.Object, e As System.ComponentModel.CancelEventArgs) Handles OpenFileDialog1.FileOk
        Dim FileName As String = OpenFileDialog1.FileName.Substring(OpenFileDialog1.FileName.LastIndexOf("\"))
        Dim DllFileName As String = FileName.Replace("\", "")
        ListBox1.Items.Add(DllFileName)
        dlls.Add(DllFileName, OpenFileDialog1.FileName)

    End Sub

    Private Sub Timer1_Tick(sender As System.Object, e As System.EventArgs) Handles Timer1.Tick
        If ListBox1.Items.Count > 0 Then
            Dim TargetProcess As Process() = Process.GetProcessesByName(TextBox1.Text)
            If TargetProcess.Length = 0 Then
                Label3.Text = ("Waiting for " + TextBox1.Text + ".exe")

            Else
                Dim ProcID As Integer = Process.GetProcessesByName(TextBox1.Text)(0).Id
                Timer1.Stop()
                Timer2.Stop()

                For Each inj As KeyValuePair(Of String, String) In dlls
                    Inject(ProcID, inj.Value)
                Next
            End If
        End If
    End Sub

    Private Sub Timer2_Tick(sender As System.Object, e As System.EventArgs) Handles Timer2.Tick
        If TextBox1.Text = "" Then
            Label3.Text = "Waiting for process to be set"

            Timer1.Stop()
        ElseIf ListBox1.Items.Count = 0 Then
            Label3.Text = "Waiting for DLLs"
            Timer1.Stop()
        Else
            Dim TargetProcess As Process() = Process.GetProcessesByName(TextBox1.Text)
            If TargetProcess.Length = 0 Then
                Label3.Text = ("Waiting for " + TextBox1.Text + ".exe")
            Else
                If RadioButton2.Checked = True Then
                    Timer1.Start()
                End If
            End If
        End If
    End Sub

    Private Sub Button1_Click(sender As System.Object, e As System.EventArgs) Handles Button1.Click
        OpenFileDialog1.Filter = "DLL (*.dll) |*.dll"
        OpenFileDialog1.ShowDialog()
    End Sub

    Private Sub Button2_Click(sender As System.Object, e As System.EventArgs) Handles Button2.Click
        If ListBox1.SelectedIndex >= 0 Then
            OpenFileDialog1.Reset()
            dlls.Remove(ListBox1.SelectedItem)
            For i As Integer = (ListBox1.SelectedItems.Count - 1) To 0 Step -1
                Dim i2 As Integer = i + 2
                ListBox1.Items.Remove(ListBox1.SelectedItems(i))
            Next
        End If
    End Sub

    Private Sub Button3_Click(sender As System.Object, e As System.EventArgs) Handles Button3.Click
        ListBox1.Items.Clear()
        dlls.Clear()
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles CheckBox1.CheckedChanged
        If CheckBox1.Checked = True Then
            My.Settings.close = 1
        Else
            My.Settings.close = 0
        End If
        My.Settings.Save()
        My.Settings.Reload()
    End Sub

    Private Sub RadioButton1_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles RadioButton1.CheckedChanged
        My.Settings.inject = "manual"
        My.Settings.Save()
        My.Settings.Reload()
        Button4.Enabled = True
        Timer1.Stop()
    End Sub

    Private Sub RadioButton2_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles RadioButton2.CheckedChanged
        My.Settings.inject = "auto"
        My.Settings.Save()
        My.Settings.Reload()
        Button4.Enabled = False
        Timer1.Start()
    End Sub

    Private Sub Button4_Click(sender As System.Object, e As System.EventArgs) Handles Button4.Click
        If ListBox1.Items.Count > 0 Then
            If TextBox1.Text <> "" Then
                Dim TargetProcess As Process() = Process.GetProcessesByName(TextBox1.Text)
                If TargetProcess.Length = 0 Then
                    MsgBox(TextBox1.Text + ".exe is not running!", MsgBoxStyle.Critical, "!!!")
                Else
                    Timer1.Stop()
                    Dim ProcID As Integer = Process.GetProcessesByName(TextBox1.Text)(0).Id
                End If
            Else
                MsgBox("You didn't specify a process", MsgBoxStyle.Critical, "!!!")
            End If
        Else
            MsgBox("Select a DLL", MsgBoxStyle.Critical, "!!!")
        End If
    End Sub

    Private Sub Button6_Click(sender As System.Object, e As System.EventArgs) Handles Button6.Click
        Me.Close()
    End Sub
End Class
