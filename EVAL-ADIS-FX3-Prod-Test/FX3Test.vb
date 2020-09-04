Imports System.IO
Imports System.Threading
Imports FX3Api

Public Class FX3Test

    Private FX3 As FX3Connection
    Private ResourcePath As String
    Private TestRunner As Thread
    Private TestFailed As Boolean
    Dim SN As String

    Private Sub FX3Test_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'get executing path and resource path
        Try
            ResourcePath = GetPathToFile("..\..\..\Resources")
            FX3 = New FX3Connection(ResourcePath, ResourcePath, ResourcePath)
        Catch ex As Exception
            MsgBox("Error loading test software! " + ex.Message)
            btn_StartTest.Enabled = False
        End Try
        btn_StopTest.Enabled = False
        testStatus.Text = "Waiting"
        testStatus.BackColor = Color.LightGoldenrodYellow
    End Sub

    Private Sub Shutdown() Handles Me.Closed
        FX3.Disconnect()
    End Sub

    Private Function GetPathToFile(Name As String) As String
        Dim pathStr As String = System.AppDomain.CurrentDomain.BaseDirectory
        pathStr = Path.Combine(pathStr, Name)
        Return pathStr
    End Function

    Private Sub WriteLine(line As String)
        testConsole.AppendText(line + Environment.NewLine)
    End Sub

    Private Sub ClearLog()
        testConsole.Text = ""
    End Sub

    Private Sub TestFinished()
        Dim fileName As String
        Dim filePath As String
        Dim writer As StreamWriter
        Dim testPassedStr As String

        If TestFailed Then
            testPassedStr = "FAILED"
        Else
            testPassedStr = "PASSED"
        End If

        btn_StartTest.Enabled = True
        btn_StopTest.Enabled = False
        Invoke(Sub() WriteLine("Test Run Finish Time: " + DateTime.Now.ToString()))
        If Not TestFailed Then
            testStatus.Text = "Passing"
            'disconnect all boards
            FX3.Disconnect()
        End If
        'save log file to CSV
        fileName = "ADIS_FX3_TEST_SN" + SN + "_" + testPassedStr.ToString() + "_" + Now.ToString("s") + ".txt"
        fileName = fileName.Replace(":", "-")
        Try
            If Not Directory.Exists(GetPathToFile("log")) Then Directory.CreateDirectory(GetPathToFile("log"))
            filePath = GetPathToFile("log\" + fileName)
            writer = New StreamWriter(filePath, FileMode.Create)
            writer.WriteLine(testConsole.Text)
            writer.Close()
        Catch ex As Exception
            MsgBox("Log write error! " + ex.Message)
        End Try
    End Sub

    Private Sub btn_StartTest_Click(sender As Object, e As EventArgs) Handles btn_StartTest.Click
        btn_StartTest.Enabled = False
        btn_StopTest.Enabled = True
        ClearLog()
        testStatus.Text = "Running"
        TestFailed = False
        SN = "NONE"
        testStatus.BackColor = Color.LightGreen
        TestRunner = New Thread(AddressOf TestRunWork)
        TestRunner.Start()
    End Sub

    Private Sub btn_StopTest_Click(sender As Object, e As EventArgs) Handles btn_StopTest.Click
        testStatus.Text = "Canceled"
        testStatus.BackColor = Color.LightGoldenrodYellow
        'set test failed flag to stop test execution process in thread
        WriteLine("Test canceled!")
        TestFailed = True
    End Sub

    Private Sub TestRunWork()
        Invoke(Sub() WriteLine("Test Run Start Time: " + DateTime.Now.ToString()))
        Invoke(Sub() WriteLine("FX3 Resource Path: " + ResourcePath))
        WaitForBoard()
        LoadFirmware()
        InitErrorLog()
        RebootTest()
        PinTest()
        Invoke(Sub() TestFinished())
    End Sub

    Private Sub WaitForBoard()
        If TestFailed Then Exit Sub

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
        If TestFailed Then Exit Sub

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
            SN = FX3.ActiveFX3SerialNumber
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

    Private Sub InitErrorLog()
        'skip if failure occurred earlier
        If TestFailed Then Exit Sub

        'error log count
        Dim logCount As UInteger

        Invoke(Sub() WriteLine("Initializing FX3 NVM Error Log..."))
        Try
            FX3.ClearErrorLog()
            logCount = FX3.GetErrorLogCount()
            If logCount <> 0 Then
                Throw New Exception("Error log failed to clear. Count " + logCount.ToString())
            End If
        Catch ex As Exception
            Invoke(Sub()
                       WriteLine("ERROR: Log init failed! " + ex.Message)
                       testStatus.Text = "FAILED"
                       testStatus.BackColor = Color.Red
                   End Sub)
            TestFailed = True
            Exit Sub
        End Try

        Invoke(Sub() WriteLine("Log successfully initialized..."))
    End Sub

    Private Sub RebootTest()
        'skip if failure occurred earlier
        If TestFailed Then Exit Sub

        Dim sn As String = FX3.ActiveFX3.SerialNumber
        Dim logCount As UInteger

        Invoke(Sub() WriteLine("Rebooting FX3..."))
        Try
            FX3.Disconnect()
            FX3.WaitForBoard(20)
            FX3.Connect(sn)
            logCount = FX3.GetErrorLogCount()
            If logCount <> 0 Then
                Throw New Exception("Non-zero error log. Count " + logCount.ToString())
            End If
        Catch ex As Exception
            Invoke(Sub()
                       WriteLine("ERROR: FX3 reboot failed: " + ex.Message)
                       testStatus.Text = "FAILED"
                       testStatus.BackColor = Color.Red
                   End Sub)
            TestFailed = True
            Exit Sub
        End Try

        Invoke(Sub() WriteLine("FX3 successfully rebooted..."))

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
        Const NUM_PIN_TRIALS As Integer = 8

        Dim pinLowTime, pinHighTime As Double

        'check if we can do PWM test (timer hardware mux lines up correctly)
        Dim pwmTest As Boolean = True
        'timer 0 used as FX3 general purpose timer, will except if you try to use
        If (pin1.PinNumber Mod 8) = 0 Then pwmTest = False
        If (pin2.PinNumber Mod 8) = 0 Then pwmTest = False

        'share the same timer
        If (pin1.PinNumber Mod 8) = (pin2.PinNumber Mod 8) Then pwmTest = False

        Try
            Invoke(Sub() WriteLine("Starting pin read/write test..."))
            'pin1 drives, pin2 reads
            FX3.ReadPin(pin2)
            For trial As Integer = 1 To NUM_PIN_TRIALS
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
            For trial As Integer = 1 To NUM_PIN_TRIALS
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

            If pwmTest Then
                Invoke(Sub() WriteLine("Starting pin clock generation test..."))
                FX3.ReadPin(pin1)
                FX3.ReadPin(pin2)
                'pin measure works on 10MHz timebase. 0.7 duty cycle @1MHz -> 0.7us high, 0.3us low
                FX3.StartPWM(1000000, 0.7, pin1)
                System.Threading.Thread.Sleep(1)
                For trial As Integer = 1 To NUM_PIN_TRIALS
                    pinLowTime = 1000 * FX3.MeasureBusyPulse({0, 0}, pin2, 0, 100)
                    pinHighTime = 1000 * FX3.MeasureBusyPulse({0, 0}, pin2, 1, 100)
                    'off by more than 0.15us
                    If Math.Abs(0.3 - pinLowTime) > 0.15 Then Throw New Exception("Invalid pin low time, " + pinLowTime.ToString("f2") + "us")
                    If Math.Abs(0.7 - pinHighTime) > 0.15 Then Throw New Exception("Invalid pin high time, " + pinHighTime.ToString("f2") + "us")
                Next
            End If

        Catch ex As Exception
            Invoke(Sub()
                       WriteLine("ERROR: FX3 GPIO[" + pin1.PinNumber.ToString() + "] <-> FX3 GPIO[" + pin2.PinNumber.ToString() + "] loop back failed! " + ex.Message)
                       testStatus.Text = "FAILED"
                       testStatus.BackColor = Color.Red
                   End Sub)
            FX3.StopPWM(pin1)
            TestFailed = True
            Exit Sub
        End Try
        'no errors
        Invoke(Sub() WriteLine("Pin connections validated..."))

    End Sub

End Class
