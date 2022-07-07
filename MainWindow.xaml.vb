Imports System.ComponentModel
Imports System.IO
Imports System.Windows.Forms
Imports ThermoFisher.CommonCore.Data.Interfaces
Imports ThermoFisher.CommonCore.RawFileReader

Class MainWindow

    'Hardcodes: 
    Dim MinFileSize As Long = 10485760 '10 MB
    Dim MaxFileSize As Long = 10737418240 '10 GB
    Dim minInterval As String = "60000" 'millisec (1 min) --> refreshing monitored folder rate

    'Declarations and definitions:
    Dim debugMode As Boolean = False '<--------------DEBUG MODE SWITCH
    Private Shared myTimer As New Timers.Timer
    Dim bw As BackgroundWorker = New BackgroundWorker
    Dim uplodadresult As Boolean = False
    Dim uploadresultcode As New ArrayList()
    Dim stopped As Boolean = False
    Dim localpathtouploadfile As String = ""
    Dim monitoredfolder As String = ""
    Dim filesToProcess As New ArrayList()
    Private Shared BlackListOfFiles As New ArrayList()
    Private Shared CleanedListOfFiles As New ArrayList()
    Dim excludeQCloudFile As Boolean = False
    Dim rawFileInstrumentSerial As String = ""
    Dim rawFileInstrumentName As String = ""
    Dim firstMonitoredFolderCheck As Boolean = True
    Dim cleanFilesList As New ArrayList()

    Public Sub New()

        InitializeComponent()

        bw.WorkerSupportsCancellation = True
        AddHandler myTimer.Elapsed, AddressOf filesManager
        AddHandler bw.DoWork, AddressOf bw_DoWork
        AddHandler bw.RunWorkerCompleted, AddressOf bw_RunWorkerCompleted

        cbSubFolder1.Items.Add("Instrument Name")
        cbSubFolder1.Items.Add("Serial Number")
        cbSubFolder1.Items.Add("YYMM")
        cbSubFolder2.Items.Add("Instrument Name")
        cbSubFolder2.Items.Add("Serial Number")
        cbSubFolder2.Items.Add("YYMM")
        cbSubFolder3.Items.Add("Instrument Name")
        cbSubFolder3.Items.Add("Serial Number")
        cbSubFolder3.Items.Add("YYMM")
        cbSubFolder1.IsEnabled = False
        cbSubFolder2.IsEnabled = False
        cbSubFolder3.IsEnabled = False
        bStartSync.IsEnabled = False
        bStopSync.IsEnabled = False
        bClearLog.IsEnabled = False
        bOutputFolder.IsEnabled = False
        bCopyLogToClipboard.IsEnabled = False
        cbDiscardQCloudFiles.IsChecked = True
        tbxInputFolderSummary.IsEnabled = False
        tbxOutputFolderSummary.IsEnabled = False
        tbxInputFolder.IsEnabled = False
        tbxOutputRootFolder.IsEnabled = False
        tbxInputFolderSummary.Text = tbxInputFolder.Text

        ' Debug mode:
        If debugMode Then bStartSync.IsEnabled = True
        If debugMode Then bStopSync.IsEnabled = True
        If debugMode Then bClearLog.IsEnabled = True
        If debugMode Then bCopyLogToClipboard.IsEnabled = True
        If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "------------DEBUG MODE ON------------")
        If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Handlers added (filesManager, DoWork And RunWorkerCompleted.")
        If debugMode Then tbxInputFolder.Text = "C:\rolivella\XCalibur\data"
        If debugMode Then tbxOutputRootFolder.Text = "C:\rolivella\XCalibur\backup"
        'If debugMode Then tbxOutputFolderSummary.Text = "C:\rolivella\XCalibur\backup"
        'If debugMode Then tbxOutputRootFolder.Text = "Z:\data\orbitrap_xl\qcml"

        ' Unit Tests: 
        'tbxInputFolder.Text = "C:\rolivella\XCalibur\data"
        'tbxOutputRootFolder.Text = "C:\rolivella\XCalibur\backup"
        'tbxOutputRootFolder.Text = "Z:\data\orbitrap_xl\qcml"
        'bStartSync.IsEnabled = True
        'bStopSync.IsEnabled = True

    End Sub

    Private Sub cbSubFolder1_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles cbSubFolder1.SelectionChanged
        tbxOutputFolderSummary.Text = tbxOutputRootFolder.Text & "\" & cbSubFolder1.SelectedItem

    End Sub

    Private Sub cbSubFolder2_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles cbSubFolder2.SelectionChanged
        tbxOutputFolderSummary.Text = tbxOutputRootFolder.Text & "\" & cbSubFolder1.SelectedItem & "\" & cbSubFolder2.SelectedItem
    End Sub

    Private Sub cbSubFolder3_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles cbSubFolder3.SelectionChanged
        tbxOutputFolderSummary.Text = tbxOutputRootFolder.Text & "\" & cbSubFolder1.SelectedItem & "\" & cbSubFolder2.SelectedItem & "\" & cbSubFolder3.SelectedItem
    End Sub

    Private Sub bInputfolder_Click(sender As Object, e As RoutedEventArgs) Handles bInputfolder.Click
        Dim dialog As New FolderBrowserDialog()
        dialog.RootFolder = Environment.SpecialFolder.Desktop
        dialog.SelectedPath = "C:\"
        dialog.Description = "Select Application Configeration Files Path"
        If dialog.ShowDialog() = Windows.Forms.DialogResult.OK Then
            tbxInputFolder.Text = dialog.SelectedPath
            tbxInputFolderSummary.Text = dialog.SelectedPath
            bOutputFolder.IsEnabled = True
        End If
    End Sub

    Private Sub bOutputFolder_Click(sender As Object, e As RoutedEventArgs) Handles bOutputFolder.Click
        Dim dialog As New FolderBrowserDialog()
        dialog.RootFolder = Environment.SpecialFolder.Desktop
        dialog.SelectedPath = "C:\"
        dialog.Description = "Select Application Configeration Files Path"
        If dialog.ShowDialog() = Windows.Forms.DialogResult.OK Then
            tbxOutputRootFolder.Text = dialog.SelectedPath
            tbxOutputFolderSummary.Text = dialog.SelectedPath
            If tbxInputFolder.Text = tbxOutputRootFolder.Text Then
                lbLog.Items.Insert(0, getCurrentLogDate() & "[ERROR] Input path must not be the same as output path.")
            Else
                bStartSync.IsEnabled = True
                bStopSync.IsEnabled = True
                bClearLog.IsEnabled = True
                bCopyLogToClipboard.IsEnabled = True
                ' Sets the timer interval (millisec).
                myTimer.Interval = minInterval
            End If
        End If
    End Sub

    Private Sub cbDiscardQCloudFiles_Checked(sender As Object, e As RoutedEventArgs) Handles cbEnableBackupSubfolders.Checked
        excludeQCloudFile = True
    End Sub

    Private Sub cbDiscardQCloudFiles_Unchecked(sender As Object, e As RoutedEventArgs) Handles cbEnableBackupSubfolders.Unchecked
        excludeQCloudFile = False
    End Sub

    Private Sub cbEnableBackupSubfolders_Checked(sender As Object, e As RoutedEventArgs) Handles cbEnableBackupSubfolders.Checked
        If cbEnableBackupSubfolders.IsChecked Then
            cbSubFolder1.IsEnabled = True
            cbSubFolder2.IsEnabled = True
            cbSubFolder3.IsEnabled = True
        End If
    End Sub

    Private Sub cbEnableBackupSubfolders_Unchecked(sender As Object, e As RoutedEventArgs) Handles cbEnableBackupSubfolders.Unchecked
        If cbEnableBackupSubfolders.IsChecked = False Then
            cbSubFolder1.IsEnabled = False
            cbSubFolder2.IsEnabled = False
            cbSubFolder3.IsEnabled = False
            cbSubFolder1.Text = ""
            cbSubFolder2.Text = ""
            cbSubFolder3.Text = ""
            tbxOutputFolderSummary.Text = tbxOutputRootFolder.Text
        End If
    End Sub

    Private Function getCurrentLogDate() As String
        Dim currentDate As DateTime = DateTime.Now
        Dim currentMonthFolder As String
        currentMonthFolder = Format$(currentDate, "yyyy-MM-dd HH:mm:ss")
        Return "[" & currentMonthFolder & "] "
    End Function

    Private Function getCurrentMonthFolder() As String
        Dim currentDate As DateTime = DateTime.Now
        Dim currentMonthFolder As String
        currentMonthFolder = Format$(currentDate, "yyMM")
        Return currentMonthFolder
    End Function

    Private Function getFilesNotInBlackList(fileslist As ArrayList) As ArrayList
        Dim output As New ArrayList()
        For Each file In fileslist
            If BlackListOfFiles.IndexOf(Path.GetFileName(file)) = -1 Then
                output.Add(file)
            End If
        Next
        Return output
    End Function

    ' RAW files filter
    Private Function getCleanFileList(fileslist As ArrayList) As ArrayList
        CleanedListOfFiles.Clear()
        For Each file In fileslist
            ' Check if the files is locked by another program: 
            If IsFileInUse(file) Then
                CleanedListOfFiles.Remove(file)
                lbLog.Items.Insert(0, getCurrentLogDate() & "[WARNING] File cannot be moved because is locked by another program. Please check: " & file)
                BlackListOfFiles.Add(Path.GetFileName(file))
            Else
                Dim filereader As System.IO.FileInfo = My.Computer.FileSystem.GetFileInfo(file)
                Try
                    ' Check that the file has the right size
                    If filereader.Length >= MinFileSize And filereader.Length <= MaxFileSize Then
                        ' Check if it's a QCloud file
                        If (Path.GetFileName(file).Contains("QC01") Or Path.GetFileName(file).Contains("QC02") Or Path.GetFileName(file).Contains("QC03") And Not excludeQCloudFile) Then
                            lbLog.Items.Insert(0, getCurrentLogDate() & "[WARNING] File not moved because is a QCloud file (QC01 or QC02): " & file)
                            BlackListOfFiles.Add(Path.GetFileName(file))
                        Else
                            Dim rawFile As IRawDataPlus = RawFileReaderAdapter.FileFactory(file) '------> Open rawFile by Thermo lib
                            'Check if it's really a Thermo Raw file with MS data
                            If rawFile.HasMsData Then
                                rawFile.SelectInstrument(instrumentType:=0, 1)
                                rawFileInstrumentSerial = rawFile.GetInstrumentData().SerialNumber
                                rawFileInstrumentName = rawFile.GetInstrumentData().Name.ToString.Replace(" ", "_").ToLower
                                CleanedListOfFiles.Add(file) 'If RAW file pass all filters, then is a valid file
                            Else
                                lbLog.Items.Insert(0, getCurrentLogDate() & "[WARNING] File without MS data: " & file)
                                BlackListOfFiles.Add(Path.GetFileName(file))
                            End If
                            rawFile.Dispose() '------> Close rawFile by Thermo lib
                        End If
                    Else
                        lbLog.Items.Insert(0, getCurrentLogDate() & "[WARNING] File not moved because its size is not between the allowed interval " & Math.Round(MinFileSize / 1048576) & "MB and " & Math.Round(MaxFileSize / 1073741824) & "GB: " & file)
                        BlackListOfFiles.Add(Path.GetFileName(file))
                    End If
                Catch ex As Exception
                    If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Instrument index not available for requested device" & vbCrLf & "Parameter name: instrumentIndex" & ex.Message)
                    BlackListOfFiles.Add(Path.GetFileName(file))
                    CleanedListOfFiles.Remove(file)
                End Try

            End If
        Next
        If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Number of clean files: " & CleanedListOfFiles.Count)
        Return CleanedListOfFiles
    End Function

    ' Move files manager
    Private Function moveFileToTarget(filenameToUpload As String, OriginFullPath As String, targetOnlyfolder As String) As ArrayList

        Dim targetFullPath As String = targetOnlyfolder & "\" & filenameToUpload
        Dim uploadStorageCode As String = ""
        Dim output As New ArrayList()

        Try

            localpathtouploadfile = targetFullPath

            'Upload file when applies
            If Not stopped Then
                Try

                    'Move files 
                    If moveFile(OriginFullPath, targetFullPath, targetOnlyfolder) Then
                        uploadStorageCode = "ST-OK"
                    Else
                        uploadStorageCode = "ST-ERR"
                    End If

                Catch uploadex As Exception
                    Console.WriteLine(uploadex.Message)
                    uploadStorageCode = "ST-ERR"
                End Try

            End If

        Catch genex As Exception
            Console.WriteLine(genex.Message)
            uploadStorageCode = "ST-ERR"
        End Try

        output.Add(uploadStorageCode)

        Return output

    End Function

    ' Move files
    Private Function moveFile(OriginFullPath As String, targetFullPath As String, targetOnlyfolder As String) As Boolean
        Try
            If (Not System.IO.Directory.Exists(targetOnlyfolder)) Then
                System.IO.Directory.CreateDirectory(targetOnlyfolder)
            End If
            If File.Exists(targetFullPath) Then
                'File.Delete(targetFullPath)
                lbLog.Items.Insert(0, getCurrentLogDate() & "[WARNING] Repeated file at destination folder! So it will not be moved.")
            Else
                File.Move(OriginFullPath, targetFullPath)
            End If

        Catch ex As Exception
            lbLog.Items.Insert(0, getCurrentLogDate() & "[ERROR] Reason: " & ex.Message)
            Return False
        End Try
        Return True
    End Function

    ' Run worker
    Private Sub bw_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)
        Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)
        Dim workerInputs As String() = e.Argument
        uploadresultcode = moveFileToTarget(workerInputs(0), workerInputs(1), workerInputs(2))
        e.Result = localpathtouploadfile
    End Sub

    'Run worker completed
    Private Sub bw_RunWorkerCompleted(ByVal sender As Object, ByVal e As RunWorkerCompletedEventArgs)

        If Not stopped Then

            Dim uploadResultCodeStorage = uploadresultcode.Item(0)

            If uploadResultCodeStorage Is "ST-OK" Then
                lbLog.Items.Insert(0, getCurrentLogDate() & ":) File successfully moved to " & Path.GetDirectoryName(e.Result) & "\" & Path.GetFileName(e.Result))
            ElseIf uploadResultCodeStorage Is "ST-ERR" Then
                BlackListOfFiles.Add(Path.GetFileName(e.Result))
                lbLog.Items.Insert(0, getCurrentLogDate() & "[ERROR] Failed to move file " & Path.GetDirectoryName(e.Result) & "\" & Path.GetFileName(e.Result))
            End If

        End If

        myTimer.Start()

    End Sub

    Private Sub bstopsync_click(sender As Object, e As RoutedEventArgs) Handles bStopSync.Click
        If bw.WorkerSupportsCancellation = True Then
            bw.CancelAsync()
        End If
        bStartSync.IsEnabled = True
        bStopSync.IsEnabled = False
        myTimer.Stop()
        stopped = True
        lbLog.Items.Insert(0, getCurrentLogDate() & "STOP monitoring RAW files at " & monitoredfolder)
    End Sub

    Private Sub bclearlog_click(sender As Object, e As RoutedEventArgs) Handles bClearLog.Click
        lbLog.Items.Clear()
    End Sub

    Private Sub bStartSync_Click(sender As Object, e As RoutedEventArgs) Handles bStartSync.Click

        monitoredfolder = tbxInputFolder.Text

        ' Initialize variables: 
        BlackListOfFiles.Add("")
        bStartSync.IsEnabled = False
        bStopSync.IsEnabled = True
        bCopyLogToClipboard.IsEnabled = True
        bClearLog.IsEnabled = True
        cbSubFolder1.IsEnabled = False
        cbSubFolder2.IsEnabled = False
        cbSubFolder3.IsEnabled = False
        bInputfolder.IsEnabled = False
        bOutputFolder.IsEnabled = False
        cbEnableBackupSubfolders.IsEnabled = False
        cbDiscardQCloudFiles.IsEnabled = False

        ' Starts timer
        myTimer.Start()

        lbLog.Items.Insert(0, getCurrentLogDate() & "START monitoring RAW files at " & monitoredfolder)

    End Sub

    ' File manager 
    Private Sub filesManager(myObject As Object, myEventArgs As EventArgs)
        Dispatcher.Invoke(Sub()

                              myTimer.Stop()
                              Dim foundFiles As New ArrayList(Directory.GetFiles(monitoredfolder, "*.raw"))
                              If foundFiles.Count Then

                                  Dim notInBlackListFiles As ArrayList = getFilesNotInBlackList(foundFiles)
                                  Dim cleanFilesList As ArrayList = getCleanFileList(notInBlackListFiles)

                                  If cleanFilesList.Count Then

                                      stopped = False
                                      If Not bw.IsBusy = True Then

                                          Dim filenameToUpload As String = cleanFilesList.Item(0)
                                          lbLog.Items.Insert(0, getCurrentLogDate() & "Processing file..." & filenameToUpload)
                                          Dim yymm As String = getCurrentMonthFolder()
                                          Dim cb_folder As String = ""
                                          If cbSubFolder1.SelectedIndex <> -1 Then
                                              cb_folder = createBackupFolder(cbSubFolder1.SelectedValue.ToString(), cb_folder, rawFileInstrumentSerial, rawFileInstrumentName, yymm)
                                          End If
                                          If cbSubFolder2.SelectedIndex <> -1 Then
                                              cb_folder = createBackupFolder(cbSubFolder2.SelectedValue.ToString(), cb_folder, rawFileInstrumentSerial, rawFileInstrumentName, yymm)
                                          End If
                                          If cbSubFolder3.SelectedIndex <> -1 Then
                                              cb_folder = createBackupFolder(cbSubFolder3.SelectedValue.ToString(), cb_folder, rawFileInstrumentSerial, rawFileInstrumentName, yymm)
                                          End If

                                          bw.RunWorkerAsync(New String() {Path.GetFileName(filenameToUpload), filenameToUpload, tbxOutputRootFolder.Text & cb_folder}) '<---Move file

                                      End If

                                  Else
                                      myTimer.Start()
                                  End If
                              Else
                                  myTimer.Start()
                              End If


                          End Sub)
    End Sub

    Private Sub bCopyLogToClipboard_Click(sender As Object, e As RoutedEventArgs) Handles bCopyLogToClipboard.Click

        Dim clipboardText As String = ""

        For Each item In lbLog.Items
            clipboardText = clipboardText & item.ToString & Environment.NewLine
        Next

        Clipboard.SetText(clipboardText)

    End Sub

    Private Function createBackupFolder(selected As String, cb_folder As String, serial As String, name As String, yymm As String) As String

        If selected Is "Serial Number" Then
                cb_folder = cb_folder + "\" + serial
            ElseIf selected Is "Instrument Name" Then
                cb_folder = cb_folder + "\" + name
            ElseIf selected Is "YYMM" Then
                cb_folder = cb_folder + "\" + yymm
            End If

        Return cb_folder

    End Function

    Private Function IsFileInUse(filename As String) As Boolean
        Dim Locked As Boolean = False
        Try
            'Open the file in a try block in exclusive mode.  
            'If the file is in use, it will throw an IOException. 
            Dim fs As FileStream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
            fs.Close()
            ' If an exception is caught, it means that the file is in Use 
        Catch ex As IOException
            Locked = True
        End Try
        Return Locked
    End Function

End Class