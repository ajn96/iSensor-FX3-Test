<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FX3Test
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FX3Test))
        Me.btn_StartTest = New System.Windows.Forms.Button()
        Me.testConsole = New System.Windows.Forms.TextBox()
        Me.btn_StopTest = New System.Windows.Forms.Button()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.testStatus = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'btn_StartTest
        '
        Me.btn_StartTest.Location = New System.Drawing.Point(12, 12)
        Me.btn_StartTest.Name = "btn_StartTest"
        Me.btn_StartTest.Size = New System.Drawing.Size(80, 30)
        Me.btn_StartTest.TabIndex = 0
        Me.btn_StartTest.Text = "Start Test"
        Me.btn_StartTest.UseVisualStyleBackColor = True
        '
        'testConsole
        '
        Me.testConsole.BackColor = System.Drawing.SystemColors.ButtonHighlight
        Me.testConsole.Location = New System.Drawing.Point(12, 49)
        Me.testConsole.Multiline = True
        Me.testConsole.Name = "testConsole"
        Me.testConsole.ReadOnly = True
        Me.testConsole.Size = New System.Drawing.Size(601, 517)
        Me.testConsole.TabIndex = 3
        '
        'btn_StopTest
        '
        Me.btn_StopTest.Location = New System.Drawing.Point(97, 12)
        Me.btn_StopTest.Name = "btn_StopTest"
        Me.btn_StopTest.Size = New System.Drawing.Size(80, 30)
        Me.btn_StopTest.TabIndex = 4
        Me.btn_StopTest.Text = "Abort Test"
        Me.btn_StopTest.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(463, 21)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(64, 13)
        Me.Label1.TabIndex = 5
        Me.Label1.Text = "Test Status:"
        '
        'testStatus
        '
        Me.testStatus.Location = New System.Drawing.Point(533, 12)
        Me.testStatus.Name = "testStatus"
        Me.testStatus.Size = New System.Drawing.Size(80, 30)
        Me.testStatus.TabIndex = 6
        Me.testStatus.Text = "Label2"
        Me.testStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'FX3Test
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(626, 578)
        Me.Controls.Add(Me.testStatus)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.btn_StopTest)
        Me.Controls.Add(Me.testConsole)
        Me.Controls.Add(Me.btn_StartTest)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "FX3Test"
        Me.Text = "EVAL-ADIS-FX3 Production Test"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents btn_StartTest As Button
    Friend WithEvents testConsole As TextBox
    Friend WithEvents btn_StopTest As Button
    Friend WithEvents Label1 As Label
    Friend WithEvents testStatus As Label
End Class
