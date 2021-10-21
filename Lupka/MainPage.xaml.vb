' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

Imports System.Net.Http
Imports Microsoft.Graphics.Canvas
Imports Windows.Devices.Enumeration
Imports Windows.Graphics.Imaging
Imports Windows.Media.Capture
Imports Windows.Media.MediaProperties
Imports Windows.Storage
Imports Windows.Storage.FileProperties
Imports Windows.Storage.Streams
Imports Windows.System.Display
''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>


Public NotInheritable Class MainPage
    Inherits Page

    Dim mbStarted As Boolean = False

    Dim moDisplayRequest As DisplayRequest
    Dim moMediaCapture As MediaCapture
    Dim msCameraId As String = ""
    Dim miObrot0 As Integer = 90
    Dim miObrot As Integer = 0

    ' True wycina wszystko co dotyczy menu ustawien - skoro Store tego nie chce, to niech nie ma.
#Const forStore = True

    Private Async Sub KamerkaStop()
        mbStarted = False
        Try
            Await moMediaCapture.StopPreviewAsync
            moDisplayRequest.RequestRelease()
            uiCamera.Source = Nothing
            moMediaCapture.Dispose()
            moDisplayRequest = Nothing
        Catch ex As Exception

        End Try
        uiTools.IsEnabled = False
        uiAppBarCamera.IsEnabled = False
        uiAppBarRotate.IsEnabled = False
        uiSettings.IsEnabled = False

    End Sub

    Private Shared ReadOnly RotationKey As Guid = New Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1")
    Private Async Function KamerkaObrot(iDegrees As Integer) As Task
        miObrot = iDegrees
        Dim props As IMediaEncodingProperties = moMediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview)
        props.Properties.Add(RotationKey, miObrot0 + iDegrees)
        Try
            Await moMediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, Nothing)
        Catch ex As Exception
            ' na wszelki wypadek - chodzi o to, zeby nie bylo crash i zielonego
        End Try
    End Function
    Private Async Sub KamerkaStart()
        mbStarted = True
        Try

            moDisplayRequest = New DisplayRequest()
            moMediaCapture = New MediaCapture()

            Dim settings As MediaCaptureInitializationSettings = New MediaCaptureInitializationSettings
            settings.VideoDeviceId = msCameraId
            settings.StreamingCaptureMode = StreamingCaptureMode.Video  ' bez μfonu

            Await moMediaCapture.InitializeAsync(settings)
            MenuUstawien()  ' bo rozne kamerki moga miec rozne!

            Dim iMaxX As Integer = 0
            Dim iMaxY As Integer = 0
            Dim oMEP As IMediaEncodingProperties = Nothing
            For Each oSP As IMediaEncodingProperties In moMediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview)
                With TryCast(oSP, VideoEncodingProperties)
                    Dim iTmp As Integer = .Width
                    If iTmp > iMaxX Then
                        oMEP = oSP
                        iMaxX = iTmp
                        iMaxY = .Height
                    End If
                End With
            Next
            If iMaxX > 0 Then Await moMediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, oMEP)
            ' Lumia 532: 640x480 oraz 800x532
            Dim dMinZoom As Double
            Select Case miObrot0
                Case 0, 180
                    dMinZoom = Math.Min(uiScroll.ActualWidth / iMaxX, uiScroll.ActualHeight / iMaxY)
                Case 90, 270
                    dMinZoom = Math.Min(uiScroll.ActualWidth / iMaxY, uiScroll.ActualHeight / iMaxX)
            End Select
            uiScroll.MinZoomFactor = dMinZoom

            moDisplayRequest.RequestActive()
            uiCamera.Source = moMediaCapture

            Await moMediaCapture.StartPreviewAsync()

            uiTools.IsEnabled = True
            uiAppBarCamera.IsEnabled = True
            uiAppBarRotate.IsEnabled = True
            uiSettings.IsEnabled = True

            Await KamerkaObrot(0)
        Catch ex As Exception
            mbStarted = False
        End Try

        If Not mbStarted Then
            App.DialogBox("Cannot start capture")
        End If
    End Sub

    Protected Overloads Sub OnNavigatedFrom(e As NavigationEventArgs)
        KamerkaStop()
        uiStart.Content = "Start"
        uiSettings.IsEnabled = False
        uiTools.IsEnabled = False
    End Sub


    Private Sub uiStart_Click(sender As Object, e As RoutedEventArgs) Handles uiStart.Click

        If mbStarted Then
            KamerkaStop()
            uiStart.Content = "Start"
            uiSettings.IsEnabled = False
            uiTools.IsEnabled = False
        Else
            ' uiCamera.RenderSize =
            KamerkaStart()
            uiStart.Content = "Stop"
            uiSettings.IsEnabled = True
            uiTools.IsEnabled = True
        End If
    End Sub

    Private Async Sub MenuKamerekClick(sender As Object, e As RoutedEventArgs)
        Dim sWybrana As String = TryCast(sender, ToggleMenuFlyoutItem).Text
        For Each oOldItems As MenuFlyoutItemBase In uiFlyoutCameras.Items
            Dim oTMFItem As ToggleMenuFlyoutItem = TryCast(oOldItems, ToggleMenuFlyoutItem)
            If oTMFItem.Text = sWybrana Then
                oTMFItem.IsChecked = True
                Dim allVideoDevices As DeviceInformationCollection = Await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)
                Dim bFound As Boolean = False
                For Each oVideo As DeviceInformation In allVideoDevices
                    If oVideo.Name = sWybrana Then
                        msCameraId = oVideo.Id
                        bFound = True
                    End If
                Next
                If Not bFound Then Exit Sub
            Else
                oTMFItem.IsChecked = False
            End If
        Next

        If sWybrana.IndexOf("Back") > 0 Then miObrot0 = 90
        If sWybrana.IndexOf("Front") > 0 Then miObrot0 = 270


        If mbStarted Then
            KamerkaStop()
            KamerkaStart()
        End If
    End Sub


    Private Async Sub MenuKamerek()
        Dim bFirst As Boolean = True
        Dim allVideoDevices As DeviceInformationCollection = Await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)
        For Each oVideo As DeviceInformation In allVideoDevices
            Dim oMItem As ToggleMenuFlyoutItem = New ToggleMenuFlyoutItem
            oMItem.Text = oVideo.Name
            If msCameraId = "" Then msCameraId = oVideo.Id
            If bFirst Then
                If oVideo.EnclosureLocation IsNot Nothing AndAlso oVideo.EnclosureLocation.Panel = Windows.Devices.Enumeration.Panel.Back Then
                    bFirst = False
                    If msCameraId <> "" Then
                        For Each oOldItems As MenuFlyoutItemBase In uiFlyoutCameras.Items
                            TryCast(oOldItems, ToggleMenuFlyoutItem).IsChecked = False
                        Next
                    End If
                    msCameraId = oVideo.Id
                    oMItem.IsChecked = True
                End If
            End If
            oMItem.AddHandler(TappedEvent, New TappedEventHandler(AddressOf MenuKamerekClick), False)
            uiFlyoutCameras.Items.Add(oMItem)
        Next
    End Sub

    Private Sub MenuUstawien()

        With moMediaCapture.VideoDeviceController.ZoomControl
            uiSettZoom.IsEnabled = .Supported AndAlso .Max > .Min
            ' Auto, Max, Min, Step, Value // SetAutoAsync , SetValueAsync
        End With

        With moMediaCapture.VideoDeviceController.ExposureControl
            uiSettExpo.IsEnabled = .Supported AndAlso .Max > .Min
            ' Auto, Max, Min, Step, Value // SetAutoAsync , SetValueAsync
        End With

        With moMediaCapture.VideoDeviceController.ExposureCompensationControl
            uiSettExCom.IsEnabled = .Supported AndAlso .Max > .Min
            ' Max, Min, Step, Value // SetValueAsync
        End With

        With moMediaCapture.VideoDeviceController.FocusControl
            uiSettFocus.IsEnabled = .Supported AndAlso .Max > .Min
            ' Auto, Max, Min, Step, Value // SetAutoAsync , SetValueAsync
        End With

        'uiSettFocus.IsEnabled = moMediaCapture.VideoDeviceController.FocusControl.Supported
        'uiSettFocus.IsEnabled = False

        With moMediaCapture.VideoDeviceController.IsoSpeedControl
            uiSettISO.IsEnabled = .Supported AndAlso .Max > .Min
            ' Auto, Max, Min, Step, Value // SetAutoAsync , SetValueAsync // SupportedPresets , SetPresetAsync
        End With

        With moMediaCapture.VideoDeviceController.WhiteBalanceControl
            uiSettWhite.IsEnabled = .Supported ' AndAlso .Max > .Min  ' =5000 jedno i drugie?
            ' Max, Min, Step, Value // SetValueAsync // SetPresetValue, Preset
        End With
#If forStore Then
        uiSettISO.Visibility = Visibility.Collapsed
        uiSettExpo.Visibility = Visibility.Collapsed
#End If

    End Sub

    Private Sub uiGrid_Loaded(sender As Object, e As RoutedEventArgs)
        MenuKamerek()
        '#If forStore Then
        '        uiSettings.Visibility = Visibility.Collapsed
        '#Else
        uiSettings.Visibility = Visibility.Visible
        uiSettings.IsEnabled = False

        '#End If

        ' EFFECTS blackwhite , grayscale
        ' nie działa: exposure ani speed
        ' nie mam jak przetestowac: focus
    End Sub

#Region "obroty"

    Private Sub ZmianaObrotu(iObrot As Integer)
        If Not ui180.IsChecked And Not ui90.IsChecked And Not ui270.IsChecked Then iObrot = 0
        ui90.IsChecked = If(iObrot = 90, True, False)
        ui180.IsChecked = If(iObrot = 180, True, False)
        ui270.IsChecked = If(iObrot = 270, True, False)
        KamerkaObrot(iObrot)
    End Sub

    Private Sub ui90_Click(sender As Object, e As RoutedEventArgs) Handles ui90.Click
        ZmianaObrotu(90)
    End Sub

    Private Sub ui180_Click(sender As Object, e As RoutedEventArgs) Handles ui180.Click
        ZmianaObrotu(180)
    End Sub

    Private Sub ui270_Click(sender As Object, e As RoutedEventArgs) Handles ui270.Click
        ZmianaObrotu(270)
    End Sub

    Private Sub uiMirror_Click(sender As Object, e As RoutedEventArgs) Handles uiMirror.Click
        uiCamera.FlowDirection = If(uiMirror.IsChecked, FlowDirection.RightToLeft, FlowDirection.LeftToRight)
    End Sub
#End Region

#Region "Settings kamerki"

    Dim sUstawienie As String   ' to co zmieniamy
    Public Property CanvasDevice As Object

    Private Sub FlyoutDismiss(sender As Object, e As RoutedEventArgs)
        ' ale moze to sie da zrobic i bez tego?
    End Sub

    Private Async Sub FlyoutChangeAuto(sender As Object, e As RoutedEventArgs)
        ' zmiana trybu auto
        Dim oAuto As ToggleSwitch = TryCast(sender, ToggleSwitch)

        Select Case sUstawienie
            Case "Exposure" ' value = ticks
                Await moMediaCapture.VideoDeviceController.ExposureControl.SetAutoAsync(oAuto.IsOn)
            Case "ISO speed"
                If oAuto.IsOn Then Await moMediaCapture.VideoDeviceController.IsoSpeedControl.SetAutoAsync()
        End Select
    End Sub

    Private Async Sub FlyoutChangeSlide(sender As Object, e As RoutedEventArgs)
        ' zmiana ustawien
        Dim oSlider As Slider = TryCast(sender, Slider)

        ' sprobuj wylaczyc Auto - jesli istnieje
        Dim oMozeAuto As UIElement = TryCast(oSlider.Parent, StackPanel).Children(1)
        Dim oControl As Control = TryCast(oMozeAuto, Control)
        Try
            If oControl.Name = "uiSettingsAuto" Then
                Dim oAuto As ToggleSwitch = TryCast(oMozeAuto, ToggleSwitch)
                oAuto.IsOn = False
            End If
        Catch ex As Exception

        End Try

        Dim dVal As Double = oSlider.Value
        Select Case sUstawienie
            Case "Exposure" ' value = ticks
                Dim oTS As TimeSpan = TimeSpan.FromTicks(dVal)
                Await moMediaCapture.VideoDeviceController.ExposureControl.SetAutoAsync(False)
                Await moMediaCapture.VideoDeviceController.ExposureControl.SetValueAsync(oTS)
            Case "Exposure compensation"
                Await moMediaCapture.VideoDeviceController.ExposureCompensationControl.SetValueAsync(dVal)
            Case "ISO speed"
                Await moMediaCapture.VideoDeviceController.IsoSpeedControl.SetValueAsync(dVal)
            Case "White balance"
                With moMediaCapture.VideoDeviceController.WhiteBalanceControl
                    If .Max > .Min Then
                        Await .SetValueAsync(dVal)
                    Else
                        Await .SetPresetAsync(dVal + 1)
                    End If
                End With
            Case "Hardware zoom"
                moMediaCapture.VideoDeviceController.ZoomControl.Value = dVal
            Case "Focus"
                Await moMediaCapture.VideoDeviceController.FocusControl.SetValueAsync(dVal)
        End Select
    End Sub

    Private Sub EfektyKamerzaste()
        ' moMediaCapture.AddVideoEffectAsync()
        ' TAK m_mediaCaptureMgr->AddEffectAsync(Windows.Media.Capture.MediaStreamType.VideoPreview, "GrayscaleTransform.GrayscaleEffect", nullptr);
        ' NIE m_mediaCaptureMgr->AddEffectAsync(Windows.Media.Capture.MediaStreamType.VideoPreview, "InvertTransform.InvertEffect", nullptr);
    End Sub

    Private Sub PokazFlyout(sHdr As String, bCanAuto As Boolean, bIsAuto As Boolean, dMin As Double, dMax As Double, dStep As Double, dVal As Double)

        sUstawienie = sHdr
        ' tworzenie "od dołu"
        Dim oStack As StackPanel = New StackPanel

        Dim oHdr As TextBlock = New TextBlock
        oHdr.HorizontalAlignment = HorizontalAlignment.Center
        oHdr.Text = sHdr
        oStack.Children.Add(oHdr)

        If bCanAuto Then
            Dim oAuto As ToggleSwitch = New ToggleSwitch
            oAuto.OnContent = "Auto"
            oAuto.OffContent = "manual"
            oAuto.Name = "uiSettingsAuto"
            If bIsAuto Then oAuto.IsOn = True
            'oAuto.AddHandler(toggled, New TappedEventHandler(AddressOf FlyoutChangeAuto), False)
            AddHandler oAuto.Toggled, AddressOf FlyoutChangeAuto
            oStack.Children.Add(oAuto)
        End If

        Dim oSlider As Slider = New Slider
        oSlider.Minimum = dMin
        oSlider.Maximum = dMax
        oSlider.Value = dVal
        oSlider.StepFrequency = dStep
        oSlider.AddHandler(TappedEvent, New TappedEventHandler(AddressOf FlyoutChangeSlide), False)
        oSlider.Name = "uiSettingsSlider"
        oStack.Children.Add(oSlider)

        'Dim oButton = New Button
        'oButton.Content = "OK"
        'oButton.AddHandler(TappedEvent, New TappedEventHandler(AddressOf FlyoutDismiss), True)
        'oStack.Children.Add(oButton)

        Dim oFlyout As Flyout = New Flyout
        oFlyout.Content = oStack

        oFlyout.ShowAt(uiCamera)
    End Sub

#Region "obsluga _Click z menu Settings"


    Private Sub uiSettExpo_Click(sender As Object, e As RoutedEventArgs) Handles uiSettExpo.Click
        With moMediaCapture.VideoDeviceController.ExposureControl
            PokazFlyout("Exposure", True, .Auto, .Min.Ticks, .Max.Ticks, .Step.Ticks, .Value.Ticks)
            ' od 600 ticks, po 100 ticks - Lumia 650
        End With
    End Sub
    Private Sub uiSettExCom_Click(sender As Object, e As RoutedEventArgs) Handles uiSettExCom.Click
        With moMediaCapture.VideoDeviceController.ExposureCompensationControl
            PokazFlyout("Exposure compensation", False, False, .Min, .Max, .Step, .Value)
        End With
    End Sub

    Private Sub uiSettISO_Click(sender As Object, e As RoutedEventArgs) Handles uiSettISO.Click
        With moMediaCapture.VideoDeviceController.IsoSpeedControl
            PokazFlyout("ISO speed", True, .Auto, .Min, .Max, .Step, .Value)
        End With
    End Sub

    Private Sub uiSettWhite_Click(sender As Object, e As RoutedEventArgs) Handles uiSettWhite.Click
        With moMediaCapture.VideoDeviceController.WhiteBalanceControl
            If .Max > .Min Then
                PokazFlyout("White balance", False, False, .Min, .Max, .Step, .Value)
            Else
                PokazFlyout("White balance", False, False, 1, 6, 1, 1)
            End If
        End With
    End Sub

    Private Sub uiSettZoom_Click(sender As Object, e As RoutedEventArgs) Handles uiSettZoom.Click
        With moMediaCapture.VideoDeviceController.ZoomControl
            PokazFlyout("Hardware zoom", False, False, .Min, .Max, .Step, .Value)
        End With
    End Sub

    Private Sub uiSettFocus_Click(sender As Object, e As RoutedEventArgs) Handles uiSettFocus.Click
        With moMediaCapture.VideoDeviceController.FocusControl
            PokazFlyout("Focus", False, False, .Min, .Max, .Step, .Value)
        End With
    End Sub
#End Region


#End Region

#Region "fotki"

    Private Async Function GetOwnFolder() As Task(Of StorageFolder)
        Dim oPicFold As StorageFolder = KnownFolders.PicturesLibrary
        Return Await oPicFold.CreateFolderAsync("Lupka", CreationCollisionOption.OpenIfExists)
    End Function

    Private Async Function Kapturnij() As Task(Of String)
        ' zrób fotkę

        Dim myPictures As StorageFolder = Await GetOwnFolder()

        Dim sFName As String = "Lupka_" & Date.Now.ToString("yyyyMMdd-HHmmss") & ".jpg"
        Dim oFile As StorageFile = Await myPictures.CreateFileAsync(sFName, CreationCollisionOption.GenerateUniqueName)
        sFName = oFile.Name ' bo jakby zadzialalo GenerateUniq, to nie bedzie takie jak dwie linijki wyzej
        'oFile path: d:\pictures\lupka\...

        Dim captureStream As InMemoryRandomAccessStream = New InMemoryRandomAccessStream()
        ' ponizsza funkcja robi dzwiek migawki
        ' jednak nie JPG, bo skoro potem przekodowujemy, to po co kompresowac, jak zaraz bitmapa bedzie potrzebna?
        Await moMediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateBmp(), captureStream)
        Dim decoder As BitmapDecoder = Await BitmapDecoder.CreateAsync(captureStream)
        Dim fileStream As IRandomAccessStream = Await oFile.OpenAsync(FileAccessMode.ReadWrite)
        Dim encoder As BitmapEncoder = Await BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder)

        'BitmapTransform w tej kolejnosci robi: scale, flip, rotation, crop

        ' 1: obrót
        Dim iObrot As Integer = miObrot + miObrot0
        If iObrot = 360 Then iObrot = 0
        Select Case iObrot
            Case 0
                encoder.BitmapTransform.Rotation = BitmapRotation.None
            Case 90
                encoder.BitmapTransform.Rotation = BitmapRotation.Clockwise90Degrees
            Case 180
                encoder.BitmapTransform.Rotation = BitmapRotation.Clockwise180Degrees
            Case 270
                encoder.BitmapTransform.Rotation = BitmapRotation.Clockwise270Degrees
        End Select

        ' limit powinien byc z uiScroll.MinZoom, ale na wszelki wypadek
        ' normalnie: Actual = 320x456
        Dim dZoomH As Double = Math.Min(1, uiScroll.ActualHeight / uiScroll.ExtentHeight)
        Dim dZoomW As Double = Math.Min(1, uiScroll.ActualWidth / uiScroll.ExtentWidth)

        ' ewentualne polozenie obrazka
        ' normalnie: Pixel = 1936x2592 
        Dim iPicH, iPicW As Integer ' obrocenie x i y z decoder
        Dim iW, iH As Integer       ' wielkosc obszaru (zoom)
        Select Case iObrot
            Case 0, 180
                iPicH = decoder.PixelHeight
                iPicW = decoder.PixelWidth
                iW = iPicH * dZoomW
                iH = iPicW * dZoomH
            Case 90, 270
                iPicH = decoder.PixelWidth
                iPicW = decoder.PixelHeight
                iW = iPicW * dZoomW
                iH = iPicH * dZoomH
        End Select

        Dim iMaxX, iMaxY As Integer ' limity wielkosci (zeby nie bylo crop poza zakresem)
        iMaxX = iPicW   ' limity
        iMaxY = iPicH

        Dim iX, iY As Integer ' poczatek obszaru (po obrocie, wiec tak jak na ekranie)
        iX = iPicW * uiScroll.HorizontalOffset / uiScroll.ExtentWidth
        iY = iPicH * uiScroll.VerticalOffset / uiScroll.ExtentHeight

        ' https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/basic-photo-video-and-audio-capture-with-mediacapture
        ' https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/imaging
        'iW = 0
        If iW <> 0 Then
            iX = Math.Min(iMaxX - 10, Math.Abs(iX))
            iY = Math.Min(iMaxY - 10, Math.Abs(iY))
            iW = Math.Max(0, Math.Min(iMaxX - iX, iW))
            iH = Math.Max(0, Math.Min(iMaxY - iY, iH))
            ' This rectangle is defined in the coordinate space after scale, rotation, and flip are applied
            Dim oBbound As BitmapBounds = New BitmapBounds With {.X = iX, .Y = iY, .Width = iW, .Height = iH}
            encoder.BitmapTransform.Bounds = oBbound
        End If

        ' You could use the scrollviewer (uiScroll) data ExtentWidth/ExtentHeight to figure out the sizes, and HorizontalOffset and VerticalOffset to figure out the position of the viewable area
        Try
            Await encoder.FlushAsync()
            Await fileStream.FlushAsync
        Catch ex As Exception
            ' nie psuj jak sie nie uda - bo jak wyleci, to bedzie zielone a nie kamerka
            sFName = ""
        End Try

        ' takie rozbicie, zeby Dispose bylo takze wtedy gdy Catch poprzednie bedzie na Flush
        Try
            fileStream.Dispose()
        Catch ex As Exception
            ' nie psuj jak sie nie uda - bo jak wyleci, to bedzie zielone a nie kamerka
            sFName = ""
        End Try


        Return sFName   ' empty z errora, a inaczej - filename
    End Function

    Private Async Sub uiPhoto_Click(sender As Object, e As RoutedEventArgs) Handles uiPhoto.Click
        Await Kapturnij()
    End Sub

    Private Async Sub UiRecognize_Click(sender As Object, e As RoutedEventArgs) Handles uiRecognize.Click
        Dim sFile As String = Await Kapturnij()
        If sFile = "" Then Exit Sub

        Dim oFold As StorageFolder = Await GetOwnFolder()
        Dim oFile As StorageFile = Await oFold.GetFileAsync(sFile)

        Dim oStream As IRandomAccessStream = Await oFile.OpenAsync(FileAccessMode.Read)
        Dim oDecoder As BitmapDecoder = Await BitmapDecoder.CreateAsync(oStream)
        Dim oBmp As SoftwareBitmap = Await oDecoder.GetSoftwareBitmapAsync()

        Dim rOCR As Windows.Media.Ocr.OcrResult = Await Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages().RecognizeAsync(oBmp)

        Dim sTxt As String = rOCR.Text
        App.ClipPut(sTxt)

        App.DialogBox("Recognized text (sent to Clipboard):" & vbCrLf & sTxt)

    End Sub

    'Private Sub uiAbout_Click(sender As Object, e As RoutedEventArgs)
    '    Dim sTxt As String = "UWP Magnifying Glass" & vbCrLf &
    '        "version " & Package.Current.Id.Version.Major & "." & Package.Current.Id.Version.Minor & "." & Package.Current.Id.Version.Build
    '    App.DialogBox(sTxt)
    'End Sub


#End Region

#Region "proby efektów"

    'Private Sub uiStill_Click(sender As Object, e As RoutedEventArgs) Handles uiStill.Click
    '    ' if previewing
    '    ' screenshot
    '    ' stop kamerki
    '    ' pokaz obrazek
    '    ' else
    '    ' start kamerki
    'End Sub

    'Private Async Sub uiRecognize_Click(sender As Object, e As RoutedEventArgs) Handles uiRecognize.Click
    'If Not NetworkInterface.GetIsNetworkAvailable() Then
    '        DialogBoxRes("resErrorNoNetwork")
    '        Exit Sub
    'End If

    '    Dim sFName = Await Kapturnij() ' zrób fotkę
    '    If sFName = "" Then Exit Sub ' byl jakis blad

    '    Dim sUrl = "https://images.google.com/searchbyimage/upload"
    '    Dim oHttp = New HttpClient()

    '    oHttp.Timeout = TimeSpan.FromMilliseconds(2000)
    '    Dim oHttpCont = New MultipartFormDataContent("---------------------------7e13441438154e")
    '    oHttpCont.Add(New StringContent(""), "image_url")   ' empty

    '    Dim bError = False
    '    Dim oStream As Stream = Nothing
    '    Try
    '        oStream = Await (Await (Await GetOwnFolder()).GetFileAsync(sFName)).OpenStreamForReadAsync
    '    Catch ex As Exception
    '        bError = True
    '    End Try
    '    If bError Then Exit Sub

    '    oHttpCont.Add(New StreamContent(oStream), "encoded_image", sFName)

    '    Try
    '        Dim oResp = Await oHttp.PostAsync(sUrl, oHttpCont)
    '    Catch ex As Exception
    '        bError = True
    '    End Try
    '    If bError Then Exit Sub

    '    ' Status Code:     302 / 
    '    ' location: https :   //www.google.com/search?tbs=sbi:AMhZZislRhGKVr3JUdWB9Vu4y8ZSCqbrg9cFcBg-2QtNtkRN6IQ3uaiDxcmZ3z9BNaA3z_14ukvdEL6sauQqz8co6lg0teZb--necr50OeSQEThsfDzOKe7tq82Kh_1SLrkiMpfetFqPwR-VAlfHEftlQk59L_1xlqlABS2QYJmRZS62V-fl_1X98Zc9fb4mSuIFhC4cYIWnFdayYDUFrat009UOZpc8WDirlOU-kDG_1gP658jUe8KsaGnT_1JvX8wzX9NjmdPn6NBLW-ix-pmISkFT4lbH-tFPbs8vARC8DsIL9rCGbB5QeSMZiTsf2vEZ7Cfb2zzGZgwhXR&hl=en-PL
    '    ' wyślij do rozpoznania
    'End Sub

#End Region


End Class
