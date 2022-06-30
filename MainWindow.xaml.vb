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
        'AddHandler bw.RunWorkerCompleted, AddressOf bw_RunWorkerCompleted

        If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Handlers added (filesManager, DoWork And RunWorkerCompleted.")

        '1. Starting configuration: 
        '1.1 Folders configuration: 
        If debugMode Then tbxInputFolder.Text = "C:\rolivella\XCalibur\data"
        If debugMode Then tbxOutputRootFolder.Text = "C:\rolivella\XCalibur\backup"
        If debugMode Then tbxOutputFolderSummary.Text = "C:\rolivella\XCalibur\backup"
        cbSubFolder1.Items.Add("Instrument Name")
        cbSubFolder1.Items.Add("Serial Number")
        cbSubFolder1.Items.Add("YYMM")
        cbSubFolder1.Items.Add("Method Name")
        cbSubFolder2.Items.Add("Instrument Name")
        cbSubFolder2.Items.Add("Serial Number")
        cbSubFolder2.Items.Add("YYMM")
        cbSubFolder2.Items.Add("Method Name")
        cbSubFolder3.Items.Add("Instrument Name")
        cbSubFolder3.Items.Add("Serial Number")
        cbSubFolder3.Items.Add("YYMM")
        cbSubFolder3.Items.Add("Method Name")
        cbSubFolder4.Items.Add("Instrument Name")
        cbSubFolder4.Items.Add("Serial Number")
        cbSubFolder4.Items.Add("YYMM")
        cbSubFolder4.Items.Add("Method Name")
        cbSubFolder1.IsEnabled = False
        cbSubFolder2.IsEnabled = False
        cbSubFolder3.IsEnabled = False
        cbSubFolder4.IsEnabled = False
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

    Private Sub cbSubFolder4_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles cbSubFolder4.SelectionChanged
        tbxOutputFolderSummary.Text = tbxOutputRootFolder.Text & "\" & cbSubFolder1.SelectedItem & "\" & cbSubFolder2.SelectedItem & "\" & cbSubFolder3.SelectedItem & "\" & cbSubFolder4.SelectedItem
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
            cbSubFolder4.IsEnabled = True
        End If

    End Sub

    Private Sub cbEnableBackupSubfolders_Unchecked(sender As Object, e As RoutedEventArgs) Handles cbEnableBackupSubfolders.Unchecked

        If cbEnableBackupSubfolders.IsChecked = False Then
            cbSubFolder1.IsEnabled = False
            cbSubFolder2.IsEnabled = False
            cbSubFolder3.IsEnabled = False
            cbSubFolder4.IsEnabled = False
            cbSubFolder1.Text = ""
            cbSubFolder2.Text = ""
            cbSubFolder3.Text = ""
            cbSubFolder4.Text = ""
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

    ' Move file to backup folder
    Private Function movetobackupfolder(source As String, target As String) As ArrayList

        Dim movetobackupcode As String = "mv-ok"
        Dim output As New ArrayList()

        Try
            If File.Exists(source) Then
                movetobackupcode = "mv-file-exist"
            Else
                File.Move(source, target)
            End If
        Catch ex As Exception
            Console.Write(ex.Message)
            movetobackupcode = "mv-file-exception"
        End Try

        output.Add(movetobackupcode)

        Return output

    End Function

    Private Function moveFileToProcessedFolder(fileToMove As String, processedTargetFilename As String, processedFolder As String) As Boolean
        Try
            If (Not System.IO.Directory.Exists(processedFolder)) Then
                System.IO.Directory.CreateDirectory(processedFolder)
            End If
            If File.Exists(processedTargetFilename) Then
                File.Delete(processedTargetFilename)
            End If
            File.Move(fileToMove, processedTargetFilename)
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
        uploadresultcode = movetobackupfolder(workerInputs(0), workerInputs(1))
        e.Result = localpathtouploadfile
    End Sub

    'Private Sub bw_RunWorkerCompleted(ByVal sender As Object, ByVal e As RunWorkerCompletedEventArgs)

    '    If Not stopped Then

    '        Dim uploadResultCodeStorage = uploadResultCode.Item(0)
    '        Dim uploadResultCodeQSample = uploadResultCode.Item(1)
    '        Dim isMovedProcessed As Boolean = False

    '        '--------------OK:

    '        ' If Upload to storage OK:
    '        If uploadResultCodeStorage Is "ST-OK" Then
    '            lbLog.Items.Insert(0, getCurrentLogDate() & ":) File " & Path.GetFileName(e.Result) & " uploaded to Storage!")
    '            initializeFlags()
    '            If Not isMovedProcessed Then
    '                Dim processedTargetFolder As String = Path.GetDirectoryName(e.Result) & "\" & processedFolderString & "\" & getCurrentMonthFolder()
    '                'Move files to processed folder
    '                If moveFileToProcessedFolder(Path.GetDirectoryName(e.Result) & "\" & Path.GetFileName(e.Result), processedTargetFolder & "\" & Path.GetFileName(e.Result), processedTargetFolder) Then
    '                    lbLog.Items.Insert(0, getCurrentLogDate() & ":) File " & e.Result & " moved to processed folder")
    '                    isMovedProcessed = True
    '                Else
    '                    lbLog.Items.Insert(0, getCurrentLogDate() & "[ERROR] File " & e.Result & " cannot be moved to processed folder. Please check.")
    '                End If
    '            End If
    '        End If

    '        If uploadResultCodeQSample Is "QS-OK" Then
    '            lbLog.Items.Insert(0, getCurrentLogDate() & ":) File " & Path.GetFileName(e.Result) & " uploaded to QSample!")
    '            initializeFlags()
    '            If Not isMovedProcessed Then
    '                Dim processedTargetFolder As String = Path.GetDirectoryName(e.Result) & "\" & processedFolderString & "\" & getCurrentMonthFolder()
    '                'Move files to processed folder
    '                If moveFileToProcessedFolder(Path.GetDirectoryName(e.Result) & "\" & Path.GetFileName(e.Result), processedTargetFolder & "\" & Path.GetFileName(e.Result), processedTargetFolder) Then
    '                    lbLog.Items.Insert(0, getCurrentLogDate() & ":) File " & e.Result & " moved to processed folder")
    '                    isMovedProcessed = True
    '                Else
    '                    lbLog.Items.Insert(0, getCurrentLogDate() & "[ERROR] File " & e.Result & " cannot be moved to processed folder. Please check.")
    '                End If
    '            End If
    '        End If

    '        '--------------ERR:

    '        ' If Upload to storage interrupted. Reason: file already exists
    '        If uploadResultCodeStorage Is "UC-ST-EX" Then
    '            BlackListOfFiles.Add(Path.GetFileName(e.Result))
    '            lbLog.Items.Insert(0, getCurrentLogDate() & "[WARNING] Upload to Storage failed! Reason: file " & Path.GetFileName(e.Result) & " already exists.")
    '            If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Upload to storage failed! Reason: file " & Path.GetFileName(e.Result) & " already exists.")
    '        End If

    '        ' If Upload to QSample interrupted. Reason: file already exists
    '        If uploadResultCodeQSample Is "UC-QS-EX" Then
    '            BlackListOfFiles.Add(Path.GetFileName(e.Result))
    '            lbLog.Items.Insert(0, getCurrentLogDate() & "[WARNING] File not uploaded to QSample. Reason: file " & Path.GetFileName(e.Result) & " already exists.")
    '            If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Upload to QSample failed! Reason: file " & Path.GetFileName(e.Result) & " already exists.")
    '        End If

    '        ' If Upload to QSample interrupted. Reason: file is not 9606
    '        If uploadResultCodeQSample Is "UC-QS-9606" Then
    '            BlackListOfFiles.Add(Path.GetFileName(e.Result))
    '            lbLog.Items.Insert(0, getCurrentLogDate() & "[WARNING] File not uploaded to QSample. Reason: file " & Path.GetFileName(e.Result) & " has not Database field informed.")
    '            If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] File not uploaded to QSample. Reason: file " & Path.GetFileName(e.Result) & " has not Database field informed.")
    '        End If

    '        ' If everything went wrong. Reason: general error. 
    '        If uploadResultCodeStorage Is "UC-SFTP-GEN" Or uploadResultCodeQSample Is "UC-SFTP-GEN" Then
    '            BlackListOfFiles.Add(Path.GetFileName(e.Result))
    '            lbLog.Items.Insert(0, getCurrentLogDate() & "[ERROR] General upload error! Reason: file " & Path.GetFileName(e.Result) & " could not be uploaded because of a general error.")
    '            If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Upload error! Reason: file " & Path.GetFileName(e.Result) & " could not be uploaded because of a general error.")
    '        End If

    '        ' If everything went wrong. Reason: connection error. 
    '        If uploadResultCodeStorage Is "UC-SFTP-CONN" Or uploadResultCodeQSample Is "UC-SFTP-CONN" Then
    '            BlackListOfFiles.Add(Path.GetFileName(e.Result))
    '            lbLog.Items.Insert(0, getCurrentLogDate() & "[ERROR] General upload error! Reason: file " & Path.GetFileName(e.Result) & " could not be uploaded because of a connectivity error.")
    '            If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Upload error! Reason: file " & Path.GetFileName(e.Result) & " could not be uploaded because of a connectivity error.")
    '        End If

    '    End If

    '    myTimer.Start()

    'End Sub

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

    'Private Sub cleanListBox()
    '    If lbLog.Items.Count > 0 Then
    '        lbLog.SelectedIndex = lbLog.Items.Count - 1
    '        lbLog.Items.RemoveAt(lbLog.SelectedIndex)
    '    End If
    'End Sub

    Private Sub bStartSync_Click(sender As Object, e As RoutedEventArgs) Handles bStartSync.Click

        monitoredfolder = tbxInputFolder.Text

        ' Initialize variables: 
        processedfolderstring = "processed"
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
                              If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Checking monitored local folder...")
                              If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] filesManager started.")
                              myTimer.Stop()
                              If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] myTimer stopped.")
                              If checkNetworkConn() Then
                                  networkErrorMessageCounter = 0
                                  If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Network connection OK.")
                                  If IO.Directory.Exists(monitoredfolder) Then 'Check if local folder exists.
                                      If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Monitored folder OK.")
                                      'Check if the "processed" folder exists. If not, create it. 
                                      Dim processedFolderTarget As String = monitoredfolder & "\" & processedfolderstring
                                      If IO.Directory.Exists(processedFolderTarget) Then
                                          Dim foundFiles As New ArrayList(Directory.GetFiles(monitoredfolder, "*.raw"))
                                          If foundFiles.Count Then
                                              If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Found files to process.")
                                              Dim notInBlackListFiles As ArrayList = getFilesNotInBlackList(foundFiles)
                                              Dim cleanFilesList As ArrayList = getCleanFileList(notInBlackListFiles)
                                              If cleanFilesList.Count Then
                                                  stopped = False
                                                  If Not bw.IsBusy = True Then
                                                      If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Running worker to upload the file to SFTP...")
                                                      Dim filenameToUpload As String = cleanFilesList.Item(0)
                                                      Dim rawFile As IRawDataPlus = RawFileReaderAdapter.FileFactory(filenameToUpload) 'Load RAW file with Thermo lib
                                                      If Not rawFile.IsError Then
                                                          If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] File IS NOT in ERROR state: " & filenameToUpload)
                                                          rawFile.SelectInstrument(instrumentType:=0, 1)
                                                          Dim serial As String = rawFile.GetInstrumentData().SerialNumber
                                                          Dim model As String = rawFile.GetInstrumentData().Model
                                                          Dim name As String = rawFile.GetInstrumentData().Name
                                                          Dim userlabel As String = rawFile.UserLabel.ToString
                                                          Dim friendlyname As String = rawFile.GetAllInstrumentFriendlyNamesFromInstrumentMethod().ToString
                                                          Dim method As String = rawFile.GetInstrumentMethod(0).ToString
                                                          'sftp_output_folder = "/" + rawFile.GetInstrumentData().Model.ToString.Replace(" ", "_").ToLower + "_" + instrumentFolder + "/raw/" + getCurrentMonthFolder() + "/" + rawFile.SampleInformation.UserText.GetValue(1) 'Storage structure: instrument_serialnumber/raw/ + /YYMM/ + /client
                                                          'Dim instrumentFolder As String = cbInstruments.SelectedItem.ToString
                                                          'sftp_output_folder = "/" + instrumentFolder + "/Raw/" + getCurrentMonthFolder() + "/" + rawFileClient 'Storage structure: instrument_serialnumber/raw/ + /YYMM/ + /client
                                                          rawFile.Dispose() '------>Close rawFile by Thermo lib
                                                          If FileLen(filenameToUpload) <= MaxFileSize Then 'Only filesize less or equal than 2GB
                                                              'UPLOAD file to FTP:
                                                              'bw.RunWorkerAsync(New String() {SFTPuserString, SFTPpasswordString, filenameToUpload, rawFileDatabase, rawFileAgendoID, rawFileClient})
                                                          Else
                                                              lbLog.Items.Insert(0, getCurrentLogDate() & "[WARNING] The file " & filenameToUpload & " is greater than 2GB so it won't be uploaded.")
                                                              myTimer.Start()
                                                          End If
                                                      Else
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
                                          System.IO.Directory.CreateDirectory(processedFolderTarget)
                                          lbLog.Items.Insert(0, getCurrentLogDate() & processedFolderTarget & " folder created")
                                          myTimer.Start()
                                      End If
                                  Else
                                      '                                  lbLog.Items.Insert(0, getCurrentLogDate() & "[ERROR] Local folder " & monitoredFolder & " not found. Please check.")
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