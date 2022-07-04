Imports System.ComponentModel
Imports System.IO
Imports System.Windows.Forms
Imports ThermoFisher.CommonCore.Data.Interfaces
Imports ThermoFisher.CommonCore.RawFileReader

Class MainWindow

    'Hardcodes: 
    Dim min_interval As String = "300000" 'millisec
    Dim MinFileSize As Long = 500000 '0.5 MB
    Dim MaxFileSize As Long = 10000000000 '10 GB

    'Declarations and definitions:
    Dim debugMode As Boolean = True '--------------DEBUG
    Private Shared myTimer As New Timers.Timer
    Dim bw As BackgroundWorker = New BackgroundWorker
    Dim flagerr01 As Boolean = False
    Dim uplodadresult As Boolean = False
    Dim uploadresultcode As New ArrayList()
    Dim stopped As Boolean = False
    Dim localpathtouploadfile As String = ""
    Dim monitoredfolder As String = ""
    Dim processedfolderstring As String = ""
    Private Shared BlackListOfFiles As New ArrayList()
    Private Shared notInUseListOfFiles As New ArrayList()
    Dim networkErrorMessageCounter As Integer = 0
    Dim fileChecksum As String = ""
    Dim excludeQCloudFile As Boolean = False


    Public Sub New()

        InitializeComponent()

        bw.WorkerSupportsCancellation = True
        AddHandler myTimer.Elapsed, AddressOf filesManager
        AddHandler bw.DoWork, AddressOf bw_DoWork
        AddHandler bw.RunWorkerCompleted, AddressOf bw_RunWorkerCompleted

        If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Handlers added (filesManager, DoWork And RunWorkerCompleted.")

        If debugMode Then tbxInputFolder.Text = "C:\rolivella\XCalibur\data"
        If debugMode Then tbxOutputRootFolder.Text = "C:\rolivella\XCalibur\backup"
        If debugMode Then tbxOutputFolderSummary.Text = "C:\rolivella\XCalibur\backup"
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
        cbDiscardQCloudFiles.IsChecked = True
        tbxInputFolderSummary.IsEnabled = False
        tbxOutputFolderSummary.IsEnabled = False
        tbxInputFolder.IsEnabled = False
        tbxOutputRootFolder.IsEnabled = False
        tbxInputFolderSummary.Text = tbxInputFolder.Text

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

    Private Function checkNetworkConn() As Boolean
        If Not My.Computer.Network.IsAvailable Then
            Return False
        End If
        Return True
    End Function

    Private Sub checkFolderPermissions(folder As String)
        Dim fileSec = System.IO.File.GetAccessControl(monitoredfolder)
        Dim accessRules = fileSec.GetAccessRules(True, True, GetType(System.Security.Principal.NTAccount))
        For Each rule As System.Security.AccessControl.FileSystemAccessRule In accessRules
            If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Identity Reference: " & rule.IdentityReference.Value)
            If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Access Control Type: " & rule.AccessControlType.ToString())
            If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] File System Rights: " & rule.FileSystemRights.ToString())
            Exit For
        Next
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

    Private Function moveFile(OriginFullPath As String, targetFullPath As String, targetOnlyfolder As String) As Boolean
        Try
            If (Not System.IO.Directory.Exists(targetOnlyfolder)) Then
                System.IO.Directory.CreateDirectory(targetOnlyfolder)
            End If
            If File.Exists(targetFullPath) Then
                File.Delete(targetFullPath)
            End If
            File.Move(OriginFullPath, targetFullPath)
        Catch ex As Exception
            Console.Write(ex.Message)
            Return False
        End Try
        Return True
    End Function

    Private Sub showRecurrentErrorMessage(message As String, errorCode As String)

        Dim outputMessage As String = ""

        If errorCode Is "ERR01" And Not flagerr01 Then
            outputMessage = message
            lbLog.Items.Insert(0, outputMessage)
            flagerr01 = True
        End If

    End Sub

    Private Sub initializeFlags()
        flagerr01 = False
    End Sub

    Private Function getFilesNotInBlackList(fileslist As ArrayList) As ArrayList
        Dim output As New ArrayList()
        For Each file In fileslist
            If BlackListOfFiles.IndexOf(Path.GetFileName(file)) = -1 Then
                output.Add(file)
            End If
        Next
        Return output
    End Function

    Private Function getCleanFileList(fileslist As ArrayList) As ArrayList
        notInUseListOfFiles.Clear()
        For Each file In fileslist
            If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Checking file state: " & file)
            If IsFileInUse(file) Then
                notInUseListOfFiles.Remove(file)
                If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] File in use: " & file)
            Else
                Dim filereader As System.IO.FileInfo = My.Computer.FileSystem.GetFileInfo(file)
                Dim rawFile As IRawDataPlus = RawFileReaderAdapter.FileFactory(file) 'Load RAW file with Thermo lib
                Try
                    rawFile.SelectInstrument(instrumentType:=0, 1)
                    If filereader.Length > MinFileSize Then ' Check that the file has a minimum size
                        Dim sampleType As String = rawFile.SampleInformation.SampleType.ToString
                        Dim client As String = rawFile.SampleInformation.UserText.GetValue(1)
                        Dim database As String = rawFile.SampleInformation.UserText.GetValue(4)
                        If sampleType = "QC" And (client = "QC01" Or client = "QC02" Or client = "QC03") And Not excludeQCloudFile Then ' Does not includes QCrawler files
                            If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] QCrawler file so it is not included to the upload." & file)
                        ElseIf database.ToLower = "undefined" Or database.ToLower = "na" Or database.ToLower = "n/a" Then
                            If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Undefined or N/A so it is not included to the upload." & file)
                        Else
                            notInUseListOfFiles.Add(file)
                        End If
                    ElseIf filereader.Length <= MinFileSize Then
                        notInUseListOfFiles.Remove(file)
                    End If
                Catch ex As Exception
                    If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Instrument index not available for requested device" & vbCrLf & "Parameter name: instrumentIndex" & ex.Message)
                    notInUseListOfFiles.Remove(file)
                End Try

                rawFile.Dispose()
            End If
        Next
        If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Number of clean files: " & notInUseListOfFiles.Count)
        Return notInUseListOfFiles
    End Function

    Private Sub bw_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)
        Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)
        Dim workerInputs As String() = e.Argument
        uploadresultcode = moveFileToTarget(workerInputs(0), workerInputs(1), workerInputs(2))
        e.Result = localpathtouploadfile
    End Sub

    ' Backups files
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

    Private Sub bw_RunWorkerCompleted(ByVal sender As Object, ByVal e As RunWorkerCompletedEventArgs)

        If Not stopped Then

            Dim uploadResultCodeStorage = uploadresultcode.Item(0)

            If uploadResultCodeStorage Is "ST-OK" Then
                lbLog.Items.Insert(0, getCurrentLogDate() & ":) File backuped successfully to " & Path.GetDirectoryName(e.Result) & "\" & Path.GetFileName(e.Result))
                If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & ":) File backuped successfully to " & Path.GetDirectoryName(e.Result) & "\" & Path.GetFileName(e.Result))
            ElseIf uploadResultCodeStorage Is "ST-ERR" Then
                lbLog.Items.Insert(0, getCurrentLogDate() & "[ERROR] Failed to backup file " & Path.GetDirectoryName(e.Result) & "\" & Path.GetFileName(e.Result))
                If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[ERROR] Failed to backup file " & Path.GetDirectoryName(e.Result) & "\" & Path.GetFileName(e.Result))
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
        lbLog.Items.Insert(0, getCurrentLogDate() & "stop monitoring raw files at " & monitoredfolder)
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

        ' Sets the timer interval (millisec).
        myTimer.Start()

        lbLog.Items.Insert(0, getCurrentLogDate() & "START monitoring RAW files at " & monitoredfolder)

        If debugMode Then checkFolderPermissions(monitoredfolder)

    End Sub

    ' FILE MANAGER -----------------------> 
    Private Sub filesManager(myObject As Object, myEventArgs As EventArgs)
        Dispatcher.Invoke(Sub()
                              myTimer.Stop()
                              If checkNetworkConn() Then
                                  networkErrorMessageCounter = 0
                                  If IO.Directory.Exists(monitoredfolder) Then 'Check if local folder exists.
                                      Dim foundFiles As New ArrayList(Directory.GetFiles(monitoredfolder, "*.raw"))
                                      If foundFiles.Count Then
                                          If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Found files to process.")
                                          Dim notInBlackListFiles As ArrayList = getFilesNotInBlackList(foundFiles)
                                          Dim cleanFilesList As ArrayList = getCleanFileList(notInBlackListFiles)
                                          If cleanFilesList.Count Then
                                              stopped = False
                                              If Not bw.IsBusy = True Then
                                                  Dim filenameToUpload As String = cleanFilesList.Item(0)
                                                  Dim rawFile As IRawDataPlus = RawFileReaderAdapter.FileFactory(filenameToUpload) '------> Open rawFile by Thermo lib

                                                  If Not rawFile.HasMsData Then

                                                      If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] File has MS data" & filenameToUpload)
                                                      rawFile.SelectInstrument(instrumentType:=0, 1)
                                                      Dim serial As String = rawFile.GetInstrumentData().SerialNumber
                                                      Dim name As String = rawFile.GetInstrumentData().Name.ToString.Replace(" ", "_").ToLower
                                                      Dim hasMSData As Boolean = rawFile.HasMsData

                                                      Dim yymm As String = getCurrentMonthFolder()
                                                      Dim cb_folder As String = ""

                                                      If cbSubFolder1.SelectedIndex <> -1 Then
                                                          cb_folder = createBackupFolder(cbSubFolder1.SelectedValue.ToString(), cb_folder, serial, name, yymm)
                                                      ElseIf cbSubFolder2.SelectedIndex <> -1 Then
                                                          cb_folder = createBackupFolder(cbSubFolder2.SelectedValue.ToString(), cb_folder, serial, name, yymm)
                                                      ElseIf cbSubFolder3.SelectedIndex <> -1 Then
                                                          cb_folder = createBackupFolder(cbSubFolder3.SelectedValue.ToString(), cb_folder, serial, name, yymm)
                                                      End If

                                                      If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Backup target folder is: " & cb_folder)

                                                      rawFile.Dispose() '------> Close rawFile by Thermo lib

                                                      If FileLen(filenameToUpload) <= MaxFileSize Then 'Only filesize less or equal than 2GB
                                                          'BACKUP file:
                                                          bw.RunWorkerAsync(New String() {Path.GetFileName(filenameToUpload), filenameToUpload, tbxOutputRootFolder.Text & cb_folder})
                                                      Else
                                                          lbLog.Items.Insert(0, getCurrentLogDate() & "[WARNING] The file " & filenameToUpload & " is greater than 2GB so it won't be uploaded.")
                                                          myTimer.Start()
                                                      End If

                                                  Else
                                                      lbLog.Items.Insert(0, getCurrentLogDate() & "[ERROR] File " & filenameToUpload & " has not MS data.")
                                                      myTimer.Start()
                                                  End If

                                              End If
                                          Else
                                              myTimer.Start()
                                          End If
                                      Else
                                          If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] No files to process.")
                                          myTimer.Start() 'No files to process.
                                      End If
                                  Else
                                      myTimer.Start()
                                  End If
                              Else
                                  If networkErrorMessageCounter < 3 Then
                                      lbLog.Items.Insert(0, getCurrentLogDate() & "[ERROR] Network connection not available. Please check.")
                                      networkErrorMessageCounter = networkErrorMessageCounter + 1
                                  End If
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