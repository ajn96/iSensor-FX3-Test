Imports System.IO
Imports System.Threading
Imports FX3Api

Public Class FX3Test

    Private FX3 As FX3Connection
    Private ResourcePath As String
    Private TestRunner As Thread
    Private TestFailed As Boolean

    Private Sub FX3Test_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'get executing path and resource path
        Try
            ResourcePath = System.AppDomain.CurrentDomain.BaseDirectory
            ResourcePath = Path.GetFullPath(Path.Combine(ResourcePath, "..\..\..\"))
            ResourcePath = Path.Combine(ResourcePath, "Resources")
            FX3 = New FX3Connection(ResourcePath, ResourcePath, ResourcePath)
        Catch ex As Exception
            MsgBox("Error loading test software! " + ex.Message)
            btn_StartTest.Enabled = False
        End Try
        btn_StopTest.Enabled = False
        testStatus.Text = "Waiting"
        testStatus.BackColor = Color.LightGoldenrodYellow
    End Sub

    Private Sub WriteLine(line As String)
        testConsole.AppendText(line + Environment.NewLine)
    End Sub

    Private Sub ClearLog()
        testConsole.Text = ""
    End Sub

    Private Sub TestFinished()
        btn_StartTest.Enabled = True
        btn_StopTest.Enabled = False
        Invoke(Sub() WriteLine("Test Run Finish Time: " + DateTime.Now.ToString()))
        If Not TestFailed Then
            testStatus.Text = "Passing"
            'disconnect all boards
            FX3.Disconnect()
        End If
        'save log file to CSV
    End Sub

    Private Sub btn_StartTest_Click(sender As Object, e As EventArgs) Handles btn_StartTest.Click
        btn_StartTest.Enabled = False
        btn_StopTest.Enabled = True
        ClearLog()
        testStatus.Text = "Running"
        TestFailed = False
        testStatus.BackColor = Color.LightGreen
        TestRunner = New Thread(AddressOf TestRunWork)
        TestRunner.Start()
    End Sub

    Private Sub btn_StopTest_Click(sender As Object, e As EventArgs) Handles btn_StopTest.Click
        testStatus.Text = "Canceled"
        testStatus.BackColor = Color.LightGoldenrodYellow
        TestFinished()
    End Sub

    Private Sub TestRunWork()
        Invoke(Sub() WriteLine("Test Run Start Time: " + DateTime.Now.ToString()))
        Invoke(Sub() WriteLine("FX3 Resource Path: " + ResourcePath))
        WaitForBoard()
        LoadFirmware()
        PinTest()
        Invoke(Sub() TestFinished())
    End Sub

    Private Sub WaitForBoard()
        Invoke(Sub() WriteLine("Scanning for FX3 boards..."))

        'bootloader already available
        If FX3.AvailableFX3s.Count > 0 Then
            Invoke(Sub() WriteLine("FX3 detected already operating in bootloader mode..."))
            Exit Sub
        End If

        If FX3.BusyFX3s.Count > 0 Then
            Invoke(Sub() WriteLine("Warning: FX3 running application code detected. Issuing reset..."))
            FX3.ResetAllFX3s()
        End If

        Invoke(Sub() WriteLine("Waiting for bootloader to enumerate (20 second timeout)..."))
        FX3.WaitForBoard(20)

    End Sub

    Private Sub LoadFirmware()
        If FX3.AvailableFX3s.Count > 0 Then
            Invoke(Sub() WriteLine("FX3 bootloader device found..."))
        Else
            Invoke(Sub()
                       WriteLine("ERROR: No FX3 bootloader device found...")
                       testStatus.Text = "FAILED"
                       testStatus.BackColor = Color.Red
                   End Sub)
            TestFailed = True
            Exit Sub
        End If
        Invoke(Sub() WriteLine("Connecting to FX3 SN" + FX3.AvailableFX3s(0) + "..."))
        Try
            FX3.Connect(FX3.AvailableFX3s(0))
        Catch ex As Exception
            Invoke(Sub()
                       WriteLine("ERROR: " + ex.Message)
                       testStatus.Text = "FAILED"
                       testStatus.BackColor = Color.Red
                   End Sub)
            TestFailed = True
            Exit Sub
        End Try
        Invoke(Sub() WriteLine("Connected! " + FX3.ActiveFX3.ToString()))
    End Sub

    Private Sub PinTest()
        'skip if failure occurred earlier
        If TestFailed Then Exit Sub

        'get pin object references to SPI port pins
        FX3.BitBangSpiConfig = New BitBangSpiConfig(True)

        'read all pins to tri-state
        Invoke(Sub() WriteLine("Tri-stating all FX3 GPIO..."))
        Try
            FX3.ReadPin(FX3.DIO1)
            FX3.ReadPin(FX3.DIO2)
            FX3.ReadPin(FX3.DIO3)
            FX3.ReadPin(FX3.DIO4)
            FX3.ReadPin(FX3.FX3_GPIO1)
            FX3.ReadPin(FX3.FX3_GPIO2)
            FX3.ReadPin(FX3.FX3_GPIO3)
            FX3.ReadPin(FX3.FX3_GPIO4)
            FX3.ReadPin(FX3.BitBangSpiConfig.CS)
            FX3.ReadPin(FX3.BitBangSpiConfig.SCLK)
            FX3.ReadPin(FX3.BitBangSpiConfig.MISO)
            FX3.ReadPin(FX3.BitBangSpiConfig.MOSI)
            FX3.ReadPin(FX3.ResetPin)
            'uart
            FX3.ReadPin(New FX3PinObject(48))
        Catch ex As Exception
            Invoke(Sub()
                       WriteLine("ERROR: Unexpected exception during pin read")
                       testStatus.Text = "FAILED"
                       testStatus.BackColor = Color.Red
                   End Sub)
            TestFailed = True
            Exit Sub
        End Try

        Invoke(Sub() WriteLine("Testing DIO1 <-> SCLK..."))
        TestPins(FX3.DIO1, FX3.BitBangSpiConfig.SCLK)
        Invoke(Sub() WriteLine("Testing MOSI <-> MISO..."))
        TestPins(FX3.BitBangSpiConfig.MOSI, FX3.BitBangSpiConfig.MISO)
        Invoke(Sub() WriteLine("Testing DIO2 <-> CS..."))
        TestPins(FX3.DIO1, FX3.BitBangSpiConfig.CS)
        Invoke(Sub() WriteLine("Testing DIO3 <-> DIO4..."))
        TestPins(FX3.DIO3, FX3.DIO4)
        Invoke(Sub() WriteLine("Testing FX3_GPIO1 <-> FX3_GPIO2..."))
        TestPins(FX3.FX3_GPIO1, FX3.FX3_GPIO2)
        Invoke(Sub() WriteLine("Testing FX3_GPIO3 <-> FX3_GPIO4..."))
        TestPins(FX3.FX3_GPIO3, FX3.FX3_GPIO4)
        Invoke(Sub() WriteLine("Testing Reset <-> Debug Tx..."))
        TestPins(FX3.ResetPin, New FX3PinObject(48))
    End Sub

    Private Sub TestPins(pin1 As FX3PinObject, pin2 As FX3PinObject)

        Try
            'pin1 drives, pin2 reads
            FX3.ReadPin(pin2)
            For trial As Integer = 0 To 7
                FX3.SetPin(pin1, 0)
                System.Threading.Thread.Sleep(10)
                If FX3.ReadPin(pin2) <> 0 Then
                    Throw New Exception("Expected logic low, was high")
                End If
                FX3.SetPin(pin1, 1)
                System.Threading.Thread.Sleep(10)
                If FX3.ReadPin(pin2) <> 1 Then
                    Throw New Exception("Expected logic high, was low")
                End If
            Next

            'pin2 drives, pin1 reads
            FX3.ReadPin(pin1)
            For trial As Integer = 0 To 7
                FX3.SetPin(pin2, 0)
                System.Threading.Thread.Sleep(10)
                If FX3.ReadPin(pin1) <> 0 Then
                    Throw New Exception("Expected logic low, was high")
                End If
                FX3.SetPin(pin2, 1)
                System.Threading.Thread.Sleep(10)
                If FX3.ReadPin(pin1) <> 1 Then
                    Throw New Exception("Expected logic high, was low")
                End If
            Next

        Catch ex As Exception
            Invoke(Sub()
                       WriteLine("ERROR: FX3 GPIO[" + pin1.PinNumber.ToString() + "] <-> FX3 GPIO[" + pin2.PinNumber.ToString() + "] loop back failed! " + ex.Message)
                       testStatus.Text = "FAILED"
                       testStatus.BackColor = Color.Red
                   End Sub)
            TestFailed = True
            Exit Sub
        End Try
        'no errors
        Invoke(Sub() WriteLine("Pin connections validated..."))

    End Sub

End Class
