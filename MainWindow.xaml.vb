Imports System.ComponentModel
Imports System.IO
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


    Public Sub New()

        InitializeComponent()

        bw.WorkerSupportsCancellation = True
        'AddHandler myTimer.Elapsed, AddressOf filesManager
        'AddHandler bw.DoWork, AddressOf bw_DoWork
        'AddHandler bw.RunWorkerCompleted, AddressOf bw_RunWorkerCompleted

        'If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Handlers added (filesManager, DoWork And RunWorkerCompleted.")

        '1. Starting configuration: 
        '1.1 Folders configuration: 
        If debugMode Then tbxInputFolder.Text = "C:\Xcalibur\data"
        If debugMode Then tbxOutputRootFolder.Text = "C:\rolivella\mydata\instruments-sim"
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

    'Private Function checkNetworkConn() As Boolean
    '    If Not My.Computer.Network.IsAvailable Then
    '        Return False
    '    End If
    '    Return True
    'End Function

    'Private Sub checkFolderPermissions(folder As String)
    '    Dim fileSec = System.IO.File.GetAccessControl(monitoredFolder)
    '    Dim accessRules = fileSec.GetAccessRules(True, True, GetType(System.Security.Principal.NTAccount))
    '    For Each rule As System.Security.AccessControl.FileSystemAccessRule In accessRules
    '        If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Identity Reference: " & rule.IdentityReference.Value)
    '        If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Access Control Type: " & rule.AccessControlType.ToString())
    '        If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] File System Rights: " & rule.FileSystemRights.ToString())
    '        Exit For
    '    Next
    'End Sub

    'Private Function getCurrentLogDate() As String
    '    Dim currentDate As DateTime = DateTime.Now
    '    Dim currentMonthFolder As String
    '    currentMonthFolder = Format$(currentDate, "yyyy-MM-dd HH:mm:ss")
    '    Return "[" & currentMonthFolder & "] "
    'End Function

    'Private Function getCurrentMonthFolder() As String
    '    Dim currentDate As DateTime = DateTime.Now
    '    Dim currentMonthFolder As String
    '    currentMonthFolder = Format$(currentDate, "yyMM")
    '    Return currentMonthFolder
    'End Function

    '' Uploads file to SFTP
    'Private Function copyLocalFileToSFTP(user As String, password As String, localFile As String, Database As String, AgendoID As String, RawClient As String) As ArrayList

    '    Dim localFileInfo As New FileInfo(localFile)
    '    Dim localFileName As String = Path.GetFileName(localFile)
    '    Dim extension As String = Path.GetExtension(localFile)
    '    Dim targetFolder = rootSFTPdataFolder + sftp_output_folder
    '    Dim targetPath As String = targetFolder + "/" + Path.GetFileNameWithoutExtension(localFile) & extension
    '    Dim scratchPath As String = scratchFolder + "/" + Path.GetFileNameWithoutExtension(localFile) & extension
    '    Dim isUploadStorageOK As Boolean = True
    '    Dim isUploadQSampleOK As Boolean = True
    '    Dim isEverythingOK As Boolean = True
    '    Dim uploadStorageCode As String = "ST-OK"
    '    Dim uploadQSampleCode As String = "QS-OK"
    '    Dim output As New ArrayList()

    '    'Connect to SFTP Server: 
    '    Try

    '        localPathToUploadFile = localFile

    '        Dim client As SftpClient = New SftpClient(SFTPaddressString, user, password)
    '        client.Connect()

    '        'Upload file when applies
    '        If Not stopped Then
    '            Try

    '                'Create storage folders when applies: 
    '                If Not client.Exists(targetFolder) Then
    '                    CreateAllDirectories(client, sftp_output_folder)
    '                End If

    '                'Copy to storage: 
    '                Dispatcher.Invoke(Sub()
    '                                      lbLog.Items.Insert(0, getCurrentLogDate() & "Uploading to Storage...Details: file " & Path.GetFileName(localPathToUploadFile) & " to folder " & targetFolder & "...")
    '                                  End Sub)

    '                Using stream As Stream = File.OpenRead(localPathToUploadFile)
    '                    'Upload to /data: 
    '                    If client.Exists(targetFolder) Then
    '                        If Not client.Exists(targetPath) Then
    '                            client.UploadFile(stream, targetPath & ".filepart") '<------------------------------UPLOADS FILE TO /DATA<------------------------------>
    '                            client.RenameFile(targetPath & ".filepart", targetPath)
    '                        Else
    '                            uploadStorageCode = "UC-ST-EX"
    '                        End If
    '                    Else

    '                    End If
    '                End Using

    '                'Copy to QSample: 
    '                Dispatcher.Invoke(Sub()
    '                                      lbLog.Items.Insert(0, getCurrentLogDate() & "Uploading to QSample...Details: file " & Path.GetFileName(localPathToUploadFile) & " to folder " & scratchFolder & "...")
    '                                  End Sub)

    '                Using stream As Stream = File.OpenRead(localPathToUploadFile)
    '                    'Upload to Qsample: 
    '                    If client.Exists(scratchFolder) Then 'Upload to QSample:
    '                        If Not client.Exists(scratchPath) Then
    '                            If Database <> "" And AgendoID <> "" And RawClient <> "" Then
    '                                client.UploadFile(stream, scratchPath & ".filepart") '<------------------------------UPLOADS FILE TO QSAMPLE<------------------------------>
    '                                client.RenameFile(scratchPath & ".filepart", scratchPath & "." & Database)
    '                            Else
    '                                uploadQSampleCode = "UC-QS-9606"
    '                            End If
    '                        Else
    '                            uploadQSampleCode = "UC-QS-EX"
    '                        End If
    '                    End If
    '                End Using


    '                'wetlab
    '                Using stream As Stream = File.OpenRead(localPathToUploadFile)
    '                    'Upload to Qsample: 
    '                    If client.Exists(scratchFolder) Then 'Upload to QSample:
    '                        If Not client.Exists(scratchPath) Then
    '                            If Database <> "" And (RawClient = "QCGV" Or RawClient = "QCDV" Or RawClient = "QCFV" Or RawClient = "QCPV " Or RawClient = "QCRP") Then
    '                                client.UploadFile(stream, scratchPath & ".filepart") '<------------------------------UPLOADS FILE TO QSAMPLE<------------------------------>
    '                                client.RenameFile(scratchPath & ".filepart", scratchPath & "." & Database)
    '                                uploadQSampleCode = "QS-OK"
    '                            Else
    '                                uploadQSampleCode = "UC-QS-9606"
    '                            End If
    '                        Else
    '                            uploadQSampleCode = "UC-QS-EX"
    '                        End If
    '                    End If
    '                End Using

    '            Catch ex As Exception
    '                If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] SFTP Upload exception: " & ex.Message)
    '                uploadStorageCode = "UC-SFTP-GEN"
    '                uploadQSampleCode = "UC-SFTP-GEN"
    '            End Try

    '        End If

    '    Catch ex As Exception
    '        If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] SFTP server connection exception: " & ex.Message)
    '        uploadStorageCode = "UC-SFTP-CONN"
    '        uploadQSampleCode = "UC-SFTP-CONN"
    '    End Try

    '    output.Add(uploadStorageCode)
    '    output.Add(uploadQSampleCode)

    '    Return output

    'End Function

    'Public Sub CreateAllDirectories(ByVal client As SftpClient, ByVal path As String)

    '    ' Consistent forward slashes
    '    path = path.Replace("\", "/")
    '    client.ChangeDirectory(rootSFTPdataFolder)

    '    For Each dir As String In path.Split("/"c)

    '        ' Ignoring leading/ending/multiple slashes
    '        If Not String.IsNullOrWhiteSpace(dir) Then

    '            If Not client.Exists(dir) Then
    '                client.CreateDirectory(dir)
    '            End If

    '            client.ChangeDirectory(dir)
    '        End If
    '    Next

    '    ' Going back to default directory
    '    client.ChangeDirectory("/")
    'End Sub

    'Private Function moveFileToProcessedFolder(fileToMove As String, processedTargetFilename As String, processedFolder As String) As Boolean
    '    Try
    '        If (Not System.IO.Directory.Exists(processedFolder)) Then
    '            System.IO.Directory.CreateDirectory(processedFolder)
    '        End If
    '        If File.Exists(processedTargetFilename) Then
    '            File.Delete(processedTargetFilename)
    '        End If
    '        File.Move(fileToMove, processedTargetFilename)
    '    Catch ex As Exception
    '        Console.Write(ex.Message)
    '        Return False
    '    End Try
    '    Return True
    'End Function

    'Private Sub showRecurrentErrorMessage(message As String, errorCode As String)

    '    Dim outputMessage As String = ""

    '    If errorCode Is "ERR01" And Not flagERR01 Then
    '        outputMessage = message
    '        lbLog.Items.Insert(0, outputMessage)
    '        flagERR01 = True
    '    End If

    'End Sub

    'Private Sub initializeFlags()
    '    flagERR01 = False
    'End Sub

    'Private Function getFilesNotInBlackList(fileslist As ArrayList) As ArrayList
    '    Dim output As New ArrayList()
    '    For Each file In fileslist
    '        If BlackListOfFiles.IndexOf(Path.GetFileName(file)) = -1 Then
    '            output.Add(file)
    '        End If
    '    Next
    '    Return output
    'End Function

    'Private Function getCleanFileList(fileslist As ArrayList) As ArrayList
    '    notInUseListOfFiles.Clear()
    '    For Each file In fileslist
    '        If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Checking file state: " & file)
    '        If IsFileInUse(file) Then
    '            notInUseListOfFiles.Remove(file)
    '            If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] File in use: " & file)
    '        Else
    '            Dim filereader As System.IO.FileInfo = My.Computer.FileSystem.GetFileInfo(file)
    '            Dim rawFile As IRawDataPlus = RawFileReaderAdapter.FileFactory(file) 'Load RAW file with Thermo lib
    '            Try
    '                rawFile.SelectInstrument(instrumentType:=0, 1)
    '                If filereader.Length > MinFileSize Then ' Check that the file has a minimum size
    '                    Dim sampleType As String = rawFile.SampleInformation.SampleType.ToString
    '                    Dim client As String = rawFile.SampleInformation.UserText.GetValue(1)
    '                    Dim database As String = rawFile.SampleInformation.UserText.GetValue(4)
    '                    If sampleType = "QC" And (client = "QC01" Or client = "QC02" Or client = "QC03") Then ' Does not includes QCrawler files
    '                        If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] QCrawler file so it is not included to the upload." & file)
    '                    ElseIf database.ToLower = "undefined" Or database.ToLower = "na" Or database.ToLower = "n/a" Then
    '                        If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Undefined or N/A so it is not included to the upload." & file)
    '                    Else
    '                        notInUseListOfFiles.Add(file)
    '                    End If
    '                ElseIf filereader.Length <= MinFileSize Then
    '                    notInUseListOfFiles.Remove(file)
    '                End If
    '            Catch ex As Exception
    '                If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Instrument index not available for requested device" & vbCrLf & "Parameter name: instrumentIndex" & ex.Message)
    '                notInUseListOfFiles.Remove(file)
    '            End Try

    '            rawFile.Dispose()
    '        End If
    '    Next
    '    If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Number of clean files: " & notInUseListOfFiles.Count)
    '    Return notInUseListOfFiles
    'End Function

    'Private Sub bw_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)
    '    Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)
    '    Dim workerInputs As String() = e.Argument
    '    uploadResultCode = copyLocalFileToSFTP(workerInputs(0), workerInputs(1), workerInputs(2), workerInputs(3), workerInputs(4), workerInputs(5))
    '    e.Result = localPathToUploadFile
    'End Sub

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

    'Private Sub bStopSync_Click(sender As Object, e As RoutedEventArgs) Handles bStopSync.Click
    '    If bw.WorkerSupportsCancellation = True Then
    '        bw.CancelAsync()
    '    End If
    '    bStartSync.IsEnabled = True
    '    bStopSync.IsEnabled = False
    '    myTimer.Stop()
    '    stopped = True
    '    lbLog.Items.Insert(0, getCurrentLogDate() & "STOP monitoring RAW files at " & monitoredFolder)
    'End Sub

    'Private Sub bClearLog_Click(sender As Object, e As RoutedEventArgs) Handles bClearLog.Click
    '    lbLog.Items.Clear()
    'End Sub

    'Private Sub bBrowseAcquisitionFolder_Click(sender As Object, e As RoutedEventArgs) Handles bBrowseAcquisitionFolder.Click
    '    Dim dialog As New FolderBrowserDialog()
    '    dialog.RootFolder = Environment.SpecialFolder.Desktop
    '    dialog.SelectedPath = "C: \"
    '    dialog.Description = "Select Application Configuration Files Path"
    '    If dialog.ShowDialog() = Windows.Forms.DialogResult.OK Then
    '        tbMonitoredFolder.IsEnabled = False
    '        bBrowseAcquisitionFolder.IsEnabled = False
    '        bStartSync.IsEnabled = True
    '        bStopSync.IsEnabled = False
    '        tbMonitoredFolder.Text = dialog.SelectedPath.ToString
    '        myTimer.Interval = min_interval
    '    End If
    'End Sub

    'Private Sub cleanListBox()
    '    If lbLog.Items.Count > 0 Then
    '        lbLog.SelectedIndex = lbLog.Items.Count - 1
    '        lbLog.Items.RemoveAt(lbLog.SelectedIndex)
    '    End If
    'End Sub

    'Private Sub bStartSync_Click(sender As Object, e As RoutedEventArgs) Handles bStartSync.Click

    '    ' Initialize variables: 
    '    processedFolderString = "processed"
    '    monitoredFolder = tbMonitoredFolder.Text
    '    'monitoredFolder = "C:\rolivella\XCalibur"
    '    BlackListOfFiles.Add("")
    '    bStartSync.IsEnabled = False
    '    bStopSync.IsEnabled = True
    '    bCopyLogToClipboard.IsEnabled = True
    '    bClearLog.IsEnabled = True

    '    ' Sets the timer interval (millisec).
    '    myTimer.Start()

    '    lbLog.Items.Insert(0, getCurrentLogDate() & "START monitoring RAW files at " & monitoredFolder)

    '    If debugMode Then checkFolderPermissions(monitoredFolder)

    'End Sub

    '' FILE MANAGER -----------------------> 
    'Private Sub filesManager(myObject As Object, myEventArgs As EventArgs)
    '    Dispatcher.Invoke(Sub()
    '                          If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Checking monitored local folder...")
    '                          If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] filesManager started.")
    '                          myTimer.Stop()
    '                          If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] myTimer stopped.")
    '                          If checkNetworkConn() Then
    '                              networkErrorMessageCounter = 0
    '                              If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Network connection OK.")
    '                              If IO.Directory.Exists(monitoredFolder) Then 'Check if local folder exists.
    '                                  If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Monitored folder OK.")
    '                                  'Check if the "processed" folder exists. If not, create it. 
    '                                  Dim processedFolderTarget As String = monitoredFolder & "\" & processedFolderString
    '                                  If IO.Directory.Exists(processedFolderTarget) Then
    '                                      Dim foundFiles As New ArrayList(Directory.GetFiles(monitoredFolder, "*.raw"))
    '                                      If foundFiles.Count Then
    '                                          If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Found files to process.")
    '                                          Dim notInBlackListFiles As ArrayList = getFilesNotInBlackList(foundFiles)
    '                                          Dim cleanFilesList As ArrayList = getCleanFileList(notInBlackListFiles)
    '                                          If cleanFilesList.Count Then
    '                                              stopped = False
    '                                              If Not bw.IsBusy = True Then
    '                                                  If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] Running worker to upload the file to SFTP...")
    '                                                  Dim filenameToUpload As String = cleanFilesList.Item(0)
    '                                                  Dim rawFile As IRawDataPlus = RawFileReaderAdapter.FileFactory(filenameToUpload) 'Load RAW file with Thermo lib
    '                                                  Dim rawFileClient As String = rawFile.SampleInformation.UserText.GetValue(1)
    '                                                  Dim rawFileAgendoID As String = rawFile.SampleInformation.UserText.GetValue(3)
    '                                                  Dim rawFileDatabase As String = rawFile.SampleInformation.UserText.GetValue(4)
    '                                                  If Not rawFile.IsError Then
    '                                                      If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] File IS NOT in ERROR state: " & filenameToUpload)
    '                                                      'Check if remote output folder exists. If not, create it. 
    '                                                      rawFile.SelectInstrument(instrumentType:=0, 1)
    '                                                      'Dim instrumentFolder As String = rawFile.GetInstrumentData().SerialNumber
    '                                                      'sftp_output_folder = "/" + rawFile.GetInstrumentData().Model.ToString.Replace(" ", "_").ToLower + "_" + instrumentFolder + "/raw/" + getCurrentMonthFolder() + "/" + rawFile.SampleInformation.UserText.GetValue(1) 'Storage structure: instrument_serialnumber/raw/ + /YYMM/ + /client
    '                                                      Dim instrumentFolder As String = cbInstruments.SelectedItem.ToString
    '                                                      sftp_output_folder = "/" + instrumentFolder + "/Raw/" + getCurrentMonthFolder() + "/" + rawFileClient 'Storage structure: instrument_serialnumber/raw/ + /YYMM/ + /client
    '                                                      rawFile.Dispose() '------>Close rawFile by Thermo lib
    '                                                      If FileLen(filenameToUpload) <= MaxFileSize Then 'Only filesize less or equal than 2GB
    '                                                          'UPLOAD file to FTP:
    '                                                          bw.RunWorkerAsync(New String() {SFTPuserString, SFTPpasswordString, filenameToUpload, rawFileDatabase, rawFileAgendoID, rawFileClient})
    '                                                      Else
    '                                                          lbLog.Items.Insert(0, getCurrentLogDate() & "[WARNING] The file " & filenameToUpload & " is greater than 2GB so it won't be uploaded.")
    '                                                          myTimer.Start()
    '                                                      End If
    '                                                  Else
    '                                                      myTimer.Start()
    '                                                  End If
    '                                              End If
    '                                          Else
    '                                              myTimer.Start()
    '                                          End If
    '                                      Else
    '                                          If debugMode Then lbLog.Items.Insert(0, getCurrentLogDate() & "[DEBUG MODE] No files to process.")
    '                                          myTimer.Start() 'No files to process.
    '                                      End If
    '                                  Else
    '                                      System.IO.Directory.CreateDirectory(processedFolderTarget)
    '                                      lbLog.Items.Insert(0, getCurrentLogDate() & processedFolderTarget & " folder created")
    '                                      myTimer.Start()
    '                                  End If
    '                              Else
    '                                  lbLog.Items.Insert(0, getCurrentLogDate() & "[ERROR] Local folder " & monitoredFolder & " not found. Please check.")
    '                                  myTimer.Start()
    '                              End If
    '                          Else
    '                              If networkErrorMessageCounter < 3 Then
    '                                  lbLog.Items.Insert(0, getCurrentLogDate() & "[ERROR] Network connection not available. Please check.")
    '                                  networkErrorMessageCounter = networkErrorMessageCounter + 1
    '                              End If
    '                              myTimer.Start()
    '                          End If
    '                      End Sub)
    'End Sub

    'Private Sub bCopyLogToClipboard_Click(sender As Object, e As RoutedEventArgs) Handles bCopyLogToClipboard.Click

    '    Dim clipboardText As String = ""

    '    For Each item In lbLog.Items
    '        clipboardText = clipboardText & item.ToString & Environment.NewLine
    '    Next

    '    Clipboard.SetText(clipboardText)

    'End Sub

    'Private Function IsFileInUse(filename As String) As Boolean
    '    Dim Locked As Boolean = False
    '    Try
    '        'Open the file in a try block in exclusive mode.  
    '        'If the file is in use, it will throw an IOException. 
    '        Dim fs As FileStream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
    '        fs.Close()
    '        ' If an exception is caught, it means that the file is in Use 
    '    Catch ex As IOException
    '        Locked = True
    '    End Try
    '    Return Locked
    'End Function

End Class